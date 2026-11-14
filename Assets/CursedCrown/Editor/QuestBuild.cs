using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class QuestBuild
    {
        [MenuItem("Cursed Crown/Build Quest APK")]
        public static void Build() => BuildInternal(false);

        [MenuItem("Cursed Crown/Build and Run Quest")]
        public static void BuildAndRun() => BuildInternal(true);

        static void BuildInternal(bool run)
        {
            // Build the saved final integration scene; do not regenerate teammates' work.
            var scene = EditorSceneManager.OpenScene(Person5ArenaSetup.Scene);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<MatchBootstrap>();
            if (bootstrap == null || bootstrap.MatchPrefab == null ||
                bootstrap.MatchPrefab.GetComponent<Person4MatchHealthIntegration>() == null)
                throw new InvalidOperationException("Final scene needs the weapon/health match bridge.");
            var match = bootstrap.MatchPrefab.GetComponent<MatchController>();
            if (match.AllowMockAlive || match.AllowDesktopPickup)
                throw new InvalidOperationException("Quest build must disable sandbox health and hand allowances.");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cursedcrown.vr");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Quest");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Person5ArenaSetup.Scene },
                locationPathName = "Builds/Quest/TheCursedCrown.apk",
                target = BuildTarget.Android, options = BuildOptions.Development | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None) });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Quest build failed: " + report.summary.result);
            Debug.Log("CC_QUEST_APK_BUILD_PASS " + report.summary.totalSize);
        }
    }
}
