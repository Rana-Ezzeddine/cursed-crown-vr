# Person 5 integration preparation — 7 November 2026

Branch: `codex/person5-integration`, based on `integration/final-game` at `b19f372`.
This preparation branch incorporates P4 at `8875993` and P5 at `a9c6f0f`; it does not merge into main or the shared integration branch.

## Ownership and connections

P1/P2 retain credit for sessions, avatars, arena, teleportation, spawn reservations and safe-drop selection. P4 retains credit for PlayerHealth, HealthHUD and their sandbox; the latest PlayerHealth source is retained unchanged. Rana's integration work, assisted by Codex, reconciles the rig/avatar changes, synchronizes health rounds, adds the arena-service adapter, prepares a combined scene and extends runtime verification. P3 weapons are not included.

`MatchController.RoundId` owns round identity. `Person5HealthBridge` synchronizes every spawned health component on the host, including late joins, before forwarding damage, and before restart reset hooks. Health round identity is server-local; clients receive the match round and replicated HP/alive state. This does not yet synchronize P3's weapon counter.

`ArenaMatchServices` implements ISpawnService using P2's actual managers. It translates client IDs through FoundationSession's existing slot map so initial spawn, death, respawn and disconnect share the same reservations. The adapter registers with PlayerHealth and is preferred by MatchController over optional test fixtures. If no drop point is safe, the match falls back to its pedestal. If no spawn is safe, P4's health coroutine waits and retries. The imported P4 five-second respawn setting is preserved; team tuning agreement remains pending.

Rig/avatar integration preserves B-menu and A-teleport behavior and adds grip pickup. Frozen poses continue reporting RightTracked=false during headset tracking loss. All peers must be rebuilt together because the pose serialization changed.

## Combined scene and reproduction

Open `Assets/CursedCrown/Tests/Person5/Person5_ArenaIntegration.unity` in Unity 6000.6.2f1. It copies P2's arena and uses the production FoundationPlayer with P4 health, an arena service adapter, match bootstrap and crown/HUD. Both AllowMockAlive and AllowDesktopPickup are false in the saved match prefab. The old visual crown placeholder is disabled.

Run `CursedCrown.Editor.Person5ArenaSetup.Build` with Unity's batch executeMethod option to regenerate the scene and build `Builds/Person5ArenaIntegration.app`. Then run `python3 Tools/check_person5_network.py --arena`.

The explicitly enabled development test fixture simulates validated damage and moves avatar roots into pickup range. It explicitly enables desktop pickup after verifying untracked pickup is rejected by default. Health, respawn, reservation and drop-placement logic are real production implementations. These tests do not certify weapon raycasts, teleport input, physical tracking or headset performance.

## Integration regression found and fixed

The first combined runtime run reproduced a duplicate client scoreboard row during joining. The host roster was correct but initial snapshot plus pending roster deltas duplicated the joining player on a client. MatchController now publishes changed rosters as a clear/add batch, making that snapshot/delta overlap converge correctly, and skips unchanged rosters. The final rerun passed 46 checks across three desktop peers with zero reported runtime errors; the final machine-readable results are in PERSON5_ARENA_RESULTS.json. Historical Person5 test reports elsewhere describe their original isolated builds, not this combined build.

## Remaining work before final gameplay acceptance

- Add P3 weapons and reconcile its shared player-prefab changes. Wire validated hits and alive/match dependencies; synchronize weapon rounds and reject old-round fire/reload requests.
- P2 teleportation still needs alive/phase/round validation and consistent headset-offset landing validation. Test simultaneous destination occupancy and no-free-spawn behavior.
- P1's standalone test project still needs its new arena dependencies represented, and the pre-existing integration session changes need review (full exception logging and removed optional Relay authentication profiles).
- Run the complete production shooting/gameplay loop and five-player acceptance on the combined version, then Quest 2/3, Android installation, tracking loss, intended network and performance checks.
- Agree respawn timing, add weapon charge/recharge feedback, and complete the final technical report/contribution record.

The shared Arena scene is not replaced. This scene remains an integration test scene until the remaining systems and acceptance checks are complete.
