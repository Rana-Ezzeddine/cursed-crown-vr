using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class Person5HealthSetup
    {
        public const string Scene = "Assets/CursedCrown/Tests/Person5/Person5_HealthIntegration.unity";
        const string Player = "Assets/CursedCrown/Tests/Person5/Person5HealthPlayer.prefab";
        const string Match = "Assets/CursedCrown/Tests/Person5/Person5HealthMatch.prefab";
        [MenuItem("Cursed Crown/Person 5/Prepare Real Health Integration")]
        public static void Prepare()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Person5Setup.Sandbox);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(StarterSetup.PlayerPath);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(Player);
            if (player == null)
            {
                var copy = Object.Instantiate(source);
                copy.name = "Person5HealthPlayer";
                copy.AddComponent<PlayerHealth>();
                player = PrefabUtility.SaveAsPrefabAsset(copy, Player);
                Object.DestroyImmediate(copy);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Match);
            if (prefab == null)
            {
                var copy = new GameObject("Person5HealthMatch");
                copy.AddComponent<NetworkObject>();
                var controller = copy.AddComponent<MatchController>();
                controller.AllowMockAlive = false;
                controller.AllowDesktopPickup = true; // Geometry/health test; no headset attached.
                copy.AddComponent<Person5HealthBridge>();
                prefab = PrefabUtility.SaveAsPrefabAsset(copy, Match);
                Object.DestroyImmediate(copy);
            }
            Object.FindFirstObjectByType<FoundationSession>().GetComponent<NetworkManager>().NetworkConfig.PlayerPrefab = player;
            Object.FindFirstObjectByType<MatchBootstrap>().MatchPrefab = prefab;
            EditorSceneManager.SaveScene(scene, Scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CC_PERSON5_HEALTH_PREPARED");
        }
        [MenuItem("Cursed Crown/Person 5/Build Real Health Integration")]
        public static void Build()
        {
            Prepare();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, locationPathName = "Builds/Person5HealthIntegration.app",
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Health integration build failed: " + report.summary.result);
            Debug.Log("CC_PERSON5_HEALTH_BUILD_PASS");
        }
    }
}
