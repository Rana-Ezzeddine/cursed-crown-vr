using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    // Sandbox-only controls for testing health before the gun system is ready.
    public sealed class HealthSandboxTester : MonoBehaviour, ISpawnService
    {
        public int TargetClientId;

        uint nextShotSequence = 1;
        ValidatedHit lastTestHit;
        bool hasLastTestHit;
        PlayerHealth subscribedTarget;
        int deathEvents;
        bool blockTestRespawns;

        void Awake()
        {
            PlayerHealth.SetSpawnService(this);
        }

        void OnDestroy()
        {
            if (subscribedTarget != null)
                subscribedTarget.PlayerDied -= OnPlayerDied;

            if (object.ReferenceEquals(PlayerHealth.SpawnService, this))
                PlayerHealth.SetSpawnService(null);
        }

        void OnGUI()
        {
            if (!Application.isPlaying || NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsServer)
                return;

            GUILayout.BeginArea(new Rect(20, 420, 340, 270), GUI.skin.box);
            GUILayout.Label("PERSON 4 HEALTH SANDBOX");
            GUILayout.Label("Target client ID (0-4)");
            TargetClientId = Mathf.RoundToInt(GUILayout.HorizontalSlider(TargetClientId, 0, 4));

            if (GUILayout.Button($"Apply one hit (-{GameRules.ShotDamage} HP)"))
                ApplyTestHit();

            GUI.enabled = hasLastTestHit;
            if (GUILayout.Button("Repeat last hit (should reject)"))
                RepeatLastHit();

            if (GUILayout.Button("Wrong-round hit (should reject)"))
                ApplyWrongRoundHit();

            if (GUILayout.Button(blockTestRespawns ?
                "Make respawn spawn available" : "Simulate no free respawn spawn"))
                blockTestRespawns = !blockTestRespawns;

            GUI.enabled = true;

            if (GUILayout.Button("Reset health / respawn"))
                ResetTarget();

            PlayerHealth target = FindTarget();
            if (target != subscribedTarget)
            {
                if (subscribedTarget != null)
                    subscribedTarget.PlayerDied -= OnPlayerDied;

                subscribedTarget = target;
                deathEvents = 0;
                if (subscribedTarget != null)
                    subscribedTarget.PlayerDied += OnPlayerDied;
            }

            if (target != null)
            {
                GUILayout.Label("Health: " + target.CurrentHealth + " / " + GameRules.MaxHealth +
                    " | Alive: " + target.IsAlive);
            }
            else
            {
                GUILayout.Label("Target player is not connected.");
            }

            GUILayout.Label("Death events this run: " + deathEvents);

            GUILayout.EndArea();
        }

        void OnPlayerDied(ulong playerId)
        {
            if (subscribedTarget != null && playerId == subscribedTarget.OwnerClientId)
                deathEvents++;
        }

        PlayerHealth FindTarget()
        {
            PlayerHealth[] players = FindObjectsByType<PlayerHealth>();
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
            ValidatedHit hit = new ValidatedHit(target.CurrentRound, shooterId, target.OwnerClientId,
                nextShotSequence++, target.transform.position);

            lastTestHit = hit;
            hasLastTestHit = true;

            bool applied = target.ApplyValidatedHit(lastTestHit);
            Debug.Log("Health sandbox hit. Accepted: " + applied);
        }

        void RepeatLastHit()
        {
            PlayerHealth target = FindTarget();
            if (target == null || !hasLastTestHit)
                return;

            bool applied = target.ApplyValidatedHit(lastTestHit);
            Debug.Log("Repeated sandbox hit. Accepted: " + applied);
        }

        void ApplyWrongRoundHit()
        {
            PlayerHealth target = FindTarget();
            if (target == null)
                return;

            ulong shooterId = target.OwnerClientId == 0 ? 1UL : 0UL;
            uint wrongRound = target.CurrentRound == uint.MaxValue ? 0u : target.CurrentRound + 1u;
            ValidatedHit hit = new ValidatedHit(wrongRound, shooterId, target.OwnerClientId,
                nextShotSequence++, target.transform.position);

            int healthBefore = target.CurrentHealth;
            bool applied = target.ApplyValidatedHit(hit);
            Debug.Log("Wrong-round sandbox hit. Accepted: " + applied +
                "; health unchanged: " + (target.CurrentHealth == healthBefore));
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
            if (blockTestRespawns)
                return false;

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
