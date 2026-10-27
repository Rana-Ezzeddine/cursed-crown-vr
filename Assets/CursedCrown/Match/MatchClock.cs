using System;

namespace CursedCrown
{
    // Host-only rules, independent of rendering and networking for deterministic tests.
    public sealed class MatchClock
    {
        public MatchPhase Phase { get; private set; } = MatchPhase.Lobby;
        public uint Round { get; private set; } = 1;
        public double Deadline { get; private set; }

        public bool TryStart(double now, double duration)
        {
            if (Phase != MatchPhase.Lobby || !Finite(now) || !Finite(duration) || duration <= 0) return false;
            double end = now + duration;
            if (!Finite(end) || end <= now) return false;
            Deadline = end;
            Phase = MatchPhase.Playing;
            return true;
        }

        public bool TryFinish(double now)
        {
            if (Phase != MatchPhase.Playing || !Finite(now) || now < Deadline) return false;
            Phase = MatchPhase.Finished;
            return true;
        }

        public bool TryRestart()
        {
            if (Phase != MatchPhase.Finished || Round == uint.MaxValue) return false;
            Round++;
            Deadline = 0;
            Phase = MatchPhase.Lobby;
            return true;
        }

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
