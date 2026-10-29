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
                if (root.GetComponent<GunEffects>() == null) root.AddComponent<GunEffects>();
                // A body collider makes shots hittable until Person 4 supplies its hitboxes.
                var body = root.transform.Find("Body");
                if (body != null && body.GetComponent<Collider>() == null)
                    body.gameObject.AddComponent<BoxCollider>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("CC_WEAPONS_INSTALLED");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}



