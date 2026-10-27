using System;
using System.Linq;
using CursedCrown;
static class CrownMatchChecks
{
    static int count;
    static void Check(bool value, string why) { count++; if (!value) throw new Exception(why); }
    static CrownMatchRules Started(double duration = 10)
    {
        var r = new CrownMatchRules();
        Check(!r.Start(0, duration), "Empty match cannot start");
        r.Join(0); r.Join(1);
        Check(!r.Start(0, duration), "Not-ready players block start");
        Check(!r.SetReady(8, 1, true) && !r.SetReady(0, 99, true), "Unknown/stale readiness rejected");
        r.SetReady(0, 1, true); r.SetReady(1, 1, true);
        Check(r.Start(0, duration), "All-ready match starts");
        return r;
    }
    static void Main()
    {
        var r = Started();
        Check(!r.Pickup(9, 1, 0) && !r.Pickup(0, 99, 0), "Unknown/stale pickup rejected");
        Check(r.Pickup(0, 1, 0), "First pickup accepted");
        Check(!r.Pickup(1, 1, 0), "Competing pickup rejected");
        r.Tick(2.7); Check(r.Players[0].Score == 2, "Completed seconds only");
        r.Tick(2.7); Check(r.Players[0].Score == 2, "No duplicate points");
        Check(r.Drop(0, 2.7) && !r.Drop(0, 2.7), "Drop exactly once");
        r.Pickup(0, 1, 3); r.Tick(3.9);
        Check(r.Players[0].Score == 2, "Fraction discarded on new possession");
        r.Tick(4); Check(r.Players[0].Score == 3, "Fresh possession counter");
        r.Tick(100); Check(r.Players[0].Score == 9, "Late frame capped at round deadline");
        Check(r.Clock.Phase == MatchPhase.Finished && r.Carrier == null, "End clears carrier");
        Check(!r.Pickup(1, 1, 100), "No pickup after match");
        Check(r.Winners().SequenceEqual(new ulong[] { 0 }), "Winner computed");
        Check(r.Restart(new ulong[] { 0, 1 }), "Restart succeeds");
        Check(r.Clock.Round == 2 && r.Players.Values.All(p => p.Score == 0 && !p.Ready), "Reset scores and readiness");
        Check(!r.Restart(new ulong[] { 0 }), "Duplicate reset rejected");
        Check(!r.SetReady(0, 1, true), "Old-round request rejected after reset");
        var d = Started(); d.Pickup(0, 1, 0); d.Leave(0, 2.4);
        Check(d.Carrier == null && d.Players[0].Score == 2 && !d.Players[0].Connected, "Disconnect drops and retains score");
        Check(!d.Pickup(0, 1, 3), "Disconnected player cannot pick up");
        Check(d.Pickup(1, 1, 3), "Another player can claim dropped crown");
        d.Drop(1, 5); d.Tick(10);
        Check(d.Winners().SequenceEqual(new ulong[] { 0, 1 }), "Tied scores share victory");
        Check(d.Restart(new ulong[] { 1 }) && d.Players.Count == 1 && !d.Players.ContainsKey(0), "Reset removes departed players");
        var edge = Started();
        Check(!edge.Pickup(0, 1, double.NaN), "Invalid request time rejected");
        Check(!edge.Pickup(0, 1, 10), "Pickup at exact deadline rejected");
        Check(edge.Players[0].Score == 0, "Deadline pickup earns no points");
        for (int round = 1; round <= 100; round++)
        {
            var x = Started(4);
            x.Pickup(0, 1, 0); x.Tick(1); x.Drop(0, 1.9);
            x.Pickup(1, 1, 2); x.Tick(999);
            Check(x.Players[0].Score == 1 && x.Players[1].Score == 2, "Possession exchange and deadline consistency");
        }
        Console.WriteLine("PASS: " + count + " crown/match assertions.");
    }
}
