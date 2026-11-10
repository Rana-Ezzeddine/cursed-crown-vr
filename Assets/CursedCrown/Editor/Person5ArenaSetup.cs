using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class Person5ArenaSetup
    {
        public const string Scene = "Assets/CursedCrown/Tests/Person5/Person5_ArenaIntegration.unity";
        [MenuItem("Cursed Crown/Person 5/Prepare Arena Integration")]
        public static void Prepare()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/CursedCrown/Tests/Person2/Person2_Sandbox.unity");
            var placeholder = GameObject.Find("Crown Visual Placeholder - Person 5 replaces");
            if (placeholder != null) placeholder.SetActive(false);
            var session = Object.FindFirstObjectByType<FoundationSession>();
            session.arenaSpawnManager = Object.FindFirstObjectByType<ArenaSpawnManager>();
            var services = new GameObject("Arena Match Services").AddComponent<ArenaMatchServices>();
            services.Spawns = session.arenaSpawnManager;
            services.Drops = Object.FindFirstObjectByType<CrownDropManager>();
            var root = new GameObject("Person5ArenaMatch");
            root.AddComponent<NetworkObject>();
            var match = root.AddComponent<MatchController>();
            match.AllowMockAlive = false;
            match.AllowDesktopPickup = false;
            root.AddComponent<Person5HealthBridge>();
            // Keep the production P3 hit -> P4 health connection when this scene
            // and match prefab are regenerated for acceptance testing.
            root.AddComponent<Person4MatchHealthIntegration>();
            var presentation = new GameObject("Crown and Match HUD").AddComponent<MatchPresentation>();
            presentation.CrownMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CursedCrown/Materials/CrownGold.mat");
            presentation.PanelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CursedCrown/Materials/UIUnlit.mat");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/CursedCrown/Tests/Person5/Person5ArenaMatch.prefab");
            Object.DestroyImmediate(root);
            new GameObject("Match Bootstrap").AddComponent<MatchBootstrap>().MatchPrefab = prefab;
            var config = session.GetComponent<NetworkManager>().NetworkConfig;
            config.PlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterSetup.PlayerPath);
            const string registryPath = "Assets/CursedCrown/Tests/Person5/Person5ArenaNetworkPrefabs.asset";
            var registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(registryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(registry, registryPath);
            }
            while (registry.PrefabList.Count > 0) registry.Remove(registry.PrefabList[0]);
            registry.Add(new NetworkPrefab { Prefab = config.PlayerPrefab });
            registry.Add(new NetworkPrefab { Prefab = prefab });
            EditorUtility.SetDirty(registry);
            config.Prefabs.NetworkPrefabsLists.Clear();
            config.Prefabs.NetworkPrefabsLists.Add(registry);
            EditorSceneManager.SaveScene(scene, Scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CC_PERSON5_ARENA_PREPARED");
        }
        public static void Build()
        {
            Prepare();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, locationPathName = "Builds/Person5ArenaIntegration.app",
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Arena build failed");
            Debug.Log("CC_PERSON5_ARENA_BUILD_PASS");
        }
    }
}
