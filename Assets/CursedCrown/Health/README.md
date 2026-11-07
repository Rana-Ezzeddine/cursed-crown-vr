# Person 4 — Health and respawning

This folder contains the first isolated health-system implementation for the Person 4 branch.

## PlayerHealth.cs

- Health starts at 100 and the host owns its networked value.
- The host calls `ApplyValidatedHit` only after Person 3 validates a shot.
- Hits from a round other than the active round are rejected. Person 5 must call `SetCurrentRound(round)` on each spawned `PlayerHealth` on the host, both when a player joins mid-round and whenever the round changes.
- A shot is applied only once, and a dead player cannot take more damage.
- Player colliders are disabled while dead and re-enabled when alive.
- At zero health, the player dies and waits five seconds before respawning.
- Respawning uses Person 2's `ISpawnService`; if no spawn is free, it waits and tries again.
- `HealthChanged` and `AliveChanged` events are available for the HUD and feedback.
- The host replicates the respawn deadline; the local HUD counts down and shows when it is waiting for a free spawn.

## Validation in Unity

Reported as passing on 29 October 2026:
- **Sandbox:** Health decreased in 25-point steps from 100 to 0.
- Repeating the exact same hit was rejected without additional damage.
- The death-event counter increased to 1 on death and remained 1 while the player was dead.
- Player colliders were disabled on death and re-enabled after respawn.
- The player respawned after the configured five-second delay with full health.
- The local HUD countdown worked. When respawns were blocked, it changed to `WAITING FOR SPAWN`; when a spawn became available, the player respawned without restarting the session.
- The wrong-round test did not reduce health, as expected: hits whose round differs from `CurrentRound` are rejected.
- The Console had no red errors after the latest branch update.
- **Production prefab smoke test:** After adding the components to `FoundationPlayer.prefab`, the developer ran `Arena.unity` and confirmed that the HUD appeared on the normal spawned player and was working.

The production-prefab check confirms that the components load and the HUD is visible in the Arena. The isolated team-integration branch now includes P3's host-authoritative `NetworkGun`, its hit event connection to P4 health, and P5 match/round coordination. Those combined connections still need a two-client Unity run. P2's production safe-spawn/drop service, P1's separate networking review, and physical Quest behaviour are not validated by the source-level integration.

## Integration status on this branch

1. `PlayerHealth`, `HealthHUD`, and the root/body hitboxes are integrated into the production `FoundationPlayer.prefab` on this branch. P3's `NetworkGun` and `GunEffects` are also present in the combined production prefab. The original shared Arena scene remains untouched.
2. **P3 hit-to-health connection:** `Person4MatchHealthIntegration` subscribes to `NetworkGun.HitValidated` on the host and forwards the hit through `Person5HealthBridge.TryApplyValidatedHit` to `PlayerHealth.ApplyValidatedHit`.
3. **P5 round/death connection:** the bridge synchronizes the authoritative `MatchController.RoundId` to each spawned `PlayerHealth` and `NetworkGun`, including late-spawned players. It forwards `PlayerDied` to `MatchController.NotifyPlayerDied` so a crown carrier can drop the crown.
4. **Respawn test service:** the integration scene registers a temporary `ISpawnService` adapter that reuses `FoundationSession.AssignSpawn`. This exercises the P4 five-second respawn loop and stable per-player spawn assignment, but it does not perform P2's safe occupancy validation or safe crown-drop selection.
5. **P2 production spawn/drop connection remains pending:** replace the temporary adapter with P2's production safe `ISpawnService` when that service is ready. Teleport/dead-player action gating also needs to be verified against P2's final movement implementation.
6. `HealthHUD` creates a small World Space Canvas in front of the local player's tracked head. It is attached to both the combined integration player prefab and the production player prefab on this branch.

## P4 + P3 + P5 team integration test

A separate integration scene is available at `Assets/CursedCrown/Tests/Person4/Person4_TeamIntegration.unity`. It is based on the isolated P5 health/match scene and uses a dedicated combined player prefab, so it does not replace the team's shared Arena scene.

The combined `Person4TeamPlayer.prefab` contains P3's `NetworkGun` and `GunEffects`, plus P4's `PlayerHealth`, `HealthHUD`, root capsule hitbox, and body collider. The temporary `HealthTestShooter` is disabled on this prefab so it cannot double-apply damage. The ordinary P4 sandbox retains that separate test shooter.

The `Person4MatchHealthIntegration` component on the match prefab performs the P4-side connections:
- Subscribes to P3's host-side `NetworkGun.HitValidated` event and forwards the validated hit through P5's `Person5HealthBridge` to `PlayerHealth.ApplyValidatedHit`.
- Supplies P3's `IPlayerState` and `IMatchState` references from the P5 bridge and match controller.
- Synchronizes P3 gun round IDs and P4 player-health round IDs to P5's authoritative match round, including late-spawned players.
- Forwards P4 death events to P5's match controller so a carrier can drop the crown.
- Provides a temporary spawn adapter using the starter's existing `FoundationSession.AssignSpawn`. This is only to test P4's respawn loop in this scene; it does **not** replace P2's safe arena spawn/drop service.

For desktop Play Mode in this integration branch only, `NetworkGun` accepts **Space** to fire and **R** to recharge; Quest continues to use the right trigger and left secondary button. Aim by holding the right mouse button and using **Q/E** to rotate the desktop rig. The network gun still performs its raycast and emits the same validated-hit event on the host.

