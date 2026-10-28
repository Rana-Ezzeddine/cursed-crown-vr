# Person 4 — Health and respawning

This folder contains the first isolated health-system implementation for the Person 4 branch.

## PlayerHealth.cs

- Health starts at 100 and the host owns its networked value.
- The host calls `ApplyValidatedHit` only after Person 3 validates a shot.
- A shot is applied only once, and a dead player cannot take more damage.
- At zero health, the player dies and waits three seconds before respawning.
- Respawning uses Person 2's `ISpawnService`; if no spawn is free, it waits and tries again.
- `HealthChanged` and `AliveChanged` events are available for the HUD and feedback.

## Integration still needed

1. Add `PlayerHealth` to the networked `FoundationPlayer` prefab during the Person 4 integration step.
2. Person 2's spawn-service component must call `PlayerHealth.SetSpawnService(this)` when it is ready.
3. Person 3 must call `ApplyValidatedHit` on the target player's component after server-side shot validation.
4. Members 2, 3, and 5 should check `IsAlive` before allowing teleport, firing, or crown pickup.
5. Add `HealthHUD` to a Canvas and assign its `HealthText` and optional `StatusText` fields. For VR, use a World Space Canvas and position it where the player can comfortably see it.

The shared player prefab and other teammates' files have intentionally not been changed in this branch yet. This code still needs to be imported and tested in Unity before it is considered verified.
