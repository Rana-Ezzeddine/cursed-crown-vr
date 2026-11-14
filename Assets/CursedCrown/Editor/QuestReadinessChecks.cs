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
            foreach(var path in System.IO.Directory.GetFiles("Assets/CursedCrown/Resources/TempleTextures", "*.jpg"))
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
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
                    var characterAvatar=MatchController.FindAvatar(match.NetworkManager.LocalClientId);
                    for(int choice=0;choice<TempleCharacter.Names.Length;choice++)
                    {
                        Invoke(characterAvatar,"HandleCharacterChoice",characterAvatar.OwnerClientId,choice);
                        Check(characterAvatar.CharacterChoice.Value==choice,"Character selection accepted: "+TempleCharacter.Names[choice]);
                    }
                    Invoke(characterAvatar,"HandleCharacterChoice",characterAvatar.OwnerClientId,-1);
                    Check(characterAvatar.CharacterChoice.Value==4,"Invalid character rejected");
                    Invoke(characterAvatar,"HandleCharacterChoice",characterAvatar.OwnerClientId+1,0);
                    Check(characterAvatar.CharacterChoice.Value==4,"Other player cannot change character");
                    Check(characterAvatar.GetComponent<TempleCharacter>()!=null,"Human character visuals created");
                    Check(MatchController.CleanName("  Rana  ")=="Rana","Player names trim whitespace");
                    Check(MatchController.CleanName("<b>Rana</b>\n").IndexOf('<')<0,"Player names cannot inject rich text");
                    Check(MatchController.CleanName(new string('A',50)).Length==16,"Player names limited to 16 characters");
                    Invoke(match,"HandlePlayerName",manager.LocalClientId,"Rana");
                    Check(match.PlayerName(manager.LocalClientId)=="Rana","Name published to network roster");
                    Invoke(match,"HandlePlayerName",manager.LocalClientId,"   ");
                    Check(match.PlayerName(manager.LocalClientId)=="Rana","Blank name cannot erase player identity");
                    Invoke(match,"HandlePlayerName",manager.LocalClientId+100,"Intruder");
                    bool intruder=false;foreach(var row in match.Scores)if(row.Name.ToString()=="Intruder")intruder=true;
                    Check(!intruder,"Unknown sender cannot register a name");
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
                    typeof(MatchPresentation).GetField("editingName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(presentation,true);
                    typeof(MatchPresentation).GetField("draftName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(presentation,"RANA");
                    Invoke(presentation,"Update");CaptureArtPreview(true);
                    if(System.IO.File.Exists("/tmp/cursed-crown-menu-preview.png"))System.IO.File.Copy("/tmp/cursed-crown-menu-preview.png","/tmp/cursed-crown-name-keyboard.png",true);
                    var saveButton=GameObject.Find("KEY SAVE NAME").transform;
                    rig.RightHand.position=saveButton.position-saveButton.forward*1.5f;rig.RightHand.rotation=saveButton.rotation;
                    Flag(rig,"TriggerPressed",true);Invoke(presentation,"Update");
                    Check(match.PlayerName(manager.LocalClientId)=="RANA","Tracked trigger saves name from VR keyboard");
                    Flag(rig,"TriggerPressed",false);Invoke(presentation,"Update");
                    Invoke(match,"HandlePlayerName",manager.LocalClientId,"Rana");
                    var readyButton = GameObject.Find("READY / NOT READY").transform;
                    rig.RightHand.position = readyButton.position - readyButton.forward * 1.5f;
                    var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obstruction.transform.position = readyButton.position - readyButton.forward * .75f;
                    obstruction.transform.localScale = Vector3.one * .25f;
                    rig.RightHand.rotation = readyButton.rotation;
                    Physics.SyncTransforms();
                    Flag(rig, "TriggerPressed", true); Invoke(presentation, "Update");
                    bool isReady = false;
                    foreach (var row in match.Scores)
                        if (row.Id == manager.LocalClientId) isReady = row.Ready;
                    Check(isReady, "Tracked trigger selects Ready through unrelated physics obstruction from 1.5 metres");
                    obstruction.SetActive(false); UnityEngine.Object.Destroy(obstruction);
                    Flag(rig,"TriggerPressed",false); Invoke(presentation,"Update");
                    CaptureArtPreview(true);

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
                    int walkable = 0;
                    for (int x = -13; x <= 13; x += 2)
                        for (int z = -13; z <= 13; z += 2)
                            if (teleport.IsServerValidDestination(new Vector3(x, 0, z), avatar)) walkable++;
                    Check(walkable > 100, "Temple exposes over 100 clear sampled floor points");
                    Check(teleport.IsServerValidDestination(new Vector3(10,0,10),avatar), "Temple chamber interior is walkable");
                    Check(teleport.IsServerValidDestination(new Vector3(10,0,7),avatar), "Temple front doorway is walkable");
                    Check(teleport.IsServerValidDestination(new Vector3(7,0,10),avatar), "Temple side escape doorway is walkable");
                    Check(!teleport.IsServerValidDestination(new Vector3(8,0,7),avatar), "Temple wall blocks teleport destinations");
                    Check(Physics.Raycast(new Vector3(8,1,5),Vector3.forward,out var coverHit,4) &&
                        coverHit.collider.name=="Chamber front pier", "Temple wall provides physical cover against shots");
                    Check(!teleport.IsServerValidDestination(new Vector3(16, 0, 0), avatar), "Outside arena rejected");
                    Check(!teleport.IsServerValidDestination(Vector3.zero, avatar), "Pedestal remains solid");
                    Check(teleport.TryResolveLanding(new Vector3(.7f,0,0),new Vector3(2,1.3f,0),avatar,out var assisted) &&
                        teleport.IsServerValidDestination(assisted,avatar) && Vector3.Distance(assisted,new Vector3(.7f,0,0))<=.751f,
                        "Near-cover aim assistance resolves to nearby safe floor");
                    teleport.BuildLandingHints();
                    Check(teleport.LandingHintCount >= 50, "At least 50 safe teleport guidance locations");
                    Debug.Log("TELEPORT_HINT_COUNT: "+teleport.LandingHintCount);
                    for(int side=0;side<4;side++)
                    {
                        var rotationToRoom=Quaternion.Euler(0,side*90,0);
                        foreach(var point in new[]{new Vector3(10,0,7),new Vector3(7,0,10),new Vector3(10,0,10)})
                            Check(teleport.IsServerValidDestination(rotationToRoom*point,avatar), "Safe teleport access in chamber "+(side+1));
                    }
                    Check(typeof(FoundationAvatar).GetMethod("MoveInputRpc",BindingFlags.Instance|BindingFlags.NonPublic)==null &&
                        typeof(FoundationAvatar).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic)==null,
                        "Continuous walking input and server movement removed");
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects lobby teleport");
                    match.RoundSeconds = 2;
                    Check(match.TryStartMatch(), "Host starts ready match");
                    Invoke(match,"HandlePlayerName",manager.LocalClientId,"Changed mid-round");
                    Check(match.PlayerName(manager.LocalClientId)=="Rana","Names locked during play");
                    int selectedCharacter=avatar.CharacterChoice.Value;
                    Invoke(avatar,"HandleCharacterChoice",avatar.OwnerClientId,(selectedCharacter+1)%TempleCharacter.Names.Length);
                    Check(avatar.CharacterChoice.Value==selectedCharacter,"Character selection locked during play");
                    Flag(rig, "IsTracked", true); Invoke(presentation, "Update");
                    var panel = GameObject.Find("Match controls");
                    Check(panel.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
                        panel.GetComponentsInChildren<Collider>().All(c => !c.enabled), "Match controls disappear and stop intercepting input during play");
                    CaptureArtPreview();
                    Flag(rig, "IsTracked", false);
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId + 1, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects wrong-owner teleport");
                    health.Alive.Value = false;
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == root, "Server rejects dead-player teleport");
                    health.Alive.Value = true;
                    Invoke(avatar, "HandleTeleportRequest", manager.LocalClientId, destination);
                    Check(avatar.RootPosition.Value == destination, "Server accepts living-player teleport during play");
                    // Verify headset-offset clearance and snap turns with teleport-only locomotion.
                    avatar.RootPosition.Value = new Vector3(10, 0, 10);
                    var pose = avatar.TrackedPose.Value;
                    pose.Head = new Vector3(.4f, 1.65f, .2f);
                    avatar.TrackedPose.Value = pose;
                    avatar.transform.SetPositionAndRotation(avatar.RootPosition.Value, avatar.RootRotation.Value);
                    Physics.SyncTransforms();
                    var feet = avatar.RootPosition.Value + avatar.HeadOffset;
                    Check(teleport.IsServerValidDestination(feet, avatar), "Own avatar does not obstruct standing destination");
                    var pivot = avatar.RootPosition.Value + avatar.HeadOffset;
                    var rotation = avatar.RootRotation.Value;
                    Invoke(avatar, "HandleSnapTurn", manager.LocalClientId, 1);
                    Check(Mathf.Abs(Quaternion.Angle(rotation, avatar.RootRotation.Value) - 30) < .01f &&
                        Vector3.Distance(pivot, avatar.RootPosition.Value + avatar.HeadOffset) < .001f,
                        "Snap turn rotates 30 degrees around the headset");
                    // Verify the complete teleport-to-crown interaction, not just floor clearance.
                    pose.Head=new Vector3(0,1.65f,0);pose.Right=new Vector3(.25f,1.35f,.35f);pose.RightTracked=true;
                    avatar.TrackedPose.Value=pose;
                    for(int i=0;i<8;i++)
                    {
                        float angle=i*Mathf.PI/4, radius=i%2==0?1.15f:1.3f;
                        var approach=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius;
                        avatar.RootRotation.Value=Quaternion.LookRotation(-approach,Vector3.up);
                        Invoke(avatar,"HandleTeleportRequest",manager.LocalClientId,approach);
                        Check(Vector3.Distance(avatar.RootPosition.Value,approach)<.001f,"Crown approach "+i+" accepts teleport");
                        Invoke(match,"HandlePickup",manager.LocalClientId,match.RoundId);
                        Check(match.Snapshot.Carried && match.Snapshot.Carrier==manager.LocalClientId,
                            "Crown pickup reachable from approach "+i+" without physical steps");
                        match.NotifyPlayerDied(manager.LocalClientId,match.RoundId);
                        typeof(MatchController).GetField("droppedPosition",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(match,match.Pedestal);
                        Invoke(match,"Publish");
                    }
                    avatar.RootPosition.Value=new Vector3(0,0,3);avatar.RootRotation.Value=Quaternion.Euler(0,180,0);
                    Invoke(match,"HandlePickup",manager.LocalClientId,match.RoundId);
                    Check(!match.Snapshot.Carried,"Distant crown pickup remains rejected");
                    pose.Right=Quaternion.Inverse(avatar.RootRotation.Value)*(new Vector3(0,.5f,0)-avatar.RootPosition.Value);
                    avatar.TrackedPose.Value=pose;
                    Invoke(match,"HandlePickup",manager.LocalClientId,match.RoundId);
                    Check(!match.Snapshot.Carried,"Pickup from inside altar remains rejected");
                    destination = avatar.RootPosition.Value;
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
                    var rig = FoundationRig.Instance;
                    Flag(rig, "IsTracked", true);
                    Invoke(UnityEngine.Object.FindFirstObjectByType<MatchPresentation>(), "Update");
                    Check(GameObject.Find("Match controls").GetComponentsInChildren<Renderer>().All(r => r.enabled),
                        "Results controls return after round end");
                    var resultsView=UnityEngine.Object.FindFirstObjectByType<MatchPresentation>();
                    Check(match.ResultsText().Contains("Rana"),"Winner announcement uses player name");
                    // A temporary five-row fixture exercises rendering, ranking, ties and departed players.
                    match.Scores.Clear();
                    match.Scores.Add(new MatchController.PlayerRow{Id=1,Name=new Unity.Collections.FixedString64Bytes("Elio"),Score=24,Connected=true});
                    match.Scores.Add(new MatchController.PlayerRow{Id=0,Name=new Unity.Collections.FixedString64Bytes("Rana"),Score=60,Connected=true});
                    match.Scores.Add(new MatchController.PlayerRow{Id=2,Name=new Unity.Collections.FixedString64Bytes("Jad"),Score=60,Connected=true});
                    match.Scores.Add(new MatchController.PlayerRow{Id=3,Name=new Unity.Collections.FixedString64Bytes("Carla"),Score=12,Connected=true});
                    match.Scores.Add(new MatchController.PlayerRow{Id=4,Name=new Unity.Collections.FixedString64Bytes("Alex"),Score=5,Connected=false});
                    string results=(string)typeof(MatchPresentation).GetMethod("Status",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(resultsView,new object[]{match});
                    Check(results.IndexOf("Rana")<results.IndexOf("Elio") && results.Contains("1.  Jad") && results.Contains("3.  Elio"),"Leaderboard sorts scores and shares tied ranks");
                    Check(results.Contains("Alex"),"Departed players remain in leaderboard");
                    Check(match.ResultsText().Contains("Rana") && match.ResultsText().Contains("Jad"),"Tied winners use player names");
                    Invoke(resultsView,"Update");
                    Invoke(avatar.GetComponent<HealthHUD>(),"Update");
                    CaptureArtPreview(true);
                    if(System.IO.File.Exists("/tmp/cursed-crown-menu-preview.png"))System.IO.File.Copy("/tmp/cursed-crown-menu-preview.png","/tmp/cursed-crown-leaderboard.png",true);
                    var duplicate=match.Scores[1];duplicate.Name=new Unity.Collections.FixedString64Bytes("Jad");match.Scores[1]=duplicate;
                    Check(match.PlayerLabel(0)=="Jad #1" && match.PlayerLabel(2)=="Jad #3","Duplicate names have distinct player suffixes");
                    Invoke(match,"Publish");
                    Check(errors == 0, "No runtime errors");
                    Debug.Log("CC_QUEST_READINESS_PASS checks=" + checks);
                    Finish(0);
                }
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }
        static void CaptureArtPreview(bool menu = false)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var go = new GameObject("Art preview camera");
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.transform.position = new Vector3(11, 9, -11);
            camera.transform.LookAt(new Vector3(0, .5f, 0));
            if(menu)
            {
                var panel=GameObject.Find("Match controls").transform;
                camera.transform.position=panel.TransformPoint(new Vector3(.65f,-.55f,-2.7f));
                camera.transform.rotation=panel.rotation;
            }
            var target = new RenderTexture(1200, 900, 24);
            camera.targetTexture = target; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1200, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); texture.Apply();
            System.IO.File.WriteAllBytes((menu ? "/tmp/cursed-crown-menu-preview.png" : "/tmp/cursed-crown-art-preview.png"), texture.EncodeToPNG());
            if(!menu)
            {
                camera.transform.position=new Vector3(0,1.7f,-8);
                camera.transform.LookAt(new Vector3(0,2,0));camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
                System.IO.File.WriteAllBytes("/tmp/cursed-crown-temple-eye.png",texture.EncodeToPNG());
                camera.transform.position=new Vector3(.85f,1.8f,-1);
                camera.transform.LookAt(new Vector3(0,1.5f,0));camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
                System.IO.File.WriteAllBytes("/tmp/cursed-crown-detail.png",texture.EncodeToPNG());
            }
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.Destroy(texture); target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(go);
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
