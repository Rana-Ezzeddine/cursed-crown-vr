using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    public class ArenaSpawnManager : MonoBehaviour
    {
        [Header("Arena References")]
        public Transform[] spawnPoints;
        public Collider arenaFloor;

        [Header("Spawn Safety")]
        public float playerRadius = 0.35f;
        public float playerHeight = 1.8f;
        public LayerMask blockingLayers = ~0;

        // Tracks which player has reserved each spawn point.
        private readonly Dictionary<int, int> reservations =
            new Dictionary<int, int>();

        // Returns true when a spawn position has enough space.
        public bool IsSpawnSafe(Vector3 position)
        {
            if (arenaFloor == null)
                return false;

            // Check that the point is above the arena floor.
            Vector3 rayStart = position + Vector3.up * 1f;

            if (!Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                3f,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider != arenaFloor)
                return false;

            Vector3 floorPosition = hit.point;

            float radius = playerRadius * 0.95f;

            Vector3 bottom =
                floorPosition + Vector3.up * (radius + 0.05f);

            Vector3 top =
                floorPosition + Vector3.up * (playerHeight - radius);

            Collider[] overlaps = Physics.OverlapCapsule(
                bottom,
                top,
                radius,
                blockingLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider obstacle in overlaps)
            {
                if (obstacle == arenaFloor)
                    continue;

                return false;
            }

            return true;
        }

        // Attempts to reserve a safe spawn for a player.
        public bool TryReserveSpawn(
            int playerId,
            out Vector3 spawnPosition,
            out Quaternion spawnRotation)
        {
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;

            // Prevent a player from reserving multiple points.
            if (reservations.ContainsKey(playerId))
                return false;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform point = spawnPoints[i];

                if (point == null)
                    continue;

                if (reservations.ContainsValue(i))
                    continue;

                if (!IsSpawnSafe(point.position))
                    continue;

                reservations[playerId] = i;

                spawnPosition = point.position;
                spawnRotation = point.rotation;

                Debug.Log(
                    $"Reserved {point.name} for player {playerId}"
                );

                return true;
            }

            Debug.LogWarning(
                $"No safe spawn available for player {playerId}"
            );

            return false;
        }

        // Releases a player's reserved spawn.
        public void ReleaseSpawn(int playerId)
        {
            if (reservations.Remove(playerId))
            {
                Debug.Log(
                    $"Released spawn reservation for player {playerId}"
                );
            }
        }

        // Checks whether a player has a reservation.
        public bool HasReservation(int playerId)
        {
            return reservations.ContainsKey(playerId);
        }
    }
}