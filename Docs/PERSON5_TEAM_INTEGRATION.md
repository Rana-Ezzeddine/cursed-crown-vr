# Teammate integration snapshot — 29 October 2026

## Person 4: real health dependency

`Assets/CursedCrown/Health/PlayerHealth.cs` and its existing `.meta` are imported **unchanged** from Person 4's `feature/p4-health-respawn` commit `d00a040d531d3f2bcdacc368b75b638a760a5658`. This is Person 4's contribution, not Rana's newly authored health system. No HealthHUD, Person 4 scene, shared player prefab or shared Arena was replaced.

Rana's `Person5HealthBridge` exposes the real per-player `CanAct` state through `IPlayerState`, which the crown controller already consumes. Attach the bridge beside MatchController. The controller prefers that local health adapter over scene-wide sandbox mocks. The host's normal alive-state polling drops the crown after an actual PlayerHealth death.

The bridge also exposes `TryApplyValidatedHit`. It rejects non-server, non-Playing and wrong-round calls before forwarding to Person 4's existing duplicate-hit/damage logic. Person 3 must still validate aim, cover, charge and shot rate before calling it. Person 4's raw `ApplyValidatedHit` currently has no round/phase check: callers must use the guarded boundary or Person 4 must add that validation before final team integration. This adapter does not implement weapons.

Use **Cursed Crown → Person 5 → Build Real Health Integration**, then `python3 Tools/check_person5_network.py --real-health`. The dedicated `Person5_HealthIntegration` scene uses a copied network player with real PlayerHealth and a separate match prefab with AllowMockAlive disabled. It preserves Person5_Sandbox for the original isolated crown tests. Desktop hand simulation remains explicitly enabled, since no headset is attached.

The automated real-health run uses Person 4's actual health, alive, damage, death, respawn and reset code. Spawn reservations/drop placement remain test doubles because Person 2's production service is not yet implemented. The imported health source uses this branch's shared GameRules.RespawnSeconds (3 seconds), not the 5-second tuning on Person 4's branch; no shared tuning was changed.

## Person 2: placement service still missing

Inspected `feature/arena-teleportation` at `6583869b80f3a1fdd691fb829d9eacd9d454c97d`. It currently provides `ArenaTeleportation`, which moves the local rig and uses right A for aim/release. It does **not** implement `ISpawnService` or `GetValidCrownDrop`, so there is no real crown-placement service to test yet. Do not present the fixture's successful placement tests as Person 2 integration acceptance. The crown controller retains the pedestal fallback.

## Person 1: compatible events, merge considerations

Inspected `feature/p1-sessions` at `fd9b97937953ccede8cea953f50dea9aaceeda01`. Its PlayerJoined/PlayerLeft events, Manager and session start/leave surface remain compatible with the match adapter. This branch's executed tests use the shared foundation implementation, not a merged copy of Person 1's newer branch.

Person 1 uses right B to show its menu; Person 2 uses right A for teleport. Crown pickup therefore uses **right grip**; gun/menu pointing retains the trigger. Hardware verification remains required.

When integrating Person 1's FoundationRig/FoundationAvatar changes, retain PickupPressed, RightTracked and the AvatarPose.RightTracked serialization/equality field. Propagate tracking loss to the host even if visual poses are frozen, so the host cannot accept a stale tracked-hand flag. Rebuild every peer together because the pose payload changed. No teammate branches or main were merged or modified by this work.
