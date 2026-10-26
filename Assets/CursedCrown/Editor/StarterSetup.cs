using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering;

namespace CursedCrown.Editor
{
    public static class StarterSetup
    {
        public const string ScenePath="Assets/CursedCrown/Scenes/Arena.unity";
        public const string PlayerPath="Assets/CursedCrown/Prefabs/FoundationPlayer.prefab";

        [MenuItem("Cursed Crown/1 - Generate Foundation (replaces open scene)")]
        public static void CreateScene()
        {
            if(!Application.isBatchMode&&!FoundationAutomation.Running&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory("Assets/CursedCrown/Materials");
            Directory.CreateDirectory("Assets/CursedCrown/Prefabs");
            Directory.CreateDirectory("Assets/CursedCrown/Scenes");
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.52f,.62f);
            var light=new GameObject("Directional Light").AddComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.None;
            var stone=Material("Stone",new Color(.23f,.28f,.35f));
            var floor=Material("Floor",new Color(.11f,.14f,.19f));
            var gold=Material("Crown Gold",new Color(1,.68f,.12f));
            var blue=Material("Spawn Blue",new Color(.12f,.55f,.85f));
            var arena=new GameObject("ARENA - Person 2 owns geometry").transform;
            Box(arena,"Floor",new Vector3(0,-.1f,0),new Vector3(20,.2f,20),floor);
            Box(arena,"North Wall",new Vector3(0,2,10),new Vector3(20,4,.3f),stone);
            Box(arena,"South Wall",new Vector3(0,2,-10),new Vector3(20,4,.3f),stone);
            Box(arena,"East Wall",new Vector3(10,2,0),new Vector3(.3f,4,20),stone);
            Box(arena,"West Wall",new Vector3(-10,2,0),new Vector3(.3f,4,20),stone);
            var spawns=new GameObject("Spawn Points").transform;spawns.SetParent(arena);
            for(int i=0;i<GameRules.MaxPlayers;i++)
            {
                float angle=i*Mathf.PI*2/GameRules.MaxPlayers;
                var point=new Vector3(Mathf.Sin(angle)*8,.12f,Mathf.Cos(angle)*8);
                var spawn=new GameObject("Spawn_"+(i+1)).transform;spawn.SetParent(spawns);spawn.position=point;
                spawn.rotation=Quaternion.LookRotation(-new Vector3(point.x,0,point.z));
                var marker=Box(arena,"Spawn Marker "+(i+1),new Vector3(point.x,.015f,point.z),new Vector3(1,.03f,1),blue);
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                var cover=Box(arena,"Cover "+(i+1),new Vector3(Mathf.Sin(angle)*4,.75f,Mathf.Cos(angle)*4),new Vector3(2,1.5f,1),stone);
                cover.transform.rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
            Box(arena,"Pedestal",new Vector3(0,.5f,0),new Vector3(1.2f,1,1.2f),stone);
            var placeholder=Box(arena,"Crown Visual Placeholder - Person 5 replaces",new Vector3(0,1.35f,0),new Vector3(.4f,.25f,.4f),gold);
            UnityEngine.Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            var rigObject=new GameObject("LOCAL XR RIG - never network spawn cameras");
            rigObject.transform.position=new Vector3(0,0,-8);
            var rig=rigObject.AddComponent<FoundationRig>();
            var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.transform.SetParent(rigObject.transform,false);
            camera.transform.localPosition=Vector3.up*1.65f;camera.AddComponent<Camera>().nearClipPlane=.05f;camera.AddComponent<AudioListener>();
            rig.Head=camera.transform;
            rig.LeftHand=Node(rigObject.transform,"Left Controller",new Vector3(-.25f,1.2f,.35f));
            rig.RightHand=Node(rigObject.transform,"Right Controller",new Vector3(.25f,1.2f,.35f));
            var player=new GameObject("FoundationPlayer");player.AddComponent<NetworkObject>();
            var avatar=player.AddComponent<FoundationAvatar>();
            avatar.Head=Visual(player.transform,"Head",new Vector3(0,1.65f,0),new Vector3(.22f,.22f,.22f),blue);
            avatar.LeftHand=Visual(player.transform,"LeftHand",new Vector3(-.25f,1.2f,.35f),new Vector3(.08f,.08f,.16f),blue);
            avatar.RightHand=Visual(player.transform,"RightHand",new Vector3(.25f,1.2f,.35f),new Vector3(.08f,.08f,.16f),blue);
            avatar.Body=Visual(player.transform,"Body",new Vector3(0,.8f,0),new Vector3(.35f,.75f,.2f),blue);
            var prefab=PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);UnityEngine.Object.DestroyImmediate(player);
            var sessionObject=new GameObject("SESSION FOUNDATION - Person 1 extends");
            var transport=sessionObject.AddComponent<UnityTransport>();
            var manager=sessionObject.AddComponent<NetworkManager>();
            manager.NetworkConfig ??= new Unity.Netcode.NetworkConfig();
            manager.NetworkConfig.NetworkTransport=transport;
            manager.NetworkConfig.PlayerPrefab=prefab;
            manager.NetworkConfig.ConnectionApproval=true;
            manager.NetworkConfig.EnableSceneManagement=false;
            manager.NetworkConfig.TickRate=30;
            var session=sessionObject.AddComponent<FoundationSession>();
            sessionObject.AddComponent<FoundationMenu>().Session=session;
            sessionObject.AddComponent<FoundationSmokeProbe>();
            var panel=new GameObject("VR Lobby Controls");panel.transform.position=new Vector3(0,1.6f,-5.5f);
            var worldMenu=panel.AddComponent<FoundationWorldMenu>();worldMenu.Session=session;
            worldMenu.MenuMaterial=Material("UI Unlit",new Color(.07f,.18f,.23f),true);
            EditorSceneManager.SaveScene(scene,ScenePath);
            for(int person=1;person<=5;person++)
            {
                string folder="Assets/CursedCrown/Tests/Person"+person;Directory.CreateDirectory(folder);
                string sandbox=folder+"/Person"+person+"_Sandbox.unity";
                if(!File.Exists(sandbox))EditorSceneManager.SaveScene(scene,sandbox,true);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="The Cursed Crown";
            PlayerSettings.companyName="Cursed Crown Team";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.cursedcrown.vr");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.runInBackground=true;
            EditorSettings.serializationMode=SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("CC_FOUNDATION_GENERATED: real scene and network player prefab saved.");
        }
        [MenuItem("Cursed Crown/2 - Validate Foundation")]
        public static void Validate()
        {
            if(!Application.isBatchMode&&!FoundationAutomation.Running&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Validation cancelled to preserve scene edits.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if(prefab==null||prefab.GetComponent<NetworkObject>()==null||prefab.GetComponent<FoundationAvatar>()==null)
                throw new InvalidOperationException("Missing network player prefab or required components.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var sessions=UnityEngine.Object.FindObjectsByType<FoundationSession>(FindObjectsSortMode.None);
            if(sessions.Length!=1)throw new InvalidOperationException("Expected one session manager.");
            var manager=sessions[0].GetComponent<NetworkManager>();
            if(manager.NetworkConfig.PlayerPrefab!=prefab||manager.NetworkConfig.NetworkTransport==null)
                throw new InvalidOperationException("Network prefab/transport is not wired.");
            if(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length!=1||
               UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1)
                throw new InvalidOperationException("Expected exactly one local camera/audio listener.");
            if(UnityEngine.Object.FindObjectsByType<FoundationRig>(FindObjectsSortMode.None).Length!=1)
                throw new InvalidOperationException("Expected one local tracking rig.");
            if(!manager.NetworkConfig.ConnectionApproval||manager.NetworkConfig.EnableSceneManagement)
                throw new InvalidOperationException("Foundation network configuration is incorrect.");
            foreach(var root in scene.GetRootGameObjects())
                foreach(var component in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject)>0)
                        throw new InvalidOperationException("Missing script on "+component.name);
            Debug.Log("CC_FOUNDATION_VALIDATION_PASS");
        }
        [MenuItem("Cursed Crown/3 - Build Desktop Test Player")]
        public static void BuildDesktop()
        {
            Validate();Directory.CreateDirectory(Path.GetDirectoryName(DesktopTestPlayer.BuildPath));
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},
                locationPathName=DesktopTestPlayer.BuildPath,target=DesktopTestPlayer.Target,
                options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Desktop build failed: "+report.summary.result);
            Debug.Log("CC_DESKTOP_BUILD_PASS");
        }
        static Transform Node(Transform parent,string name,Vector3 position)
        {var node=new GameObject(name).transform;node.SetParent(parent,false);node.localPosition=position;return node;}
        static Transform Visual(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var box=Box(parent,name,position,scale,material);UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());return box.transform;}
        static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent,false);box.transform.localPosition=position;box.transform.localScale=scale;box.GetComponent<Renderer>().sharedMaterial=material;return box;}
        static Material Material(string name,Color color,bool unlit=false)
        {string path="Assets/CursedCrown/Materials/"+name.Replace(" ","")+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.color=color;return material;}
    }
}
