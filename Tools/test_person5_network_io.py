"""File-lock regressions; load runner functions without launching Unity."""
import ast
import json
import pathlib
import time
import unittest
from unittest import mock


def runner_functions():
    source = pathlib.Path(__file__).with_name('check_person5_network.py').read_text(encoding='utf-8')
    tree = ast.parse(source)
    names = {'retry_file_access', 'read', 'command', 'wait', 'match', 'diagnostic_report', 'ReportUnavailable'}
    functions = ast.Module(body=[node for node in tree.body
                                if isinstance(node, (ast.FunctionDef, ast.ClassDef)) and node.name in names],
                           type_ignores=[])
    namespace = {'json': json, 'time': time, 'OUT': mock.MagicMock(), 'seq': {'host': 0},
                 'poll_reports': None, 'checks': [], 'processes': {'host': object()}}
    exec(compile(functions, '<runner file operations>', 'exec'), namespace)
    return namespace


class FileAccessTests(unittest.TestCase):
    def setUp(self):
        self.ns = runner_functions()

    def test_transient_lock_recovers(self):
        operation = mock.Mock(side_effect=[PermissionError('sharing lock'), 'fresh report'])
        with mock.patch.object(time, 'sleep'):
            self.assertEqual(self.ns['retry_file_access'](operation), 'fresh report')
        self.assertEqual(operation.call_count, 2)

    def test_persistent_denial_is_not_hidden(self):
        denial = PermissionError('permanent denial')
        operation = mock.Mock(side_effect=denial)
        with self.assertRaises(PermissionError) as error:
            self.ns['retry_file_access'](operation, timeout=0)
        self.assertIs(error.exception, denial)

    def test_locked_report_returns_fresh_snapshot(self):
        path = self.ns['OUT'].__truediv__.return_value
        path.read_text.side_effect = [PermissionError('sharing lock'), '{"round": 3, "ack": 7}']
        with mock.patch.object(time, 'sleep'):
            self.assertEqual(self.ns['read']('host'), {'round': 3, 'ack': 7})

    def test_report_denial_does_not_become_empty_snapshot(self):
        retry = self.ns['retry_file_access']
        self.ns['retry_file_access'] = lambda operation, **kwargs: retry(operation, timeout=0, retry_errors=kwargs.get('retry_errors', (PermissionError,)))
        self.ns['OUT'].__truediv__.return_value.read_text.side_effect = PermissionError('denied')
        with self.assertRaises(PermissionError):
            self.ns['read']('host')

    def test_not_yet_created_report_is_unavailable(self):
        self.ns['OUT'].__truediv__.return_value.read_text.side_effect = FileNotFoundError()
        with self.assertRaises(self.ns['ReportUnavailable']):
            self.ns['read']('host', timeout=0)

    def test_partial_json_is_not_accepted(self):
        self.ns['OUT'].__truediv__.return_value.read_text.return_value = '{'
        with self.assertRaises(self.ns['ReportUnavailable']):
            self.ns['read']('host', timeout=0)

    def test_command_publish_retries_without_new_sequence(self):
        path = self.ns['OUT'].__truediv__.return_value
        temporary = path.with_suffix.return_value
        temporary.replace.side_effect = [PermissionError('reader lock'), None]
        self.ns['read'] = mock.Mock(return_value={'ack': 1})
        with mock.patch.object(time, 'sleep'):
            result = self.ns['command']('host', 'ready', value=True)
        self.assertEqual(result, {'ack': 1})
        self.assertEqual(self.ns['seq']['host'], 1)
        self.assertEqual(temporary.replace.call_count, 2)
        temporary.write_text.assert_called_once()
        payload = json.loads(temporary.write_text.call_args.args[0])
        self.assertEqual(payload, {'sequence': 1, 'action': 'ready', 'value': True})


    def test_missing_report_recovers_before_deadline(self):
        path = self.ns['OUT'].__truediv__.return_value
        path.read_text.side_effect = [FileNotFoundError(), '{"health": [{"hp": 100}]}']
        with mock.patch.object(time, 'sleep'):
            self.assertEqual(self.ns['read']('host')['health'][0]['hp'], 100)

    def test_poll_retries_unavailable_report(self):
        self.ns['read'] = mock.Mock(side_effect=[
            self.ns['ReportUnavailable']('being replaced'), {'health': [100]}])
        with mock.patch.object(time, 'sleep'):
            self.ns['wait']('health received', lambda: self.ns['match']()['health'] == [100])
        self.assertEqual(self.ns['checks'], ['health received'])
        self.assertIsNone(self.ns['poll_reports'])

    def test_poll_retries_missing_health_field(self):
        self.ns['read'] = mock.Mock(side_effect=[{'connected': True}, {'health': [100]}])
        with mock.patch.object(time, 'sleep'):
            self.ns['wait']('health received', lambda: self.ns['match']()['health'] == [100])
        self.assertEqual(self.ns['checks'], ['health received'])

    def test_missing_health_never_counts_as_pass(self):
        self.ns['read'] = mock.Mock(return_value={'connected': True})
        with mock.patch.object(time, 'monotonic', side_effect=[0, 0, 1]), mock.patch.object(time, 'sleep'):
            with self.assertRaisesRegex(AssertionError, "missing report field 'health'"):
                self.ns['wait']('health received', lambda: self.ns['match']()['health'], timeout=1)
        self.assertEqual(self.ns['checks'], [])
        self.assertIsNone(self.ns['poll_reports'])

    def test_poll_uses_one_report_per_peer(self):
        self.ns['read'] = mock.Mock(side_effect=[
            {'round': 2, 'health': [{'round': 2}]}, {'round': 3}])
        self.ns['wait']('consistent round', lambda:
                        self.ns['match']()['round'] == 2 and
                        self.ns['match']()['health'][0]['round'] == 2)
        self.assertEqual(self.ns['read'].call_count, 1)

    def test_command_waits_for_report_publication(self):
        self.ns['read'] = mock.Mock(side_effect=[
            self.ns['ReportUnavailable']('being replaced'), {'ack': 1}])
        with mock.patch.object(time, 'sleep'):
            self.assertEqual(self.ns['command']('host', 'ready'), {'ack': 1})
        self.assertEqual(self.ns['seq']['host'], 1)


if __name__ == '__main__':
    unittest.main()
