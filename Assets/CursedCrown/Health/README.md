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

## Manual sandbox validation

Reported as passing in Unity on 29 October 2026:
- Health decreased in 25-point steps from 100 to 0.
- Repeating the exact same hit was rejected without additional damage.
- The player respawned after the configured five-second delay with full health.
- The player collider was disabled while dead and enabled again after respawn.
- The death-event counter increased to 1 when the player died and remained 1 while dead, as reported by the developer.
- The duplicate-hit rejection and collider tests passed in Play Mode, as reported by the developer.
- The Console had no red errors after the latest branch update.

These results validate the isolated sandbox flow. Real weapon raycasts, cover, the production spawn service, multi-client health replication, and headset behaviour have not yet been validated.

## Integration still needed

1. Add `PlayerHealth` to the networked `FoundationPlayer` prefab during the Person 4 integration step.
2. Person 2's spawn-service component must call `PlayerHealth.SetSpawnService(this)` when it is ready.
3. Person 3 must call `ApplyValidatedHit` on the target player's component after server-side shot validation.
4. Members 2, 3, and 5 should check `CanAct` before allowing teleport, firing, or crown pickup. `IsAlive` remains available when they only need the alive state.
5. `HealthHUD` is attached to the sandbox test player and creates a small World Space Canvas in front of the local player's tracked head.

## Sandbox test

The Person 4 sandbox has its own copy of the player prefab and a simple host-only test panel. It does not modify the shared FoundationPlayer prefab.

1. Open `Assets/CursedCrown/Tests/Person4/Person4_Sandbox.unity` in Unity.
2. Enter Play Mode and click **Host LAN**.
3. In the Game view, use the **PERSON 4 HEALTH SANDBOX** panel. Set target client ID to 0 for the host player.
4. Click **Wrong-round hit (should reject)** first. The Console should report `Accepted: False` and unchanged health. Then click **Apply one hit (-25 HP)** once, then click **Repeat last hit (should reject)**. Health should stay at 75 and the repeated hit should report `Accepted: False`.
5. Click **Apply one hit (-25 HP)** three more times. Health should reach 0; the player should be marked dead, its colliders should be disabled, and `Death events this run` should increase to 1.
6. While the player is dead, click **Apply one hit (-25 HP)** again. Health should remain at 0 and the death-event count should remain 1.
7. For the normal path, leave the spawn available, watch the HUD count down from five seconds, and confirm the player respawns at 100 HP.
8. To test the fallback, click **Simulate no free respawn spawn** before the player dies. After the countdown, the HUD should show **WAITING FOR SPAWN**. Click **Make respawn spawn available**; the player should then respawn at 100 HP without restarting the session.
9. Click **Reset health / respawn** to restore the target for another test. For a remote client, set the target ID to its client ID.

The panel simulates an already-validated hit; it does not test raycasts, cover, or Person 3's weapon code. Its spawn service reuses the starter's distinct initial spawn, so it does not test Person 2's future occupancy validation.

The sandbox scene and test prefab are branch-local copies. The shared player prefab, shared Arena scene, main branch, and teammates' files remain unchanged. The code must still be imported and played in Unity; I cannot run the Unity Editor or Quest hardware from GitHub, so runtime results must be confirmed by you.