### Run the team integration test

1. Pull `feature/p4-team-integration` and open `Assets/CursedCrown/Tests/Person4/Person4_TeamIntegration.unity`.
2. Run two game instances. Start **Host LAN** in one and **Join LAN** from the other. Use `127.0.0.1` for a second instance on the same computer, or the host's LAN IP on another computer.
3. Wait for both players to spawn. Each instance should show its local health HUD.
4. In the match panel, each player selects **Ready**. The host then selects **Start when everyone is ready**.
5. Aim at the other player's body. Press **Space** to fire on desktop, or pull the right-controller trigger on Quest. On desktop, hold the right mouse button and press **Q/E** to turn the rig toward the other player.
6. Each accepted shot should subtract 25 HP from the target. Four hits should trigger death, collider disabling, the five-second HUD countdown, and respawn with 100 HP.
7. Restart the match from the host panel after the round finishes. Verify that the new round accepts shots again and that health/charge reset.
8. Confirm that cover blocks the shot and that a dead player cannot fire or carry the crown.

The test scene intentionally uses the P5 match prefab and P4 integration bridge but a dedicated player prefab. It must be imported and run in Unity to validate compile-time and runtime behavior; source-level checks are not a substitute for that run.

### Integration boundary / still pending

- **P2 spawn/drop integration remains pending.** The current P4 adapter uses the foundation's stable spawn assignment to exercise respawn. Replace it with P2's registered safe `ISpawnService` when the P2 branch is integrated; also test crown-drop safety through P2's service.
- **P1 session branch is not merged into this scene.** This integration scene uses the session implementation already present in the P5 test scene. It is not intended to review or replace P1's networking work.
- This branch is a review/test combination only. Do not merge it automatically into any role branch. The role owners should review their source contributions before the team chooses an integration target.

## Sandbox test

The Person 4 sandbox has its own copy of the player prefab and a simple host-only test panel. The production `FoundationPlayer` prefab also has the Player 4 components on this feature branch; the shared Arena scene and `main` branch are unchanged.

1. Open `Assets/CursedCrown/Tests/Person4/Person4_Sandbox.unity` in Unity.
2. Enter Play Mode and click **Host LAN**.
3. In the Game view, use the **PERSON 4 HEALTH SANDBOX** panel. Set target client ID to 0 for the host player.
4. Click **Wrong-round hit (should reject)** first. This deliberately submits a hit for a different round, so health must not change and the Console should report `Accepted: False`. Then click **Apply one hit (-25 HP)** once, then click **Repeat last hit (should reject)**. Health should stay at 75 and the repeated hit should report `Accepted: False`.
5. Click **Apply one hit (-25 HP)** three more times. Health should reach 0; the player should be marked dead, its colliders should be disabled, and `Death events this run` should increase to 1.
6. While the player is dead, click **Apply one hit (-25 HP)** again. Health should remain at 0 and the death-event count should remain 1.
7. For the normal path, leave the spawn available, watch the HUD count down from five seconds, and confirm the player respawns at 100 HP.
8. To test the fallback, click **Simulate no free respawn spawn** before the player dies. After the countdown, the HUD should show **WAITING FOR SPAWN**. Click **Make respawn spawn available**; the player should then respawn at 100 HP without restarting the session.
9. Click **Reset health / respawn** to restore the target for another test. For a remote client, set the target ID to its client ID.

The panel simulates an already-validated hit; it does not test raycasts, cover, or Person 3's weapon code. Its spawn service reuses the starter's distinct initial spawn, so it does not test Person 2's future occupancy validation.

## Two-player shooting test (temporary test shooter)

The Person 4 test player prefab also has `HealthTestShooter`. It is only included in the Person 4 sandbox prefab, not the production `FoundationPlayer` prefab.

1. Open `Assets/CursedCrown/Tests/Person4/Person4_Sandbox.unity` on two running game instances. Use two computers on the same LAN, or run the Editor as host and a standalone build as client. For a same-computer client, use `127.0.0.1`; for another computer, enter the host's LAN IP address.
2. Start **Host LAN** on the first instance. Join the session from the second instance using **Join LAN**. Wait until both players have spawned.
3. In the host's **PERSON 4 HEALTH SANDBOX** panel, set the target client ID to the other player (usually client ID 1).
4. Aim the right hand at the other player's body. In desktop simulation, hold the right mouse button and use **Q/E** to rotate; press **Space** to shoot. On Quest, aim with the right controller and pull its trigger.
5. Each successful hit should reduce the target's health by 25. Four hits should cause death, disable the target collider, show the five-second respawn countdown, and respawn the target with full health.
6. Check the host Console for `Health test shot hit...`. A miss or a wall/cover hit is logged separately.

This test shooter sends only a fire request to the host. The host raycasts from the shooter's replicated right-hand transform and creates the `ValidatedHit`; the existing `PlayerHealth` applies the damage. This tests the player hitbox and health path with two networked players, but not real weapon models, ammunition/recharge, or Person 3's final firing implementation. Remove or disable the test shooter after Person 3's weapon system is integrated.

The sandbox scene and test prefab are branch-local copies. This feature branch also integrates Player 4's components into `FoundationPlayer.prefab`; `main`, the shared Arena scene, and teammates' files remain unchanged. The code must still be imported and played in Unity; I cannot run the Unity Editor or Quest hardware from GitHub, so runtime results must be confirmed by you.
