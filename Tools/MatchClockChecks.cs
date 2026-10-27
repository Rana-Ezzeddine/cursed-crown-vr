using System;
using CursedCrown;

static class MatchClockChecks
{
    static int assertions;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Main()
    {
        var clock = new MatchClock();
        Check(clock.Phase == MatchPhase.Lobby && clock.Round == 1, "Initial lobby");
        Check(!clock.TryRestart() && !clock.TryFinish(100), "No premature restart/finish");
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Check(!clock.TryStart(invalid, 30), "Reject invalid start time");
            Check(!clock.TryStart(0, invalid), "Reject invalid duration");
        }
        Check(!clock.TryStart(0, 0) && !clock.TryStart(0, -1), "Reject non-positive duration");
        Check(!clock.TryStart(double.MaxValue, double.MaxValue), "Reject deadline overflow");
        for (uint round = 1; round <= 100; round++)
        {
            double start = round * 100.0;
            Check(clock.TryStart(start, 30), "Start round");
            Check(clock.Deadline == start + 30 && clock.Round == round, "Round deadline");
            Check(!clock.TryStart(start + 1, 30) && !clock.TryRestart(), "No duplicate start or early restart");
            Check(!clock.TryFinish(double.NaN) && !clock.TryFinish(start + 29.999), "Do not end early");
            Check(clock.TryFinish(round % 2 == 0 ? start + 40 : start + 30), "Finish at deadline or after a skipped frame");
            Check(clock.Deadline == start + 30, "Preserve actual deadline on late frame");
            Check(!clock.TryFinish(start + 50), "Finish exactly once");
            Check(clock.TryRestart(), "Restart finished round");
            Check(clock.Phase == MatchPhase.Lobby && clock.Deadline == 0 && clock.Round == round + 1, "Clean next round");
            Check(!clock.TryRestart(), "Restart exactly once");
        }
        Console.WriteLine("PASS: " + assertions + " match-clock assertions (100 rounds).");
    }
}
