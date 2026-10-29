using UnityEditor;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class WeaponSetup
    {
        [MenuItem("Cursed Crown/Install Person 3 Weapons")]
        public static void Install()
        {
            const string path = "Assets/CursedCrown/Prefabs/FoundationPlayer.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<NetworkGun>() == null) root.AddComponent<NetworkGun>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("CC_WEAPONS_INSTALLED");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

