using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace CursedCrown
{
    // Opt-in harness only. Controlled players remain alive until the Editor requests stop.
    public sealed class FoundationSmokeProbe : MonoBehaviour
    {
        SmokePeerReport result;
        string path, commandPath, lastCommand;
        float started, exitAfter, nextReport, nextCommand;
        bool active, launched, reconnectRequested, reconnectStarted, finishRequested;
        float reconnectAfter, reconnectDelay;
        FoundationSession session;
        readonly System.Collections.Generic.Dictionary<ulong, Vector3> previousRemote = new();

        string Arg(string name, string fallback = "")
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }
        float Number(string name, string fallback)
        {
            return float.TryParse(Arg(name, fallback), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value : float.Parse(fallback, CultureInfo.InvariantCulture);
        }
        void Start()
        {
            string mode = Arg("-ccMode");
            if (mode != "host" && mode != "client") return;
            started = Time.realtimeSinceStartup;
            Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
            result = new SmokePeerReport { schemaVersion = 2, mode = mode, status = "Starting test player." };
            path = Arg("-ccReport", Path.Combine(Application.temporaryCachePath, "cc-test.json"));
            commandPath = Arg("-ccCommand");
            exitAfter = Number("-ccExitAfter", string.IsNullOrEmpty(commandPath) ? "25" : "900");
            reconnectAfter = Number("-ccReconnectAfter", "0");
            reconnectDelay = Number("-ccReconnectDelay", "3");
            session = GetComponent<FoundationSession>();
            session.Port = ushort.Parse(Arg("-ccPort", "17877"), CultureInfo.InvariantCulture);
            session.Address = "127.0.0.1";
            session.PlayerJoined += Joined;
            session.PlayerLeft += Left;
            session.SessionEnded += Ended;
            Application.logMessageReceived += Log;
            active = true;
            if (FoundationRig.Instance != null) FoundationRig.Instance.AutomatedMotion = true;
            WriteReport(); // A startup record exists even if later networking fails.
        }
        void Joined(ulong id)
        { result.joinedEvents++; if (id == session.Manager.LocalClientId) result.connections++; }
        void Left(ulong id) { result.leftEvents++; }
        void Ended(string reason) { result.sessionEnded = true; }
        void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) result.errors++; }

        void Update()
        {
            if (!active) return;
            if (!launched) { launched = true; session.StartLan(result.mode == "host"); }
            Sample();
            if (Time.realtimeSinceStartup >= nextCommand)
            {
                nextCommand = Time.realtimeSinceStartup + .1f;
                ReadCommand();
            }
            if (!active) return;
            if (!finishRequested && string.IsNullOrEmpty(commandPath))
            {
                if (reconnectAfter > 0 && !reconnectRequested && result.elapsed >= reconnectAfter)
                { reconnectRequested = true; session.Leave(); }
                if (reconnectRequested && !reconnectStarted && result.elapsed >= reconnectAfter + reconnectDelay && session.CanStart)
                { reconnectStarted = true; session.StartLan(false); previousRemote.Clear(); }
            }
            if (finishRequested || result.elapsed >= exitAfter) { Finish(); if (!active) return; }
            if (Time.realtimeSinceStartup >= nextReport)
            {
                nextReport = Time.realtimeSinceStartup + .5f;
                WriteReport();
            }
        }
        void Sample()
        {
            result.connectedNow = session.Manager.IsConnectedClient;
            result.connected |= result.connectedNow;
            result.currentPlayers = session.PlayerCount;
            result.peakPlayers = Mathf.Max(result.peakPlayers, result.currentPlayers);
            result.canStart = session.CanStart;
            result.status = session.Status;
            result.phase = session.Phase.ToString();
            result.elapsed = Time.realtimeSinceStartup - started;
            result.rejected |= session.Status.Contains("Session full", StringComparison.OrdinalIgnoreCase);
            var avatars = FindObjectsByType<FoundationAvatar>(FindObjectsSortMode.None);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (!avatars[i].IsSpawned) continue;
                if (result.elapsed > 3) result.validPoses &= avatars[i].TrackedPose.Value.IsValid();
                for (int j = i + 1; j < avatars.Length; j++)
                    if (avatars[j].IsSpawned && Vector3.Distance(avatars[i].RootPosition.Value, avatars[j].RootPosition.Value) < 1)
                        result.uniqueSpawns = false;
                if (!avatars[i].IsOwner)
                {
                    Vector3 head = avatars[i].TrackedPose.Value.Head;
                    if (previousRemote.TryGetValue(avatars[i].OwnerClientId, out var previous))
                        result.remoteMotion = Mathf.Max(result.remoteMotion, Vector3.Distance(previous, head));
                    previousRemote[avatars[i].OwnerClientId] = head;
                }
            }
        }
        void ReadCommand()
        {
            if (string.IsNullOrEmpty(commandPath) || !File.Exists(commandPath)) return;
            string command;
            try { command = File.ReadAllText(commandPath).Trim(); }
            catch (IOException) { return; } // The Editor may still be replacing the command.
            if (command == lastCommand) return;
            switch (command)
            {
                case "leave":
                    session.Leave(); lastCommand = command; break;
                case "join":
                    if (!session.CanStart) return; // Retry until asynchronous shutdown has completed.
                    session.StartLan(false); previousRemote.Clear(); lastCommand = command; break;
                case "stop":
                    Finish(); if (!active) lastCommand = command; break;
            }
        }
        bool WriteReport()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(result, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.LogWarning("CC_REPORT_WRITE_FAILED " + ex.GetType().Name);
                return false;
            }
        }
        void Finish()
        {
            if (!active) return;
            if (!finishRequested) { finishRequested = true; session.Leave(); }
            if (!session.CanStart) return;
            Sample();
            result.finished = true;
            if (!WriteReport()) { result.finished = false; return; }
            active = false;
            Unsubscribe();
            Application.Quit(0);
        }
        void Unsubscribe()
        {
            Application.logMessageReceived -= Log;
            if (session == null) return;
            session.PlayerJoined -= Joined;
            session.PlayerLeft -= Left;
            session.SessionEnded -= Ended;
        }
        void OnApplicationQuit() { if (active) WriteReport(); } // Unexpected quits stay finished=false.
        void OnDestroy() { if (result != null) Unsubscribe(); }
    }
}
