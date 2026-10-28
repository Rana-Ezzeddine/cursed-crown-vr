using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    // The host owns health, death, and respawn decisions.
    public sealed class PlayerHealth : NetworkBehaviour, IRoundResettable
    {
        public static PlayerHealth LocalPlayer { get; private set; }
        public static ISpawnService SpawnService { get; private set; }

        public NetworkVariable<int> Health = new(GameRules.MaxHealth);
        public NetworkVariable<bool> Alive = new(true);

        public int CurrentHealth => Health.Value;
        public bool IsAlive => Alive.Value;
        public bool CanAct => IsSpawned && Alive.Value;

        public event Action<int, int> HealthChanged;
        public event Action<bool> AliveChanged;
        public event Action<ulong> PlayerDied;

        readonly HashSet<string> processedShots = new();
        Collider[] playerColliders;

        // Person 2's spawn system registers itself here.
        public static void SetSpawnService(ISpawnService service)
        {
            SpawnService = service;
        }

        public override void OnNetworkSpawn()
        {
            Health.OnValueChanged += OnHealthValueChanged;
            Alive.OnValueChanged += OnAliveValueChanged;
            playerColliders = GetComponentsInChildren<Collider>(true);

            if (IsOwner)
                LocalPlayer = this;

            if (IsServer)
            {
                Health.Value = GameRules.MaxHealth;
                Alive.Value = true;
            }

            UpdateHitboxes(Alive.Value);
            HealthChanged?.Invoke(Health.Value, GameRules.MaxHealth);
            AliveChanged?.Invoke(Alive.Value);
        }

        public override void OnNetworkDespawn()
        {
            Health.OnValueChanged -= OnHealthValueChanged;
            Alive.OnValueChanged -= OnAliveValueChanged;
            StopAllCoroutines();

            if (IsServer && SpawnService != null)
                SpawnService.ReleaseReservation(OwnerClientId);

            if (LocalPlayer == this)
                LocalPlayer = null;
        }

        // Call this on the host only after the weapon system validates a shot.
        public bool ApplyValidatedHit(ValidatedHit hit)
        {
            if (!IsServer || !IsSpawned)
                return false;

            if (!CanAct || hit.Target != OwnerClientId || hit.Shooter == hit.Target)
                return false;

            string shotId = hit.Round + ":" + hit.Shooter + ":" + hit.ShotSequence;
            if (processedShots.Contains(shotId))
                return false;

            processedShots.Add(shotId);
            Health.Value = Mathf.Max(0, Health.Value - GameRules.ShotDamage);

            if (Health.Value == 0)
                Die();

            return true;
        }

        void Die()
        {
            if (!Alive.Value)
                return;

            Alive.Value = false;

            if (SpawnService != null)
                SpawnService.ReleaseReservation(OwnerClientId);

            PlayerDied?.Invoke(OwnerClientId);
            StartCoroutine(RespawnAfterDelay(true));
        }

        IEnumerator RespawnAfterDelay(bool waitForRespawn)
        {
            if (waitForRespawn)
                yield return new WaitForSeconds(GameRules.RespawnSeconds);

            FoundationAvatar avatar = GetComponent<FoundationAvatar>();
            if (avatar == null)
            {
                Debug.LogError("PlayerHealth needs FoundationAvatar on the same player object.");
                yield break;
            }

            // Keep trying while no safe spawn is available.
            while (IsSpawned && IsServer && !Alive.Value)
            {
                if (SpawnService != null && SpawnService.TryReserveSpawn(OwnerClientId, out Pose spawn))
                {
                    avatar.RootPosition.Value = spawn.position;
                    avatar.RootRotation.Value = spawn.rotation;
                    transform.SetPositionAndRotation(spawn.position, spawn.rotation);

                    Health.Value = GameRules.MaxHealth;
                    Alive.Value = true;
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        void OnHealthValueChanged(int previous, int current)
        {
            HealthChanged?.Invoke(current, GameRules.MaxHealth);
        }

        void OnAliveValueChanged(bool previous, bool current)
        {
            UpdateHitboxes(current);
            AliveChanged?.Invoke(current);
        }

        void UpdateHitboxes(bool enabled)
        {
            if (playerColliders == null)
                return;

            foreach (Collider playerCollider in playerColliders)
                playerCollider.enabled = enabled;
        }

        public void ResetRound()
        {
            if (!IsServer)
                return;

            StopAllCoroutines();
            processedShots.Clear();

            if (!Alive.Value)
            {
                StartCoroutine(RespawnAfterDelay(false));
                return;
            }

            Health.Value = GameRules.MaxHealth;
            Alive.Value = true;
            UpdateHitboxes(true);
        }
    }
}
