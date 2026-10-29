using UnityEngine;

namespace CursedCrown
{
    public class ArenaSpawnTester : MonoBehaviour
    {
        public ArenaSpawnManager spawnManager;

        void Start()
        {
            if (spawnManager == null)
            {
                Debug.LogError("Spawn Manager not assigned!");
                return;
            }

            // Player 1 requests a spawn.
            bool first = spawnManager.TryReserveSpawn(
                1,
                out Vector3 position1,
                out Quaternion rotation1
            );

            Debug.Log(
                $"Player 1 reservation: {first}, Position: {position1}"
            );

            // Player 2 requests a different spawn.
            bool second = spawnManager.TryReserveSpawn(
                2,
                out Vector3 position2,
                out Quaternion rotation2
            );

            Debug.Log(
                $"Player 2 reservation: {second}, Position: {position2}"
            );

            // Player 1 tries to reserve again.
            bool duplicate = spawnManager.TryReserveSpawn(
                1,
                out _,
                out _
            );

            Debug.Log(
                $"Player 1 duplicate reservation: {duplicate}"
            );

            // Release Player 1's reservation.
            spawnManager.ReleaseSpawn(1);

            Debug.Log(
                $"Player 1 still reserved: {spawnManager.HasReservation(1)}"
            );
        }
    }
}