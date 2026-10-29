using UnityEditor;
using UnityEditor.SceneManagement;
namespace CursedCrown.Editor
{
    public static class Person5PlayMode
    {
        // Run without -quit: the opt-in probe drives Play Mode and exits after checks.
        public static void Run()
        {
            Person5Setup.Validate();
            EditorSceneManager.OpenScene(Person5Setup.Sandbox);
            EditorApplication.EnterPlaymode();
        }
    }
}
