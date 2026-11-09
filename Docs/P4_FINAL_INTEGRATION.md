# P3 + P4 + P5 integration on the team final-game branch

Working branch: `feature/p4-final-integration`  
Target branch: `integration/final-game`

This branch is based on the current team integration branch. It does not merge or rewrite the shared target branch.

## What this branch adds

- P3's `NetworkGun.cs` and `GunEffects.cs` are added to the existing production `FoundationPlayer.prefab`.
- The existing P4 `PlayerHealth.cs`, `HealthHUD.cs`, root capsule hitbox and P1/P2 avatar changes are retained.
- P3's body collider is added to the existing Body object so the host raycast has a body hitbox.
- `Person4MatchHealthIntegration` is attached to `Person5ArenaMatch.prefab`. On the host it:
  - subscribes to `NetworkGun.HitValidated`;
  - assigns P5's `Person5HealthBridge` and `MatchController` references to each spawned gun;
  - synchronizes `PlayerHealth.CurrentRound` and `NetworkGun.Round` with `MatchController.RoundId`;
  - forwards accepted validated hits through `Person5HealthBridge.TryApplyValidatedHit`;
  - forwards deaths to `MatchController.NotifyPlayerDied`.
- `NetworkGun.ResetRound` uses the active `MatchController.RoundId` when the match controller is present, avoiding a separate weapon-owned round counter during match restart.

## P2 spawn/drop ownership

The P4 bridge on this branch **does not implement `ISpawnService`** and does not call `FoundationSession.AssignSpawn`.

The team arena scene already includes P2's `ArenaMatchServices`, which adapts P2's `ArenaSpawnManager` and `CrownDropManager` to the shared service contract. `MatchController.RefreshServices` explicitly prefers `ArenaMatchServices`. This preserves P2's safe spawn reservations and crown-drop selection for initial spawn/respawn and carrier death.

## Combined Unity test

Use the existing team scene:

```
Assets/CursedCrown/Tests/Person5/Person5_ArenaIntegration.unity
```

Use the scene's existing `Person5ArenaNetworkPrefabs` registry. It already registers the production player and match prefabs, so the branch does not create a parallel test registry or substitute player prefab.

Recommended two-instance acceptance sequence:

1. Open the scene in Unity 6000.6.2f1 and run the Editor as Host LAN.
2. Build a standalone Linux client with this same scene as the first and only enabled startup scene, then join the host at `127.0.0.1` on the same computer.
3. Verify both players connect, ready up and the host starts the match.
4. Fire at the other player's body. Each accepted hit should remove 25 HP; four hits should trigger death, disabled hitboxes, the five-second countdown, and respawn with 100 HP.
5. Finish/restart the match. Verify weapon charge and round identity reset, and shots in the new round reduce health again.
6. Verify P2's reserved respawn points are used and the match chooses a safe crown drop when a carrier dies.
7. Verify shots stop at the first collider, including arena cover, and repeat with VR controllers when available.

The earlier `feature/p4-team-integration` two-instance test was reported as passing for real P3 weapon damage, P4 death/respawn and match restart. **That run used P4's temporary `FoundationSession.AssignSpawn` adapter.** The new branch instead uses P2's production arena service, so the combined team scene must be rerun before merge. Do not represent the new combined service path or headset behavior as runtime-verified yet.

## Remaining team acceptance

- Run this exact branch in the combined scene with at least two peers.
- Verify P2's teleportation checks also reject movement for dead players and validate simultaneous/blocked destinations.
- Complete P1's session review and five-player acceptance on the combined build.
- Verify Quest 2/3, Android installation, tracking loss, intended network and performance.
- Agree team tuning/feedback for weapon charge and respawn before final acceptance.
