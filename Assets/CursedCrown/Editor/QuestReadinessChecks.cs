using System;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedCrown.Editor
{
    // Batch invocation: -executeMethod CursedCrown.Editor.QuestReadinessChecks.Run (without -quit).
    // Uses the saved production scene and actual host; simulated controller poses only.
    [InitializeOnLoad]
    public static class QuestReadinessChecks
    {
        const string Key = "CursedCrown.QuestReadiness";
        static int stage, checks;
        static double deadline, startAfter;
        static Vector3 destination;
        static int errors;
        static QuestReadinessChecks()
        {
            if (SessionState.GetBool(Key, false))
            {
                deadline = EditorApplication.timeSinceStartup + 90;
                startAfter = EditorApplication.timeSinceStartup + 8;
                EditorApplication.update += Tick;
                Application.logMessageReceived += OnLog;
            }
        }
        public static void Run()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            if (scenes.Length != 1 || scenes[0].path != Person5ArenaSetup.Scene)
                throw new Exception("Default build must contain only the final integration scene.");
            EditorSceneManager.OpenScene(Person5ArenaSetup.Scene);
            SessionState.SetBool(Key, true);
            deadline = EditorApplication.timeSinceStartup + 90;
            startAfter = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        static void Check(bool passed, string message)
        {
            if (!passed) throw new Exception(message);
            checks++;
            Debug.Log("QUEST_CHECK_PASS: " + message);
        }
        static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        static void Flag(FoundationRig rig, string name, bool value) =>
            typeof(FoundationRig).GetProperty(name).SetValue(rig, value);
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Readiness checks timed out");
                if (!EditorApplication.isPlaying)
                {
                    // Survives compilation reloads between executeMethod and Play Mode.
                    if (EditorApplication.timeSinceStartup >= startAfter && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                        EditorApplication.isPlaying = true;
                    return;
                }
                var session = FoundationSession.Instance;
                if (session == null) return;
                if (stage == 0) { session.StartLan(true); stage = 1; return; }
                var match = MatchController.Instance;
                var manager = NetworkManager.Singleton;
                var player = manager.LocalClient?.PlayerObject;
                if (match == null || !match.IsSpawned || player == null) return;
                var avatar = player.GetComponent<FoundationAvatar>();
                var health = player.GetComponent<PlayerHealth>();
                var teleport = UnityEngine.Object.FindFirstObjectByType<ArenaTeleportation>();
                if (stage == 1)
                {
                    Check(!match.AllowMockAlive && !match.AllowDesktopPickup, "Production health and hand checks enabled");
                    var rig = FoundationRig.Instance;
                    var menu = UnityEngine.Object.FindFirstObjectByType<FoundationWorldMenu>();
                    Flag(rig, "IsTracked", true); Flag(rig, "RightTracked", true);
                    Flag(rig, "TriggerPressed", false); Flag(rig, "MenuPressed", false);
                    Invoke(menu, "Update");
                    var pointer = GameObject.Find("Controller Menu Ray").GetComponent<LineRenderer>();
                    Check(pointer.enabled, "Pointer remains visible after Host LAN while session menu is hidden");
                    Check(!menu.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "Connected session menu remains hidden");
                    Flag(rig, "RightTracked", false); Invoke(menu, "Update");
                    Check(!pointer.enabled, "Pointer hides on controller tracking loss");
                    Flag(rig, "RightTracked", true); Invoke(menu, "Update");
                    Check(pointer.enabled, "Pointer returns after tracking recovery");
                    // Aim and click the real Ready button, then verify the authoritative roster.
                    var presentation = UnityEngine.Object.FindFirstObjectByType<MatchPresentation>();
                    Invoke(presentation, "Update");
                    var readyButton = GameObject.Find("READY / NOT READY").transform;
                    rig.RightHand.position = readyButton.position - readyButton.forward * .5f;
                    rig.RightHand.rotation = readyButton.rotation;
                    Physics.SyncTransforms();
                    Flag(rig, "TriggerPressed", true); Invoke(presentation, "Update");
                    bool isReady = false;
                    foreach (var row in match.Scores)
                        if (row.Id == manager.LocalClientId) isReady = row.Ready;
                    Check(isReady, "Tracked trigger selects Ready in real lobby");
                    Flag(rig, "TriggerPressed", false);
                    Flag(rig, "IsTracked", false); Flag(rig, "RightTracked", false);
                    var root = avatar.RootPosition.Value;
                    bool found = false;
                    for (int x = -4; x <= 4 && !found; x++)
                        for (int z = -4; z <= 4 && !found; z++)
                        {
                            var candidate = root + new Vector3(x, 0, z);
                            if (Vector3.Distance(candidate, root) > 1 && teleport.IsServerValidDestination(candidate))
                            { destination = candidate; found = true; }
                        }
                    Check(found, "Found a real clear arena destination");
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects lobby teleport");
                    match.RoundSeconds = 2;
                    Check(match.TryStartMatch(), "Host starts ready match");
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId + 1, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects wrong-owner teleport");
                    health.Alive.Value = false;
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects dead-player teleport");
                    health.Alive.Value = true;
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == destination, "Server accepts living-player teleport during play");
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, new Vector3(float.NaN, 0, 0));
                    Check(avatar.RootPosition.Value == destination, "Server still rejects non-finite destination");
                    stage = 2;
                    return;
                }
                if (stage == 2 && match.Phase == MatchPhase.Finished)
                {
                    var root = avatar.RootPosition.Value;
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, root + Vector3.right);
                    Check(avatar.RootPosition.Value == root, "Server rejects finished-round teleport");
                    Check(errors == 0, "No runtime errors");
                    Debug.Log("CC_QUEST_READINESS_PASS checks=" + checks);
                    Finish(0);
                }
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }
        static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            EditorApplication.Exit(code);
        }
    }
}
