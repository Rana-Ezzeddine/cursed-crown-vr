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
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class FoundationSession : MonoBehaviour
    {
        public static FoundationSession Instance { get; private set; }
        public NetworkManager Manager { get; private set; }
        public string Address = "127.0.0.1";
        public ushort Port = 7777;
        public string JoinCode { get; private set; } = "";
        public string Status { get; private set; } = "Choose Host or Join. This is the foundation sandbox.";
        public bool Busy { get; private set; }
        public event Action<ulong> PlayerJoined;
        public event Action<ulong> PlayerLeft;
        public event Action<string> SessionEnded;
        public int PlayerCount => Manager != null && Manager.IsListening ?
            UnityEngine.Object.FindObjectsByType<FoundationAvatar>(FindObjectsSortMode.None).Length : 0;
        readonly HashSet<ulong> admitted = new();
        readonly Dictionary<ulong, int> slots = new();
        UnityTransport transport;
        bool intentionalLeave;
        int attempt;

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

        public bool StartLan(bool host)
        {
            if (Busy || Manager.IsListening || Manager.ShutdownInProgress) return false;
            intentionalLeave = false;
            admitted.Clear(); slots.Clear(); JoinCode = "";
            transport.SetConnectionData(Address.Trim(), Port, host ? "0.0.0.0" : null);
            bool started = host ? Manager.StartHost() : Manager.StartClient();
            Status = started ? (host ? "Hosting LAN on port " + Port : "Connecting to " + Address + "...") : "Connection could not start.";
            return started;
        }

        public async void StartRelay(bool host, string code = "")
        {
            if (Busy || Manager.IsListening || Manager.ShutdownInProgress) return;
            int currentAttempt = ++attempt;
            Busy = true; intentionalLeave = false;
            Status = "Connecting to Unity Relay...";
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (currentAttempt != attempt || this == null) return;
                if (host)
                {
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameRules.MaxPlayers - 1);
                    string createdCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    if (currentAttempt != attempt || this == null) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    admitted.Clear(); slots.Clear();
                    if (!Manager.StartHost()) throw new InvalidOperationException("Relay host could not start.");
                    JoinCode = createdCode;
                    Status = "Relay host ready. Share code " + JoinCode;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Enter the host's Relay code.");
                    JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());
                    if (currentAttempt != attempt || this == null) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    if (!Manager.StartClient()) throw new InvalidOperationException("Relay client could not start.");
                    Status = "Joining Relay session...";
                }
            }
            catch (Exception ex)
            {
                if (currentAttempt == attempt && this != null)
                {
                    Status = "Relay unavailable: " + ex.Message + " Check project linking and network access, or use LAN.";
                    Debug.LogWarning(Status);
                    if (Manager.IsListening) Manager.Shutdown();
                }
            }
            finally { if (this != null && currentAttempt == attempt) Busy = false; }
        }

        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool same = admitted.Contains(request.ClientNetworkId);
            bool allowed = same || admitted.Count < GameRules.MaxPlayers;
            response.Approved = allowed;
            response.CreatePlayerObject = allowed;
            response.Pending = false;
            response.Reason = allowed ? "" : "Session full: five players maximum.";
            if (allowed) admitted.Add(request.ClientNetworkId);
        }

        public Pose AssignSpawn(ulong id)
        {
            if (!Manager.IsServer) throw new InvalidOperationException("Only the host assigns spawn slots.");
            if (!slots.TryGetValue(id, out int slot))
            {
                slot = 0;
                while (slots.ContainsValue(slot) && slot < GameRules.MaxPlayers) slot++;
                if (slot >= GameRules.MaxPlayers) throw new InvalidOperationException("No free foundation spawn.");
                slots[id] = slot;
            }
            float angle = slot * Mathf.PI * 2 / GameRules.MaxPlayers;
            var position = new Vector3(Mathf.Sin(angle) * 8f, .12f, Mathf.Cos(angle) * 8f);
            return new Pose(position, Quaternion.LookRotation(-new Vector3(position.x, 0, position.z)));
        }

        void Connected(ulong id)
        {
            if (id == Manager.LocalClientId) Status = Manager.IsHost ? "Hosting. Players can join." : "Connected to host.";
            PlayerJoined?.Invoke(id);
            Debug.Log("CC_CONNECTED " + id);
        }
        void Disconnected(ulong id)
        {
            admitted.Remove(id); slots.Remove(id);
            PlayerLeft?.Invoke(id);
            Debug.Log("CC_DISCONNECTED " + id);
            if (!Manager.IsServer && id == Manager.LocalClientId && !intentionalLeave)
            {
                Status = string.IsNullOrWhiteSpace(Manager.DisconnectReason) ?
                    "Session ended: host left, connection was lost, or joining failed." : Manager.DisconnectReason;
                SessionEnded?.Invoke(Status);
            }
        }
        void TransportFailed()
        {
            Status = "Network transport failed. Leave and reconnect; check network access.";
            SessionEnded?.Invoke(Status);
        }
        public void Leave()
        {
            attempt++; Busy = false; intentionalLeave = true;
            Manager.Shutdown(); admitted.Clear(); slots.Clear(); JoinCode = "";
            Status = "Disconnected. Choose Host or Join to start again.";
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
            if (Instance == this) Instance = null;
        }
    }
}
