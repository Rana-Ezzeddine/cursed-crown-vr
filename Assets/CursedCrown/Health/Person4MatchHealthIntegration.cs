using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    // Connects P3 weapon events and P5 match state to P4 player health.
    // The spawn methods are an integration-test adapter; P2's production
    // occupancy-aware ISpawnService should replace this when integrated.
    [RequireComponent(typeof(MatchController), typeof(Person5HealthBridge))]
    public sealed class Person4MatchHealthIntegration : NetworkBehaviour, ISpawnService
    {
        MatchController match;
        Person5HealthBridge healthBridge;
        readonly HashSet<PlayerHealth> subscribedPlayers = new();

        public override void OnNetworkSpawn()
        {
            match = GetComponent<MatchController>();
            healthBridge = GetComponent<Person5HealthBridge>();

            if (!IsServer)
                return;

            PlayerHealth.SetSpawnService(this);
            match.RefreshServices();
            NetworkGun.HitValidated += OnHitValidated;

            SyncPlayers();
            SubscribeToDeaths();
        }

        void Update()
        {
            if (!IsSpawned || !IsServer || match == null)
                return;

            SyncPlayers();
            SubscribeToDeaths();
        }

        void SyncPlayers()
        {
            uint round = match.RoundId;

            PlayerHealth[] players = FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None);

            foreach (PlayerHealth player in players)
            {
                if (player.IsSpawned)
                    player.SetCurrentRound(round);
            }

            NetworkGun[] guns = FindObjectsByType<NetworkGun>(
                FindObjectsSortMode.None);

            foreach (NetworkGun gun in guns)
            {
                if (!gun.IsSpawned)
                    continue;

                gun.PlayerState = healthBridge;
                gun.MatchState = match;
                gun.SetRound(round);
            }
        }

        void SubscribeToDeaths()
        {
            PlayerHealth[] activePlayers = FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None);

            foreach (PlayerHealth oldPlayer in new List<PlayerHealth>(subscribedPlayers))
            {
                if (oldPlayer == null || !ContainsPlayer(activePlayers, oldPlayer))
                {
                    if (oldPlayer != null)
                        oldPlayer.PlayerDied -= OnPlayerDied;

                    subscribedPlayers.Remove(oldPlayer);
                }
            }

            foreach (PlayerHealth player in activePlayers)
            {
                if (player.IsSpawned && subscribedPlayers.Add(player))
                    player.PlayerDied += OnPlayerDied;
            }
        }

        static bool ContainsPlayer(PlayerHealth[] players, PlayerHealth target)
        {
            foreach (PlayerHealth player in players)
            {
                if (player == target)
                    return true;
            }

            return false;
        }

        void OnHitValidated(ValidatedHit hit)
        {
            if (!IsServer || match.Phase != MatchPhase.Playing ||
                hit.Round != match.RoundId)
                return;

            bool accepted = healthBridge.TryApplyValidatedHit(hit);
            if (!accepted)
                Debug.Log("Validated weapon hit was rejected by player health.");
        }

        void OnPlayerDied(ulong playerId)
        {
            if (IsServer && match != null)
                match.NotifyPlayerDied(playerId, match.RoundId);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                NetworkGun.HitValidated -= OnHitValidated;

            foreach (PlayerHealth player in subscribedPlayers)
            {
                if (player != null)
                    player.PlayerDied -= OnPlayerDied;
            }

            subscribedPlayers.Clear();

            if (object.ReferenceEquals(PlayerHealth.SpawnService, this))
                PlayerHealth.SetSpawnService(null);
        }

        // Test adapter: reuse the starter's stable per-player spawn assignment.
        // It is not a replacement for P2's safe arena reservation service.
        public bool TryReserveSpawn(ulong playerId, out Pose pose)
        {
            pose = default;

            if (!IsServer || FoundationSession.Instance == null ||
                FoundationSession.Instance.Manager == null ||
                !FoundationSession.Instance.Manager.IsServer)
                return false;

            pose = FoundationSession.Instance.AssignSpawn(playerId);
            return true;
        }

        // The foundation test adapter keeps its initial slot assigned.
        public void ReleaseReservation(ulong playerId) { }

        // The match test scene does not replace P2's safe crown-drop manager.
        public Vector3 GetValidCrownDrop(Vector3 requestedPosition)
        {
            return requestedPosition;
        }
    }
}
