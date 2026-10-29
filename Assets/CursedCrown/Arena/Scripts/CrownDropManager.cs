using UnityEngine;

namespace CursedCrown
{
    public class CrownDropManager : MonoBehaviour
    {
        [Header("Arena References")]
        public Transform[] crownDropPoints;
        public Collider arenaFloor;

        [Header("Drop Safety")]
        public float clearanceRadius = 0.35f;
        public float clearanceHeight = 0.5f;
        public LayerMask blockingLayers = ~0;

        // Checks whether a crown can safely be dropped here.
        public bool IsSafeDropPosition(Vector3 position)
        {
            if (arenaFloor == null)
                return false;

            // Confirm the point is above the arena floor.
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

            // Check whether the crown has space.
            Vector3 center =
                hit.point + Vector3.up *
                (clearanceHeight / 2f + 0.05f);

            Collider[] overlaps = Physics.OverlapSphere(
                center,
                clearanceRadius,
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

        // Returns the first safe crown-drop position.
        public bool TryGetSafeDropPosition(out Vector3 position)
        {
            position = Vector3.zero;

            if (crownDropPoints == null ||
                crownDropPoints.Length == 0)
            {
                return false;
            }

            foreach (Transform point in crownDropPoints)
            {
                if (point == null)
                    continue;

                if (IsSafeDropPosition(point.position))
                {
                    position = point.position;
                    return true;
                }
            }

            return false;
        }

        // Run this from the Unity Inspector to test all drop points.
        [ContextMenu("Test Crown Drop Positions")]
        void TestCrownDropPositions()
        {
            Debug.Log("===== CROWN DROP SAFETY TEST =====");

            if (crownDropPoints == null ||
                crownDropPoints.Length == 0)
            {
                Debug.LogWarning(
                    "No crown drop points assigned!"
                );
                return;
            }

            int safeCount = 0;
            int blockedCount = 0;

            for (int i = 0; i < crownDropPoints.Length; i++)
            {
                Transform point = crownDropPoints[i];

                if (point == null)
                {
                    Debug.LogWarning(
                        $"Crown drop point {i + 1} is missing!"
                    );
                    continue;
                }

                bool safe =
                    IsSafeDropPosition(point.position);

                if (safe)
                {
                    safeCount++;

                    Debug.Log(
                        $"{point.name}: SAFE"
                    );
                }
                else
                {
                    blockedCount++;

                    Debug.LogWarning(
                        $"{point.name}: BLOCKED"
                    );
                }
            }

            Debug.Log(
                $"Results: {safeCount} SAFE, " +
                $"{blockedCount} BLOCKED"
            );

            if (TryGetSafeDropPosition(out Vector3 position))
            {
                Debug.Log(
                    $"Selected safe crown drop: {position}"
                );
            }
            else
            {
                Debug.LogWarning(
                    "No safe crown drop position found!"
                );
            }

            Debug.Log("===== TEST COMPLETE =====");
        }
    }
}