using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    // Sandbox-only controls for testing health before the gun system is ready.
    public sealed class HealthSandboxTester : MonoBehaviour, ISpawnService
    {
        public int TargetClientId;

        uint nextShotSequence = 1;

        void Awake()
        {
            PlayerHealth.SetSpawnService(this);
        }

        void OnDestroy()
        {
            if (PlayerHealth.SpawnService == this)
                PlayerHealth.SetSpawnService(null);
        }

        void OnGUI()
        {
            if (!Application.isPlaying || NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsServer)
                return;

            GUILayout.BeginArea(new Rect(20, 460, 300, 190), GUI.skin.box);
            GUILayout.Label("PERSON 4 HEALTH SANDBOX");
            GUILayout.Label("Target client ID (0-4)");
            TargetClientId = Mathf.RoundToInt(GUILayout.HorizontalSlider(TargetClientId, 0, 4));

            if (GUILayout.Button("Apply one hit (-25 HP)"))
                ApplyTestHit();

            if (GUILayout.Button("Reset health / respawn"))
                ResetTarget();

            PlayerHealth target = FindTarget();
            if (target != null)
                GUILayout.Label("Health: " + target.CurrentHealth + " / " + GameRules.MaxHealth +
                    " | Alive: " + target.IsAlive);
            else
                GUILayout.Label("Target player is not connected.");

            GUILayout.EndArea();
        }

        PlayerHealth FindTarget()
        {
            PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (PlayerHealth player in players)
            {
                if (player.OwnerClientId == (ulong)TargetClientId)
                    return player;
            }

            return null;
        }

        void ApplyTestHit()
        {
            PlayerHealth target = FindTarget();
            if (target == null)
                return;

            // Use a different ID so the test shot is not treated as a self-hit.
            ulong shooterId = target.OwnerClientId == 0 ? 1UL : 0UL;
            ValidatedHit hit = new ValidatedHit(0, shooterId, target.OwnerClientId,
                nextShotSequence++, target.transform.position);

            bool applied = target.ApplyValidatedHit(hit);
            Debug.Log("Health sandbox hit. Accepted: " + applied);
        }

        void ResetTarget()
        {
            PlayerHealth target = FindTarget();
            if (target != null)
                target.ResetRound();
        }

        // Reuse the starter's distinct initial spawn for this isolated test.
        public bool TryReserveSpawn(ulong playerId, out Pose pose)
        {
            pose = default;
            if (FoundationSession.Instance == null || FoundationSession.Instance.Manager == null ||
                !FoundationSession.Instance.Manager.IsServer)
                return false;

            pose = FoundationSession.Instance.AssignSpawn(playerId);
            return true;
        }

        // The starter keeps each player's initial slot assigned during this test.
        public void ReleaseReservation(ulong playerId) { }

        public Vector3 GetValidCrownDrop(Vector3 requestedPosition)
        {
            return requestedPosition;
        }
    }
}
