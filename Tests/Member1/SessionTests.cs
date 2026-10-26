using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CursedCrown;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

static class SessionTests
{
    // Unity resumes awaits on its main-thread context. Drain an equivalent queue explicitly.
    sealed class MainThreadContext : SynchronizationContext
    {
        readonly Queue<Action> work = new Queue<Action>();
        public override void Post(SendOrPostCallback callback, object state) => work.Enqueue(() => callback(state));
        public void Drain() { while (work.Count > 0) work.Dequeue()(); }
    }
    static readonly MainThreadContext context = new MainThreadContext();
    static int passed;
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static void Call(FoundationSession session, string method) => typeof(FoundationSession)
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
    sealed class Fixture : IDisposable
    {
        public readonly FoundationSession Session = new FoundationSession();
        public readonly NetworkManager Manager = new NetworkManager();
        public readonly UnityTransport Transport = new UnityTransport();
        public Fixture()
        {
            Time.realtimeSinceStartupAsDouble = 0; Time.frameCount = 0;
            Application.cloudProjectId = "test-project";
            UnityServices.Initialize = () => Task.CompletedTask; UnityServices.Calls = 0;
            AuthenticationService.Instance = new AuthenticationService();
            RelayService.Instance = new RelayService(); Debug.LastWarning = "";
            FoundationRig.Instance = new FoundationRig();
            Session.Components[typeof(NetworkManager)] = Manager;
            Session.Components[typeof(UnityTransport)] = Transport;
            Call(Session, "Awake");
        }
        public void Frame(double seconds = 0) { Time.frameCount++; Time.realtimeSinceStartupAsDouble += seconds; Call(Session, "Update"); }
        public void StopFinished() { Manager.FinishShutdown(); Frame(); }
        public void Dispose() => Call(Session, "OnDestroy");
    }
    static void Test(string name, Action<Fixture> body)
    {
        using (var fixture = new Fixture()) body(fixture);
        passed++; Console.WriteLine("PASS " + name);
    }
    static int Main()
    {
        SynchronizationContext.SetSynchronizationContext(context);
        Test("IPv4 normalization and malformed destinations", f => {
            Assert(SessionInput.TryAddress(" 192.168.001.010 ", out var ip) && ip == "192.168.1.10", "normalization");
            Assert(SessionInput.TryAddress("LOCALHOST", out ip) && ip == "127.0.0.1", "localhost");
            foreach (var input in new[] { null, "", "1", "1.2.3", "1.2.3.4.5", "256.2.3.4", "1.2.-3.4", "0.0.0.0", "224.0.0.1", "255.255.255.255", "::1", "host/name" })
                Assert(!SessionInput.TryAddress(input, out _), "accepted invalid IP: " + input);
        });
        Test("Relay codes normalized; non-ASCII and punctuation rejected", f => {
            Assert(SessionInput.TryRelayCode(" ab12cd ", out var code) && code == "AB12CD", "normalization");
            foreach (var input in new[] { null, "", "AB CD", "AB!123", "\u00e9AB123", new string('A', 13) })
                Assert(!SessionInput.TryRelayCode(input, out _), "accepted invalid code");
        });
        Test("Bad client input never starts transport", f => {
            f.Session.Address = "bad";
            Assert(!f.Session.StartLan(false) && f.Manager.ClientStarts == 0 && f.Session.CanStart, "bad address");
            f.Session.Port = 0; Assert(!f.Session.StartLan(true), "zero port");
        });
        Test("Host binds all interfaces without using client address input", f => {
            f.Session.Address = "bad"; Assert(f.Session.StartLan(true), "host start");
            Assert(f.Transport.Listen == "0.0.0.0" && f.Session.Phase == SessionPhase.Connected, "host binding/state");
        });
        Test("Pending client rejects duplicate start", f => {
            f.Session.StartLan(false); Assert(f.Session.Busy && !f.Session.StartLan(false), "duplicate accepted");
            Assert(f.Manager.ClientStarts == 1, "started twice"); f.Manager.Connect();
            Assert(f.Session.Phase == SessionPhase.Connected && !f.Session.Busy, "connect transition");
        });
        Test("Failed start shuts down and becomes retryable", f => {
            f.Manager.StartSucceeds = false; Assert(!f.Session.StartLan(true), "false start");
            Assert(!f.Session.CanStart, "retry during shutdown"); f.StopFinished();
            Assert(f.Session.CanStart && f.Session.Phase == SessionPhase.Failed, "no retry after failure");
        });
        Test("Player events and counts are idempotent on every peer", f => {
            int joins = 0, leaves = 0; f.Session.PlayerJoined += _ => joins++; f.Session.PlayerLeft += _ => leaves++;
            f.Session.RegisterAvatar(0); f.Session.RegisterAvatar(1); f.Session.RegisterAvatar(1);
            f.Session.UnregisterAvatar(1); f.Session.UnregisterAvatar(1);
            Assert(joins == 2 && leaves == 1 && f.Session.PlayerCount == 1, "duplicate/missing event");
        });
        Test("Five-player cap and released admission", f => {
            for (ulong i = 0; i < 5; i++) Assert(f.Manager.Approve(i).Approved, "slot rejected");
            Assert(!f.Manager.Approve(5).Approved, "sixth accepted");
            Assert(f.Manager.Approve(1).Approved, "duplicate changed admission");
            f.Session.UnregisterAvatar(1); Assert(f.Manager.Approve(5).Approved, "slot not released");
        });
        Test("Spawn slots are distinct, stable and reused after leaving", f => {
            f.Session.StartLan(true); var first = f.Session.AssignSpawn(0).position;
            for (ulong i = 1; i < 5; i++) {
                var p = f.Session.AssignSpawn(i).position;
                Assert(p.x != first.x || p.z != first.z, "duplicate spawn");
            }
            bool rejected = false; try { f.Session.AssignSpawn(5); } catch (InvalidOperationException) { rejected = true; }
            Assert(rejected, "sixth spawn accepted"); f.Session.UnregisterAvatar(0);
            var reused = f.Session.AssignSpawn(5).position;
            Assert(reused.x == first.x && reused.z == first.z, "spawn not released");
        });
        Test("Clients cannot assign authoritative spawns", f => {
            bool rejected = false; try { f.Session.AssignSpawn(1); } catch (InvalidOperationException) { rejected = true; }
            Assert(rejected, "client assigned spawn");
        });
        Test("Leave waits for completed shutdown and returns rig to lobby", f => {
            f.Session.StartLan(true); f.Session.RegisterAvatar(0); int ends = 0; f.Session.SessionEnded += _ => ends++;
            f.Session.Leave(); f.Session.Leave(); f.Frame();
            Assert(!f.Session.CanStart && ends == 0 && f.Manager.Shutdowns == 1, "early/duplicate cleanup");
            f.StopFinished(); f.Frame();
            Assert(f.Session.CanStart && ends == 1 && f.Session.PlayerCount == 0 && FoundationRig.Instance.Resets == 1, "cleanup");
            Assert(f.Session.StartLan(true), "cannot host again");
        });
        Test("Connect watchdog prevents an endless busy screen", f => {
            f.Session.StartLan(false); f.Frame(21); Assert(f.Session.Phase == SessionPhase.Leaving, "no timeout");
            f.StopFinished(); Assert(f.Session.Phase == SessionPhase.Failed && f.Session.CanStart, "timeout not retryable");
        });
        Test("Host loss notifies exactly once and permits reconnect", f => {
            int ends = 0; f.Session.SessionEnded += _ => ends++;
            f.Session.StartLan(false); f.Manager.Connect(); f.Manager.Disconnect(); f.Manager.Disconnect();
            f.StopFinished(); f.Frame(); Assert(ends == 1 && f.Session.CanStart, "host loss handling");
        });
        Test("Capacity rejection stays understandable after cleanup", f => {
            f.Session.StartLan(false); f.Manager.Disconnect("Session full: five players maximum."); f.StopFinished();
            Assert(f.Session.Status.Contains("Session full") && f.Session.CanStart, "capacity message");
        });
        Test("Transport failure requests cleanup", f => {
            f.Session.StartLan(true); f.Manager.FailTransport(); f.StopFinished();
            Assert(f.Session.Phase == SessionPhase.Failed && f.Session.CanStart, "transport failure");
        });
        Test("Unlinked Relay fails before making a service request", f => {
            Application.cloudProjectId = ""; f.Session.StartRelay(true);
            Assert(UnityServices.Calls == 0 && f.Session.Status.Contains("linked"), "missing project guard");
        });
        Test("Invalid Relay code fails before authentication", f => {
            f.Session.StartRelay(false, "bad!"); Assert(UnityServices.Calls == 0, "invalid code reached SDK");
        });
        Test("Relay host reserves four joining places and uses DTLS", f => {
            f.Session.StartRelay(true);
            Assert(f.Session.JoinCode == "ABC123" && f.Session.Phase == SessionPhase.Connected, "relay host");
            Assert(RelayService.Instance.Capacity == 4 && AllocationUtils.LastProtocol == "dtls", "capacity/encryption");
        });
        Test("Relay client normalizes code and waits for actual connection", f => {
            f.Session.StartRelay(false, " ab12cd ");
            Assert(RelayService.Instance.Code == "AB12CD" && f.Session.Phase == SessionPhase.Connecting, "relay client");
            f.Manager.Connect(); Assert(f.Session.Phase == SessionPhase.Connected, "connected prematurely or never");
        });
        Test("A signed-out retry authenticates again", f => {
            f.Session.StartRelay(true); f.Session.Leave(); f.StopFinished();
            AuthenticationService.Instance.IsSignedIn = false; f.Session.StartRelay(true);
            Assert(AuthenticationService.Instance.SignIns == 2 && f.Manager.HostStarts == 2, "signed-out session reused stale initialization");
        });
        Test("Cancelled Relay allocation cannot start a host later", f => {
            var pending = new TaskCompletionSource<Allocation>(); RelayService.Instance.Allocate = () => pending.Task;
            f.Session.StartRelay(true); f.Session.Leave(); f.StopFinished(); pending.SetResult(new Allocation()); context.Drain();
            Assert(f.Manager.HostStarts == 0 && f.Session.JoinCode == "" && f.Session.CanStart, "stale async completion");
        });
        Test("Relay timeout invalidates delayed allocation", f => {
            var pending = new TaskCompletionSource<Allocation>(); RelayService.Instance.Allocate = () => pending.Task;
            f.Session.StartRelay(true); f.Frame(46); f.StopFinished(); pending.SetResult(new Allocation()); context.Drain();
            Assert(f.Manager.HostStarts == 0 && f.Session.Phase == SessionPhase.Failed, "timeout race");
        });
        Test("Old Relay completion cannot overwrite a newer LAN session", f => {
            var pending = new TaskCompletionSource<Allocation>(); RelayService.Instance.Allocate = () => pending.Task;
            f.Session.StartRelay(true); f.Session.Leave(); f.StopFinished(); f.Session.StartLan(true);
            pending.SetResult(new Allocation()); context.Drain();
            Assert(f.Transport.Mode == "lan" && f.Manager.HostStarts == 1 && f.Session.JoinCode == "", "old Relay overwrote LAN");
        });
        Test("Cancel then retry shares authentication already in progress", f => {
            var pending = new TaskCompletionSource<bool>(); UnityServices.Initialize = () => pending.Task;
            f.Session.StartRelay(true); f.Session.Leave(); f.StopFinished(); f.Session.StartRelay(true);
            Assert(UnityServices.Calls == 1, "racing service initialization"); pending.SetResult(true); context.Drain();
            Assert(f.Manager.HostStarts == 1 && AuthenticationService.Instance.SignIns == 1, "stale retry/duplicate sign-in");
        });
        Test("Destroy invalidates pending service work", f => {
            var pending = new TaskCompletionSource<Allocation>(); RelayService.Instance.Allocate = () => pending.Task;
            f.Session.StartRelay(true); Call(f.Session, "OnDestroy"); pending.SetResult(new Allocation()); context.Drain();
            Assert(f.Manager.HostStarts == 0, "start after destroy");
        });
        Test("Service errors do not expose exception contents", f => {
            UnityServices.Initialize = () => Task.FromException(new Exception("secret-token"));
            f.Session.StartRelay(true); f.StopFinished();
            Assert(!f.Session.Status.Contains("secret-token") && !Debug.LastWarning.Contains("secret-token"), "secret leaked");
        });
        int runnerTests = SmokePlanTests.RunAll();
        Console.WriteLine(passed + " session logic tests and " + runnerTests + " runner regression tests passed. Unity/NGO/Relay/headset integration still requires Unity.");
        return 0;
    }
}
