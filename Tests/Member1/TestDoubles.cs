// These doubles exercise the real session controller without Unity or live services.
// They do NOT validate NGO serialization, SDK compatibility, transport or headset behaviour.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public T GetComponent<T>() => (T)Components[typeof(T)];
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type a, Type b) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public static class Application { public static bool runInBackground; public static string cloudProjectId = "test-project"; }
    public static class Time { public static double realtimeSinceStartupAsDouble; public static int frameCount; }
    public static class Debug
    {
        public static string LastWarning;
        public static void Log(string message) { }
        public static void LogWarning(string message) { LastWarning = message; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator -(Vector3 p) => new Vector3(-p.x, -p.y, -p.z);
    }
    public struct Quaternion { public static Quaternion LookRotation(Vector3 direction) => new Quaternion(); }
    public struct Pose
    {
        public Vector3 position; public Quaternion rotation;
        public Pose(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
    }
}
namespace Unity.Netcode
{
    public class NetworkManager
    {
        public class Config
        {
            public bool ConnectionApproval, EnableSceneManagement;
            public uint TickRate; public int ClientConnectionBufferTimeout;
        }
        public class ConnectionApprovalRequest { public ulong ClientNetworkId; }
        public class ConnectionApprovalResponse { public bool Approved, CreatePlayerObject, Pending; public string Reason; }
        public readonly Config NetworkConfig = new Config();
        public event Action<ConnectionApprovalRequest, ConnectionApprovalResponse> ConnectionApprovalCallback;
        public event Action<ulong> OnClientConnectedCallback, OnClientDisconnectCallback;
        public event Action OnTransportFailure;
        public bool IsListening, ShutdownInProgress, IsServer, IsHost, IsConnectedClient;
        public bool StartSucceeds = true;
        public ulong LocalClientId;
        public string DisconnectReason = "";
        public int HostStarts, ClientStarts, Shutdowns;
        public bool StartHost()
        {
            HostStarts++;
            if (!StartSucceeds) return false;
            IsListening = IsServer = IsHost = IsConnectedClient = true;
            LocalClientId = 0; OnClientConnectedCallback?.Invoke(0); return true;
        }
        public bool StartClient()
        {
            ClientStarts++;
            if (!StartSucceeds) return false;
            IsListening = true; LocalClientId = 7; return true;
        }
        public void Connect() { IsConnectedClient = true; OnClientConnectedCallback?.Invoke(LocalClientId); }
        public void Disconnect(string reason = "") { DisconnectReason = reason; OnClientDisconnectCallback?.Invoke(LocalClientId); }
        public void FailTransport() => OnTransportFailure?.Invoke();
        public void Shutdown() { Shutdowns++; ShutdownInProgress = true; }
        public void FinishShutdown() { IsListening = IsServer = IsHost = IsConnectedClient = ShutdownInProgress = false; }
        public ConnectionApprovalResponse Approve(ulong id)
        {
            var response = new ConnectionApprovalResponse();
            ConnectionApprovalCallback?.Invoke(new ConnectionApprovalRequest { ClientNetworkId = id }, response);
            return response;
        }
    }
}
namespace Unity.Netcode.Transports.UTP
{
    public class UnityTransport
    {
        public string Address, Listen, Mode; public ushort Port;
        public void SetConnectionData(string address, ushort port, string listen)
        { Address = address; Port = port; Listen = listen; Mode = "lan"; }
        public void SetRelayServerData(object data) { Mode = "relay"; }
    }
}
namespace Unity.Services.Core
{
    public static class UnityServices
    {
        public static int Calls;
        public static Func<Task> Initialize = () => Task.CompletedTask;
        public static Task InitializeAsync() { Calls++; return Initialize(); }
    }
}
namespace Unity.Services.Authentication
{
    public sealed class AuthenticationService
    {
        public static AuthenticationService Instance = new AuthenticationService();
        public bool IsSignedIn; public int SignIns;
        public void SwitchProfile(string profile) { }
        public Task SignInAnonymouslyAsync() { IsSignedIn = true; SignIns++; return Task.CompletedTask; }
    }
}
namespace Unity.Services.Relay.Models
{
    public sealed class Allocation { public Guid AllocationId = Guid.NewGuid(); }
    public sealed class JoinAllocation { }
}
namespace Unity.Services.Relay
{
    using Unity.Services.Relay.Models;
    public sealed class RelayService
    {
        public static RelayService Instance = new RelayService();
        public int Capacity, HostAllocations; public string Code;
        public Func<Task<Allocation>> Allocate = () => Task.FromResult(new Allocation());
        public Task<Allocation> CreateAllocationAsync(int capacity) { Capacity = capacity; HostAllocations++; return Allocate(); }
        public Task<string> GetJoinCodeAsync(Guid id) => Task.FromResult("ABC123");
        public Task<JoinAllocation> JoinAllocationAsync(string code) { Code = code; return Task.FromResult(new JoinAllocation()); }
    }
    public static class AllocationUtils
    {
        public static string LastProtocol;
        public static object ToRelayServerData(object allocation, string protocol) { LastProtocol = protocol; return allocation; }
    }
}
namespace CursedCrown
{
    public sealed class ArenaSpawnManager
    {
        public bool TryReserveSpawn(int slot, out UnityEngine.Vector3 position, out UnityEngine.Quaternion rotation)
        {
            position = new UnityEngine.Vector3();
            rotation = new UnityEngine.Quaternion();
            return false;
        }
        public void ReleaseSpawn(int slot) { }
    }

    public sealed class FoundationRig
    {
        public static FoundationRig Instance = new FoundationRig();
        public int Resets;
        public void ResetToLobby() { Resets++; }
    }
}

