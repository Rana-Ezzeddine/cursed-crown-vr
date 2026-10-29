using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    // Register the prefab on every peer before joining. The host spawns one match
    // object per session, including when NGO scene management is disabled.
    [DefaultExecutionOrder(-100)]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        public GameObject MatchPrefab;
        NetworkManager manager;
        void Start()
        {
            manager = FindFirstObjectByType<NetworkManager>();
            if (manager == null || MatchPrefab == null)
            {
                Debug.LogError("Person 5 match setup is missing its NetworkManager or prefab.", this);
                enabled = false;
                return;
            }
            if (!manager.NetworkConfig.Prefabs.Contains(MatchPrefab)) manager.AddNetworkPrefab(MatchPrefab);
            manager.OnServerStarted += SpawnMatch;
        }
        void SpawnMatch()
        {
            if (!manager.IsServer) return;
            Instantiate(MatchPrefab).GetComponent<NetworkObject>().Spawn();
        }
        void OnDestroy()
        {
            if (manager != null) manager.OnServerStarted -= SpawnMatch;
        }
    }
}
