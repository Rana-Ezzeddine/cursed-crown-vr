using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class Person5Setup
    {
        public const string Sandbox = "Assets/CursedCrown/Tests/Person5/Person5_Sandbox.unity";
        const string PrefabPath = "Assets/CursedCrown/Tests/Person5/MatchTest.prefab";

        [MenuItem("Cursed Crown/Person 5/Validate Match Sandbox")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(Sandbox);
            var boots = Object.FindObjectsByType<MatchBootstrap>(FindObjectsSortMode.None);
            if (boots.Length != 1 || boots[0].MatchPrefab == null ||
                boots[0].MatchPrefab.GetComponent<MatchController>() == null ||
                boots[0].MatchPrefab.GetComponent<NetworkObject>() == null)
                throw new System.InvalidOperationException("Expected one wired match bootstrap.");
            if (Object.FindObjectsByType<MatchPresentation>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<FoundationSession>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<FoundationRig>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1 ||
                Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1)
                throw new System.InvalidOperationException("Sandbox must contain one presentation/session/rig/camera/listener.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new System.InvalidOperationException("Missing script: " + t.name);
            Debug.Log("CC_PERSON5_VALIDATION_PASS");
        }
        [MenuItem("Cursed Crown/Person 5/Build Desktop Sandbox")]
        public static void BuildDesktop()
        {
            Validate();
            Directory.CreateDirectory("Builds");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Sandbox }, locationPathName = "Builds/Person5Sandbox.app",
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Person 5 build failed: " + report.summary.result);
            Debug.Log("CC_PERSON5_BUILD_PASS");
        }

        [MenuItem("Cursed Crown/Person 5/Prepare Match Flow Sandbox")]
        public static void Prepare()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Sandbox);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var temporary = new GameObject("Person5Match");
                temporary.AddComponent<NetworkObject>();
                var controller = temporary.AddComponent<MatchController>();
                controller.RoundSeconds = GameRules.MatchSeconds;
                controller.AllowMockAlive = true;
                controller.AllowDesktopPickup = true;
                prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
                Object.DestroyImmediate(temporary);
            }
            var bootstraps = Object.FindObjectsByType<MatchBootstrap>(FindObjectsSortMode.None);
            if (bootstraps.Length > 1) throw new System.InvalidOperationException("More than one match bootstrap in Person 5 sandbox.");
            var bootstrap = bootstraps.Length == 1 ? bootstraps[0] : new GameObject("PERSON 5 MATCH").AddComponent<MatchBootstrap>();
            bootstrap.MatchPrefab = prefab;
            var presentations = Object.FindObjectsByType<MatchPresentation>(FindObjectsSortMode.None);
            if (presentations.Length > 1) throw new System.InvalidOperationException("More than one match presentation.");
            var presentation = presentations.Length == 1 ? presentations[0] : new GameObject("PERSON 5 CROWN AND HUD").AddComponent<MatchPresentation>();
            presentation.CrownMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CursedCrown/Materials/CrownGold.mat");
            presentation.PanelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CursedCrown/Materials/UIUnlit.mat");
            if (presentation.CrownMaterial == null || presentation.PanelMaterial == null)
                throw new System.InvalidOperationException("Foundation gold/UI materials are missing.");
            var placeholder = GameObject.Find("Crown Visual Placeholder - Person 5 replaces");
            if (placeholder != null) placeholder.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CC_PERSON5_SANDBOX_PREPARED — run host/client checks before committing.");
        }
    }
}
