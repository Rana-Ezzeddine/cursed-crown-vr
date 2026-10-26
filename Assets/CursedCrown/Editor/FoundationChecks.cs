using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CursedCrown.Editor
{
    public static class FoundationChecks
    {
        static readonly Dictionary<int, Process> processes = new();
        static readonly Dictionary<int, SmokePeerReport> snapshots = new();
        static readonly HashSet<int> killed = new();
        static string reports, binary, failure;
        static SmokeTestPlan plan;
        static Stopwatch clock;
        static double nextPoll, nextSummary, failureDeadline;
        static bool ending;

        static FoundationChecks()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Abort;
            EditorApplication.quitting += Abort;
        }

        [MenuItem("Cursed Crown/4 - Run Local Six-Process Network Checks")]
        public static void Run()
        {
            if (clock != null) throw new InvalidOperationException("Checks already running.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before running the network checks.");
            binary = DesktopTestPlayer.BinaryPath();
            VerifyPoseValidation();
            MemberOneChecks.VerifyInputs();
            reports = Path.Combine(Path.GetTempPath(), "cursed-crown-smoke-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff"));
            Directory.CreateDirectory(reports);
            snapshots.Clear(); killed.Clear();
            plan = new SmokeTestPlan();
            ending = false; failure = ""; nextPoll = nextSummary = 0;
            clock = Stopwatch.StartNew();
            EditorApplication.update += Tick;
            Debug.Log("CC_NETWORK_CHECKS_STARTED v2 reports=" + reports);
        }

        static void Tick()
        {
            if (clock == null || clock.Elapsed.TotalSeconds < nextPoll) return;
            double now = clock.Elapsed.TotalSeconds;
            nextPoll = now + .25;
            try
            {
                RefreshReports();
                if (ending)
                {
                    if (AllExited() || now >= failureDeadline) FinishFailure();
                    return;
                }
                foreach (var pair in processes)
                {
                    if (!pair.Value.HasExited) continue;
                    // Read once more after observing exit; it can race the earlier refresh.
                    ReadReport(pair.Key);
                    if (!snapshots.TryGetValue(pair.Key, out var r) || !r.finished || pair.Value.ExitCode != 0)
                    {
                        BeginFailure("Player " + pair.Key + " exited without a successful final report (exit " + pair.Value.ExitCode + ").");
                        return;
                    }
                }

                int stage = plan.Stage;
                var action = plan.Next(now, ForPlan);
                switch (action.Kind)
                {
                    case SmokeActionKind.Launch: Launch(action.Index); break;
                    case SmokeActionKind.Leave: Command(action.Index, "leave"); break;
                    case SmokeActionKind.Join: Command(action.Index, "join"); break;
                    case SmokeActionKind.Stop: Command(action.Index, "stop"); break;
                    case SmokeActionKind.Failed: BeginFailure(action.Message); return;
                    case SmokeActionKind.Passed:
                        WriteSummary("PASS");
                        Unhook();
                        CleanupProcesses();
                        Debug.Log("CC_NETWORK_CHECKS_PASS: five participants, sixth rejected while full, poses, departures, late join and reconnect. " + reports);
                        return;
                }
                if (plan.Stage != stage)
                {
                    Debug.Log("CC_NETWORK_STAGE_PASS " + (stage + 1) + "/3");
                    CleanupProcesses(); // ForPlan exposes a final report only after its process exits.
                }
                if (now >= nextSummary)
                {
                    nextSummary = now + 2;
                    WriteSummary("RUNNING");
                }
            }
            catch (Exception ex)
            {
                // Keep existing checkpoints and give only this run's processes time to finish.
                if (ending) { FinishFailure(); return; }
                BeginFailure("Runner exception: " + ex.GetType().Name + ". See process progress in summary.txt.");
                Debug.LogException(ex);
            }
        }

        static void Launch(int index)
        {
            int stage = index < 6 ? 0 : index < 8 ? 1 : 2;
            bool host = index == 0 || index == 6 || index == 8;
            string args = "-batchmode -nographics -job-worker-count 2 -ccMode " + (host ? "host" : "client") +
                " -ccPort " + (17877 + stage) + " -ccExitAfter 900" +
                " -ccReport \"" + Path.Combine(reports, index + ".json") + "\"" +
                " -ccCommand \"" + Path.Combine(reports, index + ".command") + "\"" +
                " -logFile \"" + Path.Combine(reports, index + ".log") + "\"";
            var process = Process.Start(new ProcessStartInfo(binary, args) { UseShellExecute = false });
            if (process == null) throw new InvalidOperationException("Test player did not start.");
            processes.Add(index, process);
            Debug.Log("CC_TEST_PLAYER_STARTED " + index + " " + plan.Status);
        }

        static void Command(int index, string action)
        {
            if (!processes.TryGetValue(index, out var process) || process.HasExited) return;
            File.WriteAllText(Path.Combine(reports, index + ".command"), action);
        }
        static void RefreshReports()
        {
            foreach (int index in processes.Keys) ReadReport(index);
        }
        static void ReadReport(int index)
        {
            string path = Path.Combine(reports, index + ".json");
            if (!File.Exists(path)) return;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                var report = JsonUtility.FromJson<SmokePeerReport>(reader.ReadToEnd());
                if (report != null) snapshots[index] = report;
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            { /* Keep the previous complete checkpoint and retry on the next poll. */ }
        }
        static SmokePeerReport ForPlan(int index)
        {
            if (!snapshots.TryGetValue(index, out var report)) return null;
            if (report.finished && processes.TryGetValue(index, out var process) && !process.HasExited) return null;
            return report;
        }
        static bool AllExited()
        {
            foreach (var process in processes.Values) if (!process.HasExited) return false;
            return true;
        }
        static void BeginFailure(string reason)
        {
            failure = reason;
            ending = true;
            failureDeadline = clock.Elapsed.TotalSeconds + 10;
            foreach (int index in processes.Keys) Command(index, "stop");
            WriteSummary("FAIL (collecting final reports)");
        }
        static void FinishFailure()
        {
            Unhook();
            RefreshReports();
            foreach (var pair in processes)
                if (!pair.Value.HasExited) { pair.Value.Kill(); killed.Add(pair.Key); }
            WriteSummary("FAIL");
            CleanupProcesses();
            Debug.LogError("CC_NETWORK_CHECKS_FAIL: " + failure + " Inspect " + reports);
        }
        static void WriteSummary(string outcome)
        {
            var lines = new List<string> { outcome + " - readiness-driven network checks v2", plan.Status };
            if (!string.IsNullOrEmpty(failure)) lines.Add("Reason: " + failure);
            for (int index = 0; index < 10; index++)
            {
                bool launched = processes.ContainsKey(index) || snapshots.ContainsKey(index);
                if (!launched) continue;
                string processState = killed.Contains(index) ? "stopped after deadline" :
                    processes.TryGetValue(index, out var p) ? (p.HasExited ? "exited " + p.ExitCode : "running") : "exited 0";
                if (!snapshots.TryGetValue(index, out var r))
                    lines.Add(index + ": NO STARTUP REPORT (" + processState + ")");
                else
                    lines.Add(index + ": " + (r.finished ? (SmokeTestPlan.FinalPassed(index, r) ? "PASS" : "FAIL") : "IN PROGRESS") +
                        " (" + processState + ") " + JsonUtility.ToJson(r));
            }
            File.WriteAllLines(Path.Combine(reports, "summary.txt"), lines);
        }
        static void CleanupProcesses()
        {
            foreach (var process in processes.Values)
            {
                try { if (!process.HasExited) process.Kill(); }
                catch (Exception ex) { Debug.LogWarning("CC_TEST_CLEANUP " + ex.GetType().Name); }
                finally { process.Dispose(); }
            }
            processes.Clear();
        }
        static void Unhook()
        {
            EditorApplication.update -= Tick;
            clock = null;
        }
        static void Abort()
        {
            if (clock == null) return;
            failure = "Run interrupted by Editor exit or script reload.";
            foreach (var pair in processes) if (!pair.Value.HasExited) killed.Add(pair.Key);
            WriteSummary("ABORTED");
            Unhook();
            CleanupProcesses();
        }

        [MenuItem("Cursed Crown/Cancel Running Network Checks")]
        public static void Cancel()
        {
            if (clock != null && !ending) BeginFailure("Network check cancelled by the user.");
        }

        public static void VerifyPoseValidation()
        {
            var pose=new AvatarPose{Head=Vector3.up*1.65f,Left=new Vector3(-.2f,1.2f,.3f),Right=new Vector3(.2f,1.2f,.3f),
                HeadRotation=Quaternion.identity,LeftRotation=Quaternion.identity,RightRotation=Quaternion.identity};
            if(!pose.IsValid())throw new Exception("Valid pose rejected.");
            pose.Head.x=float.NaN;if(pose.IsValid())throw new Exception("NaN pose accepted.");
            pose.Head=Vector3.up*1.65f;pose.Left=Vector3.one*20;if(pose.IsValid())throw new Exception("Out-of-range controller accepted.");
            pose.Left=new Vector3(-.2f,1.2f,.3f);pose.HeadRotation=new Quaternion(0,0,0,0);if(pose.IsValid())throw new Exception("Invalid quaternion accepted.");
            Debug.Log("CC_POSE_VALIDATION_PASS");
        }
        [MenuItem("Cursed Crown/Run All Foundation Checks (build + network)")]
        public static void RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building/running checks.");
            if (!File.Exists(StarterSetup.ScenePath)) StarterSetup.CreateScene();
            StarterSetup.BuildDesktop();
            Run();
        }
    }
}
