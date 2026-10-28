# Person 4 — Health and respawning

This folder contains the first isolated health-system implementation for the Person 4 branch.

## PlayerHealth.cs

- Health starts at 100 and the host owns its networked value.
- The host calls `ApplyValidatedHit` only after Person 3 validates a shot.
- A shot is applied only once, and a dead player cannot take more damage.
- Player colliders are disabled while dead and re-enabled when alive.
- At zero health, the player dies and waits five seconds before respawning.
- Respawning uses Person 2's `ISpawnService`; if no spawn is free, it waits and tries again.
- `HealthChanged` and `AliveChanged` events are available for the HUD and feedback.

## Manual sandbox validation

Reported as passing in Unity on 28 October 2026:
- Health decreased in 25-point steps from 100 to 0.
- Repeating the exact same hit was rejected without additional damage.
- The player respawned after the configured five-second delay with full health.
- The player collider was disabled while dead and enabled again after respawn.
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
4. Click **Apply one hit (-25 HP)** once, then click **Repeat last hit (should reject)**. Health should stay at 75 and the repeated hit should report `Accepted: False`.
5. Click **Apply one hit (-25 HP)** three more times. Health should reach 0; the player should be marked dead and its colliders should be disabled.
6. Wait five seconds. Because the sandbox registers a simple test spawn service, the player should respawn at its starter spawn and return to 100 HP.
7. Click **Reset health / respawn** to restore the target for another test. For a remote client, set the target ID to its client ID.

The panel simulates an already-validated hit; it does not test raycasts, cover, or Person 3's weapon code. Its spawn service reuses the starter's distinct initial spawn, so it does not test Person 2's future occupancy validation.

The sandbox scene and test prefab are branch-local copies. The shared player prefab, shared Arena scene, main branch, and teammates' files remain unchanged. The code must still be imported and played in Unity; I cannot run the Unity Editor or Quest hardware from GitHub, so runtime results must be confirmed by you.
