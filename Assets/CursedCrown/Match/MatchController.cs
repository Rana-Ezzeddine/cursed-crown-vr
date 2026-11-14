using System;
using System.Linq;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CursedCrown
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MatchController : NetworkBehaviour, IMatchState
    {
        public struct State : INetworkSerializable, IEquatable<State>
        {
            public MatchPhase Phase;
            public double Deadline;
            public uint Round;
            public bool Carried;
            public ulong Carrier;
            public Vector3 CrownPosition;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
            {
                s.SerializeValue(ref Phase); s.SerializeValue(ref Deadline); s.SerializeValue(ref Round);
                s.SerializeValue(ref Carried); s.SerializeValue(ref Carrier); s.SerializeValue(ref CrownPosition);
            }
            public bool Equals(State o) => Phase == o.Phase && Deadline == o.Deadline && Round == o.Round &&
                Carried == o.Carried && Carrier == o.Carrier && CrownPosition == o.CrownPosition;
        }
        [Serializable]
        public struct PlayerRow : INetworkSerializable, IEquatable<PlayerRow>
        {
            public ulong Id;
            public int Score;
            public FixedString64Bytes Name;
            public bool Ready, Connected;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
            { s.SerializeValue(ref Id); s.SerializeValue(ref Name); s.SerializeValue(ref Score); s.SerializeValue(ref Ready); s.SerializeValue(ref Connected); }
            public bool Equals(PlayerRow o) => Id == o.Id && Name.Equals(o.Name) && Score == o.Score && Ready == o.Ready && Connected == o.Connected;
        }
        [Min(1)] public float RoundSeconds = GameRules.MatchSeconds;
        [Tooltip("Sandbox only: allow crown pickup without Person 4's health service.")]
        public bool AllowMockAlive;
        [Tooltip("Sandbox only: use desktop simulated hand poses. Keep disabled for headset integration.")]
        public bool AllowDesktopPickup;
        public Vector3 Pedestal = new(0, 1.35f, 0);
        readonly NetworkVariable<State> state = new();
        NetworkList<PlayerRow> rows;
        readonly CrownMatchRules rules = new();
        readonly Dictionary<ulong,string> playerNames = new();
        public static string CleanName(string input)
        {
            var result=new StringBuilder();
            foreach(char c in input ?? "")
            {
                if(result.Length>=16)break;
                if((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='-'||c=='\''||c==' ')
                    result.Append(c);
            }
            return string.Join(" ",result.ToString().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries));
        }
        public string PlayerName(ulong id)
        {
            if(rows!=null)foreach(var row in rows)if(row.Id==id && row.Name.Length>0)return row.Name.ToString();
            return "Player "+(id+1);
        }
        public string PlayerLabel(ulong id)
        {
            string name=PlayerName(id);int count=0;
            foreach(var row in rows)if(string.Equals(row.Name.ToString(),name,StringComparison.OrdinalIgnoreCase))count++;
            return count>1?name+" #"+(id+1):name;
        }
        public void SetPlayerName(string name)
        {if(IsSpawned)NameRpc(new FixedString64Bytes(CleanName(name)));}
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void NameRpc(FixedString64Bytes name,RpcParams rpc=default) => HandlePlayerName(rpc.Receive.SenderClientId,name.ToString());
        internal void HandlePlayerName(ulong sender,string name)
        {
            if(!IsServer || !IsSpawned || Phase!=MatchPhase.Lobby || !NetworkManager.ConnectedClients.ContainsKey(sender))return;
            string clean=CleanName(name);if(clean.Length==0)return;
            playerNames[sender]=clean;Publish();
        }
        Vector3 droppedPosition;
        FoundationSession session;
        IPlayerState health;
        ISpawnService spawns;
        public static MatchController Instance { get; private set; }
        public MatchPhase Phase => state.Value.Phase;
        public uint RoundId => state.Value.Round;
        public State Snapshot => state.Value;
        public NetworkList<PlayerRow> Scores => rows;
        public double RemainingSeconds => IsSpawned && Phase == MatchPhase.Playing
            ? Math.Max(0, state.Value.Deadline - NetworkManager.ServerTime.Time) : 0;
        void Awake() { rows = new NetworkList<PlayerRow>(); }
        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (!IsServer) return;
            droppedPosition = Pedestal;
            RefreshServices();
            foreach (ulong id in NetworkManager.ConnectedClientsIds) rules.Join(id);
            session = FoundationSession.Instance;
            session.PlayerJoined += Joined;
            session.PlayerLeft += Left;
            Publish();
        }
        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            if (session != null) { session.PlayerJoined -= Joined; session.PlayerLeft -= Left; }
        }
        void Joined(ulong id) { rules.Join(id); Publish(); }
        void Left(ulong id)
        {
            if (rules.Carrier == id) DropCarrier(id);
            rules.Leave(id, Now); Publish();
        }
        double Now => NetworkManager.ServerTime.Time;
        public bool TryStartMatch()
        {
            if (!IsSpawned || !IsServer || !rules.Start(Now, RoundSeconds)) return false;
            Publish(); return true;
        }
        public bool TryReturnToLobby()
        {
            if (!IsSpawned || !IsServer || !rules.Restart(NetworkManager.ConnectedClientsIds.ToArray())) return false;
            droppedPosition = Pedestal;
            RefreshServices();
            Publish();
            GetComponent<Person5HealthBridge>()?.SynchronizeRounds();
            foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component is IRoundResettable resettable) resettable.ResetRound();
            return true;
        }
        public void SetReady(bool ready) => SetReady(ready, RoundId);
        public void SetReady(bool ready, uint round) { if (IsSpawned) ReadyRpc(ready, round); }
        public void RequestPickup() => RequestPickup(RoundId);
        public void RequestPickup(uint round) { if (IsSpawned) PickupRpc(round); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void ReadyRpc(bool ready, uint round, RpcParams rpc = default)
        { if (rules.SetReady(rpc.Receive.SenderClientId, round, ready)) Publish(); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void PickupRpc(uint round, RpcParams rpc = default)
        { HandlePickup(rpc.Receive.SenderClientId, round); }
        internal void HandlePickup(ulong id, uint round)
        {
            if (!IsServer) return;
            if (round != RoundId || Phase != MatchPhase.Playing || rules.Carrier.HasValue || !IsAlive(id)) return;
            var avatar = FindAvatar(id);
            if (avatar == null || (!AllowDesktopPickup && !avatar.TrackedPose.Value.RightTracked)) return;
            Vector3 hand = avatar.RootPosition.Value + avatar.RootRotation.Value * avatar.TrackedPose.Value.Right;
            Vector3 target = droppedPosition;
            if (!Finite(hand) || Vector3.Distance(hand, target) > GameRules.CrownPickupDistance) return;
            // Raycasts alone miss a wall containing the ray origin.
            foreach (var solid in Physics.OverlapSphere(hand, .01f, ~0, QueryTriggerInteraction.Ignore))
                if (!solid.transform.IsChildOf(avatar.transform)) return;
            foreach (var hit in Physics.RaycastAll(hand, (target - hand).normalized, Vector3.Distance(hand, target), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(avatar.transform)) return;
            if (rules.Pickup(id, round, Now)) Publish();
        }
        public void NotifyPlayerDied(ulong id, uint round)
        { if (IsSpawned && IsServer && round == RoundId && Phase == MatchPhase.Playing) DropCarrier(id); }
        void DropCarrier(ulong id)
        {
            if (rules.Carrier != id) return;
            var avatar = FindAvatar(id);
            Vector3 requested = avatar != null ? avatar.RootPosition.Value : droppedPosition;
            Vector3 position = spawns != null ? spawns.GetValidCrownDrop(requested) : Pedestal;
            droppedPosition = Finite(position) ? position : Pedestal;
            rules.Drop(id, Now); Publish();
        }
        // Call after installing/replacing teammate services dynamically. Cached between rounds.
        public void RefreshServices()
        {
            var components = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            health = GetComponent<IPlayerState>() ?? components.OfType<IPlayerState>().FirstOrDefault();
            spawns = components.OfType<ArenaMatchServices>().FirstOrDefault() ?? components.OfType<ISpawnService>().FirstOrDefault();
        }
        bool IsAlive(ulong id) => health != null ? health.IsAlive(id) : AllowMockAlive;
        public static FoundationAvatar FindAvatar(ulong id) => FindObjectsByType<FoundationAvatar>(FindObjectsSortMode.None)
            .FirstOrDefault(a => a.IsSpawned && a.OwnerClientId == id);
        static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (rules.Carrier.HasValue && !IsAlive(rules.Carrier.Value)) DropCarrier(rules.Carrier.Value);
            rules.Tick(Now);
            Publish();
        }
        void Publish()
        {
            state.Value = new State { Phase = rules.Clock.Phase, Deadline = rules.Clock.Deadline, Round = rules.Clock.Round,
                Carried = rules.Carrier.HasValue, Carrier = rules.Carrier.GetValueOrDefault(), CrownPosition = droppedPosition };
            var ordered = rules.Players.OrderBy(p => p.Key).ToArray();
            var next = ordered.Select(pair => new PlayerRow { Id = pair.Key,
                Name = new FixedString64Bytes(playerNames.TryGetValue(pair.Key,out var name)?name:"Player "+(pair.Key+1)),
                Ready = pair.Value.Ready, Score = pair.Value.Score, Connected = pair.Value.Connected }).ToArray();
            if (rows.Count == next.Length && next.Select((row, i) => rows[i].Equals(row)).All(equal => equal)) return;
            // A join can occur while NGO sends the initial full snapshot. Replace the
            // small roster as a clear/add batch so a queued delta cannot append a
            // duplicate to that snapshot. Do not publish unchanged rows every frame.
            rows.Clear();
            foreach (var row in next) rows.Add(row);
        }
        public string ResultsText()
        {
            if (Phase != MatchPhase.Finished || rows.Count == 0) return "";
            int best = 0;
            foreach (var row in rows) best = Math.Max(best, row.Score);
            var names = new System.Collections.Generic.List<string>();
            foreach (var row in rows) if (row.Score == best) names.Add(PlayerLabel(row.Id));
            string winners = string.Join(", ", names);
            return "Winner(s): " + winners;
        }
    }
}
