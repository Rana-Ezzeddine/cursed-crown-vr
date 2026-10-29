using UnityEditor;
using UnityEditor.SceneManagement;
namespace CursedCrown.Editor
{
    [InitializeOnLoad]
    public static class Person5PlayMode
    {
        const string Request = "Person5.PlayModeRequested";
        static double readyAt;
        static Person5PlayMode()
        {
            readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update += Check;
        }
        // Run without -quit. SessionState survives startup compilation/domain reload.
        public static void Run()
        {
            Person5Setup.Validate();
            EditorSceneManager.OpenScene(Person5Setup.Sandbox);
            SessionState.SetBool(Request, true);
        }
        static void Check()
        {
            if (!SessionState.GetBool(Request, false) || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.timeSinceStartup < readyAt) return;
            SessionState.SetBool(Request, false);
            EditorApplication.EnterPlaymode();
        }
    }
}
