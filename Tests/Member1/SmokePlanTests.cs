using System;
using System.Collections.Generic;
using CursedCrown;

static class SmokePlanTests
{
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    sealed class Run
    {
        public readonly SmokeTestPlan Plan = new SmokeTestPlan();
        public readonly Dictionary<int, SmokePeerReport> Reports = new Dictionary<int, SmokePeerReport>();
        public readonly List<int> Launched = new List<int>();
        public double Now;
        public SmokeAction Next(double advance = .1)
        { Now += advance; return Plan.Next(Now, i => Reports.TryGetValue(i, out var r) ? r : null); }
        public void Apply(SmokeAction action)
        {
            int first = action.Index < 6 ? 0 : action.Index < 8 ? 6 : 8;
            switch (action.Kind)
            {
                case SmokeActionKind.Launch:
                    Launched.Add(action.Index);
                    Reports[action.Index] = Healthy(1);
                    if (action.Index == 5)
                    { Reports[5].connected = Reports[5].connectedNow = false; Reports[5].rejected = true; Reports[5].canStart = true; }
                    else UpdateGroup(first, 1);
                    break;
                case SmokeActionKind.Leave:
                    Reports[action.Index].connectedNow = false;
                    Reports[action.Index].canStart = true;
                    Reports[action.Index].currentPlayers = 0;
                    Reports[first].leftEvents++;
                    UpdateGroup(first, 0);
                    break;
                case SmokeActionKind.Join:
                    Reports[action.Index].connectedNow = true;
                    Reports[action.Index].canStart = false;
                    Reports[action.Index].connections++;
                    UpdateGroup(first, 1);
                    break;
                case SmokeActionKind.Stop:
                    Reports[action.Index].finished = true;
                    Reports[action.Index].connectedNow = false;
                    Reports[action.Index].canStart = true;
                    if (action.Index == 6)
                    {
                        Reports[7].sessionEnded = true; Reports[7].connectedNow = false;
                        Reports[7].canStart = true; Reports[7].currentPlayers = 0;
                    }
                    break;
            }
        }
        void UpdateGroup(int first, int joins)
        {
            int end = first == 0 ? 5 : first + 2;
            int count = 0;
            for (int i = first; i < end; i++) if (Reports.TryGetValue(i, out var r) && r.connectedNow) count++;
            for (int i = first; i < end; i++)
                if (Reports.TryGetValue(i, out var r) && r.connectedNow)
                { r.currentPlayers = count; r.peakPlayers = Math.Max(r.peakPlayers, count); r.joinedEvents += joins; }
        }
        public void FillCapacity()
        {
            for (int i = 0; i < 6; i++)
            {
                var action = Next();
                Assert(action.Kind == SmokeActionKind.Launch && action.Index == i, "unexpected capacity setup action");
                Apply(action);
            }
        }
    }
    static SmokePeerReport Healthy(int count) => new SmokePeerReport {
        schemaVersion = 2, connected = true, connectedNow = true, connections = 1,
        currentPlayers = count, peakPlayers = count, joinedEvents = 1, remoteMotion = .1f
    };
    public static int RunAll()
    {
        int count = 0;
        Action<string, Action> test = (name, body) => { body(); count++; Console.WriteLine("PASS runner: " + name); };
        test("slow startup does not launch clients early or stop the host at 50 seconds", () => {
            var run = new Run(); var action = run.Next();
            Assert(action.Kind == SmokeActionKind.Launch && action.Index == 0, "host not launched");
            Assert(run.Next(60).Kind == SmokeActionKind.None, "fixed 50-second finish remains");
            run.Reports[0] = Healthy(1);
            action = run.Next(); Assert(action.Kind == SmokeActionKind.Launch && action.Index == 1, "did not wait for host");
        });
        test("startup waits are bounded and identify the blocked phase", () => {
            var run = new Run(); run.Next();
            var action = run.Next(91);
            Assert(action.Kind == SmokeActionKind.Failed && action.Message.Contains("Fill"), "missing startup timeout detail");
        });
        test("a started client must actually connect before another starts", () => {
            var run = new Run(); run.Apply(run.Next());
            Assert(run.Next().Index == 1, "first client not requested");
            Assert(run.Next(20).Kind == SmokeActionKind.None, "launched next client before a connection report");
        });
        test("sixth launch requires a full roster on every existing peer", () => {
            var run = new Run();
            for (int i = 0; i < 5; i++) run.Apply(run.Next());
            run.Reports[3].currentPlayers = 4;
            Assert(run.Next().Kind == SmokeActionKind.None, "sixth launched before full roster replication");
            run.Reports[3].currentPlayers = 5;
            var action = run.Next();
            Assert(action.Kind == SmokeActionKind.Launch && action.Index == 5, "sixth launch missing");
        });
        test("sixth acceptance is a failure, never waived", () => {
            var run = new Run(); run.FillCapacity(); run.Reports[5].connected = true;
            Assert(run.Next().Kind == SmokeActionKind.Failed, "accepted sixth passed");
        });
        test("first five cannot leave before rejection has been observed", () => {
            var run = new Run(); run.FillCapacity(); run.Reports[5].rejected = false;
            Assert(run.Next(25).Kind == SmokeActionKind.None, "participant left on an independent timer");
            run.Reports[5].rejected = true;
            var action = run.Next();
            Assert(action.Kind == SmokeActionKind.Leave && action.Index == 1, "departure not controlled after rejection");
        });
        test("loss of an existing player invalidates the capacity attempt", () => {
            var run = new Run(); run.FillCapacity(); run.Reports[1].connectedNow = false;
            Assert(run.Next().Kind == SmokeActionKind.Failed, "capacity attempt passed after a place became free");
        });
        test("progress records alone cannot count as final passed reports", () => {
            var r = Healthy(5);
            Assert(!SmokeTestPlan.FinalPassed(1, r), "in-progress report passed");
            r.finished = true; Assert(SmokeTestPlan.FinalPassed(1, r), "valid final report failed");
            r.errors = 1; Assert(!SmokeTestPlan.FinalPassed(1, r), "runtime error waived");
        });
        test("old player/report schema requires a rebuild", () => {
            var run = new Run(); run.Next(); run.Reports[0] = Healthy(1); run.Reports[0].schemaVersion = 0;
            var action = run.Next();
            Assert(action.Kind == SmokeActionKind.Failed && action.Message.Contains("Rebuild"), "stale executable not detected");
        });
        test("all three stages pass only after departures, rejoin and final reports", () => {
            var run = new Run(); bool passed = false;
            for (int i = 0; i < 200; i++)
            {
                var action = run.Next();
                Assert(action.Kind != SmokeActionKind.Failed, action.Message);
                if (action.Kind == SmokeActionKind.Passed) { passed = true; break; }
                run.Apply(action);
            }
            Assert(passed && run.Plan.Stage == 3 && run.Launched.Count == 10, "suite did not complete all ten players");
            Assert(run.Reports[9].connections == 2 && run.Reports[7].sessionEnded, "rejoin/host-loss not exercised");
        });
        test("missing final report remains a failure after the wait deadline", () => {
            var run = new Run(); run.FillCapacity();
            bool finalWait = false;
            for (int i = 0; i < 30; i++)
            {
                var action = run.Next();
                if (action.Kind == SmokeActionKind.Stop && action.Index == 4) { /* Simulate a process that never finishes. */ }
                else run.Apply(action);
                if (run.Plan.Status.Contains("FinalReports")) { finalWait = true; break; }
            }
            Assert(finalWait, "did not reach final wait");
            Assert(run.Next(91).Kind == SmokeActionKind.Failed, "missing report was marked passed");
        });
        return count;
    }
}
