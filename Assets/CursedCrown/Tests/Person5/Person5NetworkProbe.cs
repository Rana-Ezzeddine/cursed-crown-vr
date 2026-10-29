#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown.Tests
{
    // Explicit opt-in local test driver. Never added to a scene or release build.
    // Commands exercise real RPCs. Only the host test fixture can reposition roots
    // or simulate teammate health/placement, without changing pickup validation.
    public sealed class Person5NetworkProbe : MonoBehaviour, IPlayerState, ISpawnService, IRoundResettable
    {
        [Serializable] public class Command
        {
            public int sequence;
            public string action;
            public ulong player, shooter;
            public int count;
            public uint shot;
            public uint round;
            public bool value;
            public float seconds;
            public Vector3 position;
        }
        [Serializable] public class HealthRow { public ulong id; public int hp; public bool alive; }
        [Serializable] public class Report
        {
            public string mode, phase, result, error;
            public int ack, errors, resets, matchObjects;
            public bool connected, carried, accepted, mockAlive;
            public HealthRow[] health;
            public uint round;
            public ulong localId, carrier;
            public double remaining, deadline, serverTime;
            public float roundSeconds;
            public Vector3 crown;
            public MatchController.PlayerRow[] scores;
        }
        string prefix, mode;
        bool realHealth;
        readonly Report report = new();
        readonly HashSet<ulong> dead = new();
        FoundationSession session;
        GameObject wall;
        string placement = "invalid";
        float nextWrite, started;
        public static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return "";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (Arg("-p5Mode") == "" || (scene != "Person5_Sandbox" && scene != "Person5_HealthIntegration")) return;
            new GameObject("OPT-IN PERSON 5 TEST FIXTURE").AddComponent<Person5NetworkProbe>();
        }
        void Start()
        {
            mode = Arg("-p5Mode"); prefix = Arg("-p5Report");
            if (string.IsNullOrEmpty(prefix)) { enabled = false; return; }
            report.mode = mode;
            realHealth = Arg("-p5RealHealth") == "1";
            if (realHealth) PlayerHealth.SetSpawnService(this);
            Application.logMessageReceived += Log;
            Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
            session = FoundationSession.Instance;
            session.Port = ushort.Parse(Arg("-p5Port"));
            session.Address = "127.0.0.1";
            started = Time.realtimeSinceStartup;
            // Start on the following frame, after every bootstrap Start callback.
        }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            { report.errors++; report.error = message; }
        }
        bool launched;
        void Update()
        {
            if (!launched) { launched = true; session.StartLan(mode == "host"); }
            var path = prefix + ".command.json";
            if (File.Exists(path))
            {
                var cmd = JsonUtility.FromJson<Command>(File.ReadAllText(path));
                if (cmd.sequence > report.ack)
                {
                    report.ack = cmd.sequence;
                    try { Execute(cmd); } catch (Exception ex) { Debug.LogException(ex); }
                    report.ack = cmd.sequence;
                }
            }
            if (Time.realtimeSinceStartup >= nextWrite)
            { nextWrite = Time.realtimeSinceStartup + .1f; Write(); }
            if (Time.realtimeSinceStartup - started > 600) Quit();
        }
        void Execute(Command cmd)
        {
            var match = MatchController.Instance;
            report.accepted = false;
            switch (cmd.action)
            {
                case "ready": match.SetReady(cmd.value, cmd.round); break;
                case "pickup": match.RequestPickup(cmd.round); break;
                case "start": report.accepted = match.TryStartMatch(); break;
                case "restart": report.accepted = match.TryReturnToLobby(); break;
                case "leave": session.Leave(); break;
                case "rejoin": session.StartLan(mode == "host"); break;
                case "quit": Quit(); break;
                default:
                    if (!session.Manager.IsServer) throw new InvalidOperationException("Fixture mutation requires host.");
                    switch (cmd.action)
                    {
                        case "hit":
                            var bridge = match.GetComponent<Person5HealthBridge>();
                            if (bridge == null) throw new InvalidOperationException("Real health bridge missing.");
                            for (int i = 0; i < cmd.count; i++)
                                report.accepted = bridge.TryApplyValidatedHit(new ValidatedHit(cmd.round, cmd.shooter,
                                    cmd.player, cmd.shot + (uint)i, Vector3.zero));
                            break;
                        case "services":
                            match.AllowMockAlive = false;
                            if (!cmd.value) gameObject.SetActive(false);
                            match.RefreshServices();
                            gameObject.SetActive(true);
                            break;
                        case "duration": match.RoundSeconds = cmd.seconds; break;
                        case "move":
                            var avatar = MatchController.FindAvatar(cmd.player);
                            avatar.RootPosition.Value = cmd.position;
                            avatar.RootRotation.Value = Quaternion.identity;
                            break;
                        case "trackingRequired": match.AllowDesktopPickup = !cmd.value; break;
                        case "dead":
                            if (cmd.value) dead.Add(cmd.player); else dead.Remove(cmd.player);
                            break;
                        case "deathEvent": match.NotifyPlayerDied(cmd.player, cmd.round); break;
                        case "placement": placement = cmd.value ? "valid" : "invalid"; break;
                        case "wall":
                            if (wall != null) Destroy(wall);
                            if (cmd.value)
                            {
                                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                wall.name = "TEST ONLY reach blocker";
                                wall.transform.position = cmd.position;
                                wall.transform.localScale = new Vector3(.3f, .3f, .1f);
                            }
                            break;
                        default: throw new InvalidOperationException("Unknown test command " + cmd.action);
                    }
                    break;
            }
        }
        void Write()
        {
            var m = MatchController.Instance;
            report.connected = session.Manager.IsConnectedClient;
            report.localId = session.Manager.LocalClientId;
            report.resets = resets;
            report.matchObjects = FindObjectsByType<MatchController>(FindObjectsSortMode.None).Count(m => m.IsSpawned);
            if (m != null && m.IsSpawned)
            {
                report.phase = m.Phase.ToString(); report.round = m.RoundId; report.roundSeconds = m.RoundSeconds; report.mockAlive = m.AllowMockAlive;
                report.carried = m.Snapshot.Carried; report.carrier = m.Snapshot.Carrier;
                report.crown = m.Snapshot.CrownPosition; report.remaining = m.RemainingSeconds;
                report.deadline = m.Snapshot.Deadline; report.serverTime = session.Manager.ServerTime.Time;
                var rows = new List<MatchController.PlayerRow>();
                foreach (var row in m.Scores) rows.Add(row);
                report.scores = rows.ToArray(); report.result = m.ResultsText();
                if (realHealth)
                {
                    var healthRows = new List<HealthRow>();
                    foreach (var row in rows)
                    {
                        var h = Person5HealthBridge.FindHealth(row.Id);
                        if (h != null) healthRows.Add(new HealthRow { id = row.Id, hp = h.CurrentHealth, alive = h.IsAlive });
                    }
                    report.health = healthRows.ToArray();
                }
            }
            var temp = prefix + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(report, true));
            if (File.Exists(prefix + ".json")) File.Replace(temp, prefix + ".json", null);
            else File.Move(temp, prefix + ".json");
        }
        void Quit()
        {
            Write(); session.Leave();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }
        void OnDestroy() { Application.logMessageReceived -= Log; if (realHealth) PlayerHealth.SetSpawnService(null); }
        public bool IsAlive(ulong id) => !dead.Contains(id);
        public int GetCharge(ulong id) => 10;
        public bool TryReserveSpawn(ulong id, out Pose pose)
        { pose = default; if (!realHealth || session == null || !session.Manager.IsServer) return false;
            pose = session.AssignSpawn(id); return true; }
        public void ReleaseReservation(ulong id) { }
        public Vector3 GetValidCrownDrop(Vector3 requested) => placement == "valid" ? new Vector3(0, 1.35f, -.1f) : new Vector3(float.NaN, 0, 0);
        int resets;
        public void ResetRound() { resets++; dead.Clear(); }
    }
}
#endif
