# Person 3 weapons integration

Right trigger requests one shot per press; left secondary requests a two-second recharge. The host owns charge, recharge completion, rate limiting, and the raycast. The host uses its last validated `FoundationAvatar.TrackedPose`, skips all colliders on the shooter's avatar, and stops at the nearest other collider so cover blocks hits. Each accepted shot has a unique sequence number; `HitValidated` emits one event for a living target. `ShotFired` replicates the host-confirmed endpoints for a brief laser tracer.

`Cursed Crown > Install Person 3 Weapons` adds `NetworkGun`, `GunEffects`, and a provisional body collider to `FoundationPlayer`. Person 4 may replace the collider with its hitboxes. This setup is idempotent.

Integration contract: once the Person 4 and 5 branches are combined, wire `NetworkGun.HitValidated` on the host to `Person5HealthBridge.TryApplyValidatedHit`. Supply an `IPlayerState` from the health bridge and an `IMatchState` from `MatchController` to each gun. The current branch deliberately compiles against `main`, where those concrete types do not exist. The integration owner must verify the round ID used by both systems and call `ResetRound` on the host; Person 5's match restart already iterates `IRoundResettable`.

## Validation record

- Unity 6000.6.2f1 imported and compiled this branch with zero Console errors on 29 October 2026.
- Unity menu reported `CC_WEAPONS_INSTALLED`; serialized prefab contains gun, tracer, and body collider.
- Networked fire, recharge, cover, hit-to-health, death, round reset, disconnect, and physical controller tests are pending an integrated two-client build and Quest devices. No claim of gameplay acceptance is made here.

