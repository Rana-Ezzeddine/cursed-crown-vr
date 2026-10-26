using System;

namespace CursedCrown
{
    // No Unity dependencies: the real runner and standalone regression tests use the same plan.
    [Serializable]
    public sealed class SmokePeerReport
    {
        public int schemaVersion;
        public string mode, status, phase;
        public bool connected, connectedNow, rejected, sessionEnded, finished, canStart;
        public bool validPoses = true, uniqueSpawns = true;
        public int peakPlayers, currentPlayers, joinedEvents, leftEvents, errors, connections;
        public float remoteMotion, elapsed;
    }

    public enum SmokeActionKind { None, Launch, Leave, Join, Stop, Passed, Failed }

    public readonly struct SmokeAction
    {
        public readonly SmokeActionKind Kind;
        public readonly int Index;
        public readonly string Message;
        public SmokeAction(SmokeActionKind kind, int index = -1, string message = "")
        { Kind = kind; Index = index; Message = message; }
    }

    public sealed class SmokeTestPlan
    {
        enum Step { Begin, Fill, RejectExtra, Departure, HostLoss, Rejoin, StopPlayers, FinalReports, Done }
        Step step;
        int expected, stopCursor;
        double deadline, stageDeadline;
        public int Stage { get; private set; }
        public string Status => Stage == 3 ? "Complete" : "Stage " + (Stage + 1) + "/3: " + step;
        public const double StepTimeoutSeconds = 90;
        const double StageTimeoutSeconds = 360;
        int First => Stage == 0 ? 0 : Stage == 1 ? 6 : 8;
        int Count => Stage == 0 ? 6 : 2;

        void Move(Step next, double now)
        { step = next; deadline = now + StepTimeoutSeconds; }

        public SmokeAction Next(double now, Func<int, SmokePeerReport> read)
        {
            if (step == Step.Done) return default;
            if (step == Step.Begin)
            {
                stageDeadline = now + StageTimeoutSeconds;
                expected = 1;
                Move(Step.Fill, now);
                return new SmokeAction(SmokeActionKind.Launch, First);
            }
            if (now > deadline || now > stageDeadline)
                return Fail("Timed out waiting for " + Status + ". Inspect the progress/exit details in summary.txt.");

            for (int i = First; i < First + Count; i++)
            {
                var report = read(i);
                if (report == null) continue;
                if (report.schemaVersion != 2) return Fail("Old test player detected. Rebuild with Run All Foundation Checks.");
                if (report.errors > 0) return Fail("Player " + i + " reported " + report.errors + " runtime error(s).");
            }

            switch (step)
            {
                case Step.Fill:
                    if (!GroupReady(read, First, expected)) return default;
                    if (expected < (Stage == 0 ? 5 : 2))
                    {
                        expected++;
                        Move(Step.Fill, now);
                        return new SmokeAction(SmokeActionKind.Launch, First + expected - 1);
                    }
                    if (!GroupHealthy(read, First, expected)) return default;
                    if (Stage == 0)
                    {
                        // The first five stay alive until the extra client is definitively rejected.
                        Move(Step.RejectExtra, now);
                        return new SmokeAction(SmokeActionKind.Launch, 5);
                    }
                    if (Stage == 1)
                    {
                        Move(Step.HostLoss, now);
                        return new SmokeAction(SmokeActionKind.Stop, 6);
                    }
                    Move(Step.Departure, now);
                    return new SmokeAction(SmokeActionKind.Leave, 9);

                case Step.RejectExtra:
                    if (!GroupReady(read, 0, 5)) return Fail("A participant left before capacity rejection was checked.");
                    var extra = read(5);
                    if (extra == null) return default;
                    if (extra.connected) return Fail("Sixth participant connected while five players were still present.");
                    if (!extra.rejected) return default;
                    Move(Step.Departure, now);
                    return new SmokeAction(SmokeActionKind.Leave, 1);

                case Step.Departure:
                    var host = read(First);
                    var client = read(Stage == 0 ? 1 : 9);
                    if (host == null || client == null || host.leftEvents < 1 || client.connectedNow || !client.canStart)
                        return default;
                    if (Stage == 0)
                    {
                        if (!PeerReady(host, 4)) return default;
                        for (int i = 2; i < 5; i++) if (!PeerReady(read(i), 4)) return default;
                        BeginStop(now);
                    }
                    else
                    {
                        if (!PeerReady(host, 1)) return default;
                        Move(Step.Rejoin, now);
                        return new SmokeAction(SmokeActionKind.Join, 9);
                    }
                    break;

                case Step.HostLoss:
                    var departedHost = read(6);
                    var remaining = read(7);
                    if (departedHost == null || !departedHost.finished || remaining == null ||
                        !remaining.sessionEnded || remaining.connectedNow || !remaining.canStart) return default;
                    BeginStop(now);
                    break;

                case Step.Rejoin:
                    if (!GroupReady(read, 8, 2) || !GroupHealthy(read, 8, 2) ||
                        read(9).connections < 2 || read(8).joinedEvents < 3) return default;
                    BeginStop(now);
                    break;

                case Step.StopPlayers:
                    if (stopCursor < Count)
                        return new SmokeAction(SmokeActionKind.Stop, First + stopCursor++);
                    Move(Step.FinalReports, now);
                    break;

                case Step.FinalReports:
                    for (int i = First; i < First + Count; i++)
                        if (read(i) == null || !read(i).finished) return default;
                    for (int i = First; i < First + Count; i++)
                        if (!FinalPassed(i, read(i))) return Fail("Final acceptance checks failed for player " + i + ".");
                    Stage++;
                    if (Stage == 3)
                    {
                        step = Step.Done;
                        return new SmokeAction(SmokeActionKind.Passed);
                    }
                    step = Step.Begin;
                    break;
            }
            return default;
        }

        void BeginStop(double now) { stopCursor = 0; Move(Step.StopPlayers, now); }
        SmokeAction Fail(string message)
        { step = Step.Done; return new SmokeAction(SmokeActionKind.Failed, message: message); }

        static bool PeerReady(SmokePeerReport r, int count) => r != null && !r.finished &&
            r.connectedNow && r.currentPlayers == count && r.errors == 0;
        static bool GroupReady(Func<int, SmokePeerReport> read, int first, int count)
        {
            for (int i = first; i < first + count; i++) if (!PeerReady(read(i), count)) return false;
            return true;
        }
        static bool Healthy(SmokePeerReport r) => r.validPoses && r.uniqueSpawns && r.remoteMotion > .01f;
        static bool GroupHealthy(Func<int, SmokePeerReport> read, int first, int count)
        {
            for (int i = first; i < first + count; i++) if (!Healthy(read(i))) return false;
            return true;
        }
        public static bool FinalPassed(int index, SmokePeerReport r)
        {
            if (r == null || r.schemaVersion != 2 || !r.finished || r.errors != 0) return false;
            if (index == 5) return r.rejected && !r.connected;
            if (!r.connected) return false;
            if (index < 5) return r.peakPlayers == 5 && Healthy(r) && (index != 0 || r.leftEvents > 0);
            if (r.peakPlayers != 2) return false;
            if (index == 6 || index == 7) return index == 6 || r.sessionEnded;
            return Healthy(r) && (index == 8 ? r.joinedEvents >= 3 && r.leftEvents >= 1 : r.connections >= 2);
        }
    }
}
