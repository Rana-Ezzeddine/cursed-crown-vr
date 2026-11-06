using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace CursedCrown
{
    public enum SessionPhase { Idle, Starting, Connecting, Connected, Leaving, Failed }

    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class FoundationSession : MonoBehaviour
    {
        public static FoundationSession Instance { get; private set; }

        public NetworkManager Manager { get; private set; }

        public string Address = "127.0.0.1";
        public ushort Port = 7777;

        // NEW: Arena spawn integration
        [Header("Arena Integration")]
        public ArenaSpawnManager arenaSpawnManager;

        public string JoinCode { get; private set; } = "";
        public string Status { get; private set; } = "Choose Host or Join.";
        public SessionPhase Phase { get; private set; }
        public bool Busy => Phase == SessionPhase.Starting || Phase == SessionPhase.Connecting ||
            Phase == SessionPhase.Leaving || (Manager != null && Manager.ShutdownInProgress);
        public bool CanStart => Manager != null && !Busy && !Manager.IsListening;
        public int PlayerCount => players.Count;
        public event Action<ulong> PlayerJoined;
        public event Action<ulong> PlayerLeft;
        public event Action<string> SessionEnded;

        // Avatar spawn/despawn reports the complete local roster, including late-join snapshots.
        readonly HashSet<ulong> players = new();
        readonly HashSet<ulong> admitted = new();
        readonly Dictionary<ulong, int> slots = new();

        // NEW: Remember each player's actual reserved spawn.
        readonly Dictionary<ulong, Pose> assignedSpawns = new();

        UnityTransport transport;
        SessionPhase phaseAfterStop;

        bool endedRaised;
        int attempt, stopFrame;
        double deadline;
        Task servicesReady;
        const double ConnectionTimeout = 20;
        const double ServicesTimeout = 45;
        void Awake()
        {
            Instance = this;

            Application.runInBackground = true;

            Manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();

            Manager.ConnectionApprovalCallback += Approve;
            Manager.OnClientConnectedCallback += Connected;
            Manager.OnClientDisconnectCallback += Disconnected;
            Manager.OnTransportFailure += TransportFailed;

            Manager.NetworkConfig.ConnectionApproval = true;
            Manager.NetworkConfig.EnableSceneManagement = false;
            Manager.NetworkConfig.TickRate = 30;
            Manager.NetworkConfig.ClientConnectionBufferTimeout = 15;
        }

        void Update()
        {
            if ((Phase == SessionPhase.Starting || Phase == SessionPhase.Connecting) &&
                Time.realtimeSinceStartupAsDouble >= deadline)
                StopSession("Connection timed out. Check the host, network and code/address, then try again.", true);

            // NGO stop callbacks run before shutdown finishes. Wait at least one frame.
            if (Phase == SessionPhase.Leaving && Time.frameCount > stopFrame &&
                !Manager.IsListening && !Manager.ShutdownInProgress)
                CompleteStop();
            else if (Phase == SessionPhase.Connected && !Manager.IsListening)
                StopSession("Session ended. The host left or the connection was lost. You can host or join again.", true);
        }

        bool Begin(string message, double timeout)
        {
            if (!CanStart) return false;
            attempt++;
            endedRaised = false;
            ClearSpawnReservations();
            admitted.Clear();
            players.Clear();
            JoinCode = "";
            Phase = SessionPhase.Starting;
            Status = message;
            deadline = Time.realtimeSinceStartupAsDouble + timeout;
            return true;
        }

        // =====================================================
        // NEW: CLEAR ARENA SPAWN RESERVATIONS
        // =====================================================

        void ClearSpawnReservations()
        {
            if (arenaSpawnManager != null)
            {
                foreach (var entry in slots)
                {
                    arenaSpawnManager.ReleaseSpawn(entry.Value);
                }
            }

            assignedSpawns.Clear();
            slots.Clear();
        }

        // =====================================================
        // LAN HOSTING / JOINING
        // =====================================================

        public bool StartLan(bool host)
        {
            if (!CanStart) return false;
            string address = "127.0.0.1";
            if (Port == 0 || (!host && !SessionInput.TryAddress(Address, out address)))
            {
                Phase = SessionPhase.Failed;
                Status = "Enter the host's IPv4 address, such as 192.168.1.10, and a nonzero port.";
                return false;
            }
            if (!Begin(host ? "Starting LAN host..." : "Connecting to " + address + "...", ConnectionTimeout)) return false;
            try
            {
                transport.SetConnectionData(address, Port, host ? "0.0.0.0" : null);
                Phase = SessionPhase.Connecting;
                bool started = host ? Manager.StartHost() : Manager.StartClient();
                if (!started) StopSession("Could not start the connection. Check the network/port and try again.", true);
                else if (host && Phase != SessionPhase.Leaving)
                {
                    Phase = SessionPhase.Connected;
                    Status = "Hosting LAN on port " + Port + ". Players can join using this device's LAN IPv4 address.";
                }
                return started;
            }
            catch (Exception ex)
            {
                Fail("LAN could not start. Check the address, port and network permissions.", ex);
                return false;
            }
        }

        // UI entry point. Exceptions are handled and cancelled/stale completions are ignored.
        public async void StartRelay(bool host, string code = "")
        {
            if (!CanStart) return;
            string normalized = "";
            if (!host && !SessionInput.TryRelayCode(code, out normalized))
            {
                Phase = SessionPhase.Failed;
                Status = "Enter the host's Relay code using letters and numbers.";
                return;
            }
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Phase = SessionPhase.Failed;
                Status = "Relay needs a linked Unity services project. Complete the team setup, or use LAN.";
                return;
            }
            if (!Begin("Connecting to Unity services...", ServicesTimeout)) return;
            int current = attempt;
            try
            {
                // Retried UI attempts share an in-flight sign-in instead of racing the SDK.
                if (servicesReady == null || servicesReady.IsFaulted || servicesReady.IsCanceled ||
                    (servicesReady.IsCompleted && !AuthenticationService.Instance.IsSignedIn))
                    servicesReady = InitializeServices();
                await servicesReady;
                if (!IsCurrent(current)) return;
                if (host)
                {
                    Status = "Creating Relay session...";
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameRules.MaxPlayers - 1);
                    if (!IsCurrent(current)) return;
                    string createdCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    if (!IsCurrent(current)) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    Phase = SessionPhase.Connecting;
                    deadline = Time.realtimeSinceStartupAsDouble + ConnectionTimeout;
                    admitted.Clear();
                    if (!Manager.StartHost()) throw new InvalidOperationException("Relay host start failed.");
                    if (!IsCurrent(current)) return;
                    JoinCode = createdCode;
                    Phase = SessionPhase.Connected;
                    Status = "Hosting Relay. Share the code displayed below.";
                }
                else
                {
                    Status = "Looking up Relay code...";
                    JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(normalized);
                    if (!IsCurrent(current)) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    Phase = SessionPhase.Connecting;
                    Status = "Joining Relay session...";
                    deadline = Time.realtimeSinceStartupAsDouble + ConnectionTimeout;
                    if (!Manager.StartClient()) throw new InvalidOperationException("Relay client start failed.");
                }
            }
            catch (Exception ex)
            {
                if (IsCurrent(current))
                    Fail("Relay unavailable. Check project linking and network access, or use LAN.", ex);
            }
        }

        bool IsCurrent(int current)
        {
            return this != null && current == attempt &&
                   Phase != SessionPhase.Leaving && Phase != SessionPhase.Failed;
        }

        static async Task InitializeServices()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        void Fail(string message, Exception ex)
        {
            Debug.LogError(message + " " + ex);
            if (Manager != null && (Manager.IsListening || Manager.ShutdownInProgress))
                StopSession(message, true);
            else
            {
                Phase = SessionPhase.Failed;
                Status = message;
            }
        }

        // Called by FoundationAvatar when a networked avatar appears/disappears.
        public void RegisterAvatar(ulong id)
        {
            if (players.Add(id))
                PlayerJoined?.Invoke(id);
        }

        public void UnregisterAvatar(ulong id)
        {
            if (players.Remove(id))
                PlayerLeft?.Invoke(id);
        }

        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool allowed = admitted.Contains(request.ClientNetworkId) || admitted.Count < GameRules.MaxPlayers;
            response.Approved = allowed;
            response.CreatePlayerObject = allowed;
            response.Pending = false;

            response.Reason = allowed
                ? ""
                : "Session full: five players maximum.";

            if (allowed)
            {
                admitted.Add(request.ClientNetworkId);
            }
        }

        // =====================================================
        // UPDATED: MULTIPLAYER ARENA SPAWN ASSIGNMENT
        // =====================================================

        public Pose AssignSpawn(ulong id)
        {
            if (!Manager.IsServer)
            {
                throw new InvalidOperationException(
                    "Only the host assigns spawn slots."
                );
            }

            // Return the same spawn if already assigned.
            if (assignedSpawns.TryGetValue(
                id,
                out Pose existingSpawn))
            {
                return existingSpawn;
            }

            // Keep Member 1's original slot assignment.
            if (!slots.TryGetValue(id, out int slot))
            {
                slot = 0;

                while (slots.ContainsValue(slot) &&
                       slot < GameRules.MaxPlayers)
                {
                    slot++;
                }

                if (slot >= GameRules.MaxPlayers)
                {
                    throw new InvalidOperationException(
                        "No free foundation spawn."
                    );
                }

                slots[id] = slot;
            }

            // NEW: Use your ArenaSpawnManager.
            if (arenaSpawnManager != null)
            {
                if (arenaSpawnManager.TryReserveSpawn(
                    slot,
                    out Vector3 spawnPosition,
                    out Quaternion spawnRotation))
                {
                    Pose arenaSpawn = new Pose(
                        spawnPosition,
                        spawnRotation
                    );

                    assignedSpawns[id] = arenaSpawn;

                    Debug.Log(
                        $"Player {id} assigned arena spawn " +
                        $"at {spawnPosition}"
                    );

                    return arenaSpawn;
                }

                Debug.LogWarning(
                    $"No safe arena spawn available for player {id}. " +
                    "Using foundation fallback."
                );
            }

            // Preserve Member 1's original spawn calculation.
            float angle =
                slot * Mathf.PI * 2 / GameRules.MaxPlayers;

            var position = new Vector3(
                Mathf.Sin(angle) * 8f,
                .12f,
                Mathf.Cos(angle) * 8f
            );

            Pose fallbackSpawn = new Pose(
                position,
                Quaternion.LookRotation(
                    -new Vector3(
                        position.x,
                        0,
                        position.z
                    )
                )
            );

            assignedSpawns[id] = fallbackSpawn;

            return fallbackSpawn;
        }

        // =====================================================
        // PLAYER CONNECTED
        // =====================================================

        void Connected(ulong id)
        {
            if (id == Manager.LocalClientId && Phase != SessionPhase.Leaving)
            {
                Phase = SessionPhase.Connected;
                Status = Manager.IsHost ? "Hosting. Players can join." : "Connected to host.";
            }
            Debug.Log("CC_CONNECTED " + id);
        }

        // =====================================================
        // UPDATED: PLAYER DISCONNECTED
        // =====================================================

        void Disconnected(ulong id)
        {
            UnregisterAvatar(id);
            admitted.Remove(id);

            // NEW: Release the player's reserved arena spawn.
            if (slots.TryGetValue(id, out int slot))
            {
                if (arenaSpawnManager != null)
                {
                    arenaSpawnManager.ReleaseSpawn(slot);
                }

                slots.Remove(id);
            }

            assignedSpawns.Remove(id);

            Debug.Log("CC_DISCONNECTED " + id);
            if (Phase == SessionPhase.Leaving) return;
            if (!Manager.IsServer && id == Manager.LocalClientId)
            {
                string reason = Manager.DisconnectReason == "Session full: five players maximum." ?
                    "Session full: five players maximum. Wait for a free place or join another host." :
                    "Session ended or joining failed. Check the host, network and that everyone uses the same build, then reconnect.";
                StopSession(reason, true);
            }
        }

        // =====================================================
        // TRANSPORT FAILURE
        // =====================================================

        void TransportFailed()
        {
            if (Phase != SessionPhase.Leaving)
                StopSession("Network connection failed. Check Wi-Fi/internet and network permissions, then reconnect.", true);
        }

        // =====================================================
        // UPDATED: LEAVE SESSION
        // =====================================================

        public void Leave()
        {
            if (Phase != SessionPhase.Leaving)
                StopSession("Disconnected. Choose Host or Join to start again.", false);
        }

        void StopSession(string message, bool failed)
        {
            if (Phase == SessionPhase.Leaving) return;
            attempt++; // Invalidates every pending Relay await, including timeout and cancel.
            phaseAfterStop = failed ? SessionPhase.Failed : SessionPhase.Idle;
            Phase = SessionPhase.Leaving;
            Status = message;
            JoinCode = "";
            stopFrame = Time.frameCount;
            Manager.Shutdown();
        }

        void CompleteStop()
        {
            // Stay in Leaving while player callbacks run; restart is safe only after cleanup.
            foreach (ulong id in new List<ulong>(players)) UnregisterAvatar(id);
            admitted.Clear();
            ClearSpawnReservations();
            if (FoundationRig.Instance != null) FoundationRig.Instance.ResetToLobby();
            Phase = phaseAfterStop;
            if (!endedRaised)
            {
                endedRaised = true;
                SessionEnded?.Invoke(Status);
            }
        }

        void OnDestroy()
        {
            attempt++;

            if (Manager != null)
            {
                Manager.ConnectionApprovalCallback -= Approve;
                Manager.OnClientConnectedCallback -= Connected;
                Manager.OnClientDisconnectCallback -= Disconnected;
                Manager.OnTransportFailure -= TransportFailed;
            }

            if (Instance == this)
                Instance = null;
        }
    }
}