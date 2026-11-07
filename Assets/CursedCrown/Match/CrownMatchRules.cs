using System;
using System.Collections.Generic;
using System.Linq;

namespace CursedCrown
{
    // Host-only game rules. Geometry, player health and network identity are validated
    // by the Unity adapter before calling these methods.
    public sealed class CrownMatchRules
    {
        public sealed class Player
        {
            public bool Ready { get; internal set; }
            public int Score { get; internal set; }
            public bool Connected { get; internal set; } = true;
        }
        readonly Dictionary<ulong, Player> players = new();
        public IReadOnlyDictionary<ulong, Player> Players => players;
        public MatchClock Clock { get; } = new();
        public ulong? Carrier { get; private set; }
        double possessionStart;
        int awarded;

        public bool Join(ulong id)
        {
            if (players.TryGetValue(id, out var existing))
            {
                if (existing.Connected) return false;
                existing.Connected = true;
                existing.Ready = false;
                return true;
            }
            players.Add(id, new Player());
            return true;
        }
        public bool SetReady(ulong id, uint round, bool ready)
        {
            if (round != Clock.Round || Clock.Phase != MatchPhase.Lobby || !players.TryGetValue(id, out var p) || !p.Connected) return false;
            p.Ready = ready;
            return true;
        }
        public bool Start(double now, double duration)
        {
            if (!players.Values.Any(p => p.Connected) || players.Values.Any(p => p.Connected && !p.Ready)) return false;
            return Clock.TryStart(now, duration);
        }
        public bool Pickup(ulong id, uint round, double now)
        {
            Tick(now);
            if (!Finite(now) || round != Clock.Round || Clock.Phase != MatchPhase.Playing || Carrier.HasValue || !players.TryGetValue(id, out var p) || !p.Connected) return false;
            Carrier = id;
            possessionStart = now;
            awarded = 0;
            return true;
        }
        public bool Drop(ulong id, double now)
        {
            if (!Finite(now) || Carrier != id) return false;
            Settle(now);
            Carrier = null;
            awarded = 0;
            return true;
        }
        public bool Leave(ulong id, double now)
        {
            Drop(id, now);
            if (!players.TryGetValue(id, out var p) || !p.Connected) return false;
            p.Connected = false;
            p.Ready = false;
            return true;
        }
        public void Tick(double now)
        {
            if (!Finite(now) || Clock.Phase != MatchPhase.Playing) return;
            Settle(now);
            if (Clock.TryFinish(now)) Carrier = null;
        }
        void Settle(double now)
        {
            if (Clock.Phase != MatchPhase.Playing || !Carrier.HasValue || !players.TryGetValue(Carrier.Value, out var p)) return;
            double elapsed = Math.Max(0, Math.Min(now, Clock.Deadline) - possessionStart);
            int wholeSeconds = (int)Math.Min(int.MaxValue, Math.Floor(elapsed));
            int delta = Math.Max(0, wholeSeconds - awarded);
            p.Score = (int)Math.Min(int.MaxValue, (long)p.Score + delta);
            awarded = Math.Max(awarded, wholeSeconds);
        }
        public bool Restart(IEnumerable<ulong> connectedPlayers)
        {
            if (!Clock.TryRestart()) return false;
            Carrier = null;
            possessionStart = 0;
            awarded = 0;
            players.Clear();
            foreach (ulong id in connectedPlayers) Join(id);
            return true;
        }
        public ulong[] Winners()
        {
            if (Clock.Phase != MatchPhase.Finished || players.Count == 0) return Array.Empty<ulong>();
            int highest = players.Values.Max(p => p.Score);
            return players.Where(p => p.Value.Score == highest).Select(p => p.Key).OrderBy(id => id).ToArray();
        }
        static bool Finite(double time) => !double.IsNaN(time) && !double.IsInfinity(time);
    }
}
