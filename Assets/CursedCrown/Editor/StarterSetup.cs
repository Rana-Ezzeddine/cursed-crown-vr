using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CursedCrown.Editor
{
    public static class StarterSetup
    {
        private const string ScenePath = "Assets/CursedCrown/Scenes/Arena.unity";

        [MenuItem("Cursed Crown/Create Starter Scene (replaces open scene)")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .5f, .6f);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            light.shadows = LightShadows.None;
            var stone = Material("Stone", new Color(.22f, .26f, .34f));
            var gold = Material("Crown Gold", new Color(1f, .68f, .12f));
            var blue = Material("Spawn Blue", new Color(.12f, .55f, .85f));
            Box("Floor", Vector3.zero, new Vector3(20,.2f,20), stone);
            Box("North Wall", new Vector3(0,2,10), new Vector3(20,4,.3f), stone);
            Box("South Wall", new Vector3(0,2,-10), new Vector3(20,4,.3f), stone);
            Box("East Wall", new Vector3(10,2,0), new Vector3(.3f,4,20), stone);
            Box("West Wall", new Vector3(-10,2,0), new Vector3(.3f,4,20), stone);
            var spawns = new GameObject("Spawn Points").transform;
            for (var i=0; i<GameRules.MaxPlayers; i++)
            {
                var angle = i * Mathf.PI * 2 / GameRules.MaxPlayers;
                var point = new Vector3(Mathf.Sin(angle)*8,.1f,Mathf.Cos(angle)*8);
                var spawn = new GameObject("Spawn_" + (i+1)).transform;
                spawn.SetParent(spawns); spawn.position = point;
                spawn.rotation = Quaternion.LookRotation(-new Vector3(point.x,0,point.z));
                Box("Spawn Marker " + (i+1),point + Vector3.up*.01f,new Vector3(1,.02f,1),blue);
                var cover = new Vector3(Mathf.Sin(angle)*4, .85f, Mathf.Cos(angle)*4);
                Box("Cover " + (i+1),cover,new Vector3(2,1.5f,1),stone);
            }
            Box("Pedestal",new Vector3(0,.6f,0),new Vector3(1.2f,1,1.2f),stone);
            Box("Crown Placeholder",new Vector3(0,1.5f,0),new Vector3(.4f,.25f,.4f),gold);
            var rig = new GameObject("LOCAL PREVIEW ONLY - Replace with network player");
            rig.transform.position = new Vector3(0,.1f,-8);
            var tracked = rig.AddComponent<LocalTrackedRig>();
            var camera = new GameObject("Main Camera"); camera.tag="MainCamera";
            camera.transform.SetParent(rig.transform,false);camera.transform.localPosition=Vector3.up*1.6f;
            camera.AddComponent<Camera>().nearClipPlane=.05f; camera.AddComponent<AudioListener>();
            tracked.Head=camera.transform;
            tracked.LeftHand=Hand(rig.transform,"Left Hand",new Vector3(-.2f,1.2f,.3f),blue);
            tracked.RightHand=Hand(rig.transform,"Right Hand",new Vector3(.2f,1.2f,.3f),gold);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="The Cursed Crown";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.cursedcrown.vr");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            AssetDatabase.SaveAssets();
            Debug.Log("Cursed Crown starter generated. Gameplay/network/headset testing remains pending.");
        }

        private static Transform Hand(Transform parent,string name,Vector3 position,Material material)
        {
            var hand=Box(name,Vector3.zero,new Vector3(.08f,.08f,.16f),material);
            hand.transform.SetParent(parent,false);hand.transform.localPosition=position;
            Object.DestroyImmediate(hand.GetComponent<Collider>());
            return hand.transform;
        }
        private static GameObject Box(string name,Vector3 position,Vector3 scale,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;
            box.transform.position=position;box.transform.localScale=scale;
            box.GetComponent<Renderer>().sharedMaterial=material;return box;
        }
        private static Material Material(string name,Color color)
        {
            var path="Assets/CursedCrown/Materials/"+name.Replace(" ","")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.color=color;return material;
        }
    }
}
