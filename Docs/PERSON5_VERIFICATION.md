# Person 5 verification — 29 October 2026

Current outcome: crown/match implementation is built and desktop-verified, including real Person 4 health integration. Real Person 2 placement and physical Quest validation remain outstanding.

## Compilation milestone

- Existing repository and `feature/person5-crown-match`, starting at `feeb3cc`.
- Preserved all nine pre-existing untracked files in a local backup before editing.
- Unity 6000.6.2f1 successfully resolved the pinned packages and compiled runtime/editor scripts using this repository's project path.
- `Person5Setup.Prepare` generated only the Person 5 sandbox and its match prefab. `Person5Setup.Validate` passed scene wiring, one camera/listener/rig/session/presentation/bootstrap, and missing-script checks.
- Standalone actual-C# checks passed: 1,010 clock assertions and 536 crown/match assertions.
- Old `cursed-crown-foundation-20261022` Editor was inspected and left untouched; its unsaved scene was not discarded.

Compilation does not establish working gameplay, networking, UI or Quest behavior. Runtime results will be recorded below after execution. Raw Unity logs, cloud settings, caches and generated platform credentials are excluded from Git.

## Missing integrations and hardware

The original sandbox uses explicit mocks. The separate health-integration scene now includes Person 4’s unchanged PlayerHealth through a Person 5 adapter, with mock alive disabled. Person 2’s actual placement service is still unavailable. Desktop hand simulation remains enabled; physical integration must be retested with both sandbox flags disabled.

Quest/controller input, Android installation, mixed Quest 2/3 sessions, five physical headsets, Relay and university Wi-Fi remain unverified. Desktop results cannot certify these.

## Standalone networking milestone

`Person5Setup.BuildDesktop` succeeded with Person5_Sandbox explicitly selected. `Tools/check_person5_network.py` passed 64 checks across five independent desktop processes with zero reported runtime errors. Evidence and source/assembly hashes are in PERSON5_NETWORK_RESULTS.json.

The run verified all-ready/host-only start; replicated deadline/ownership/scores; near versus far, untracked, missing-health, dead, wall-blocked and hand-inside-wall pickup; stale rounds; competing requests; completed-second scoring and fractional discard; death callback and alive polling; invalid-placement fallback and valid mock placement; late joining; carrier disconnect and retained points; reconnect; finish and overtime rejection; host-only restart; reset hooks; all-zero shared ties; repeated rounds; host loss and a fresh host session with one match object per peer.

Two preliminary runs were not passes: the first exposed a missing JSON serialization attribute in the test report, and the second used a build predating a newly added fixture command. Both were corrected before the successful run. No failing run is counted as acceptance evidence.

This milestone uses shortened rounds to exercise transitions. Full 240-second and Editor Play Mode results are recorded separately below when completed.

## Full duration, Play Mode and desktop UI

- A standalone run passed 71 checks including a complete 240-second round, possession scoring through expiry, and restart/reconnect. Every final process report had zero errors (independently checked after completion).
- The corrected Editor Play Mode runner passed 64 checks with four standalone clients and zero errors. Concurrent host/client pickup requests were dispatched from two Python threads. Reports are written atomically; an unreadable report can no longer count as zero errors.
- The first headless Play Mode attempt logged an internal Unity Search indexing exception; its apparent aggregate pass was invalidated after auditing the report. A subsequent GUI startup attempt timed out. Waiting for editor initialization with a SessionState request fixed startup; the final headless Play Mode run was clean. These preliminary attempts are not acceptance evidence.
- Visually inspected the rendered current Mac sandbox, crown on pedestal, readable score/control panels, clicked Host LAN → Ready → Start, observed the 240-second countdown decrease, and confirmed a distant pickup stayed rejected. This is desktop UI evidence, not headset evidence.
- No Android devices were listed by the installed ADB tool.

Remote follow-up: Person 4 and Person 2 work exists on separate branches. Their public contracts are being inspected for real integration; the earlier statement about missing implementations refers to this feature checkout before dependency integration.

## Real Person 4 integration milestone

Imported Person 4's PlayerHealth source/metadata unchanged from `d00a040`; added Person5HealthBridge and a separate Person5_HealthIntegration scene, player prefab and match prefab. Its explicit build succeeded. Thirty standalone host/client checks passed with AllowMockAlive disabled and zero runtime errors: replicated damage, duplicate-hit rejection, real death/drop, dead pickup rejection, real respawn and retake, round-guarded hits, and reset cancellation of a pending real respawn. See PERSON5_REAL_HEALTH_RESULTS.json.

Health, death, respawn and reset were real Person 4 code; spawn placement/reservations and drop service were still explicitly mocked. Person 2's remote branch has no ISpawnService yet. Person 1's newer session branch was inspected for API/input compatibility but not merged. See PERSON5_TEAM_INTEGRATION.md for exact revisions and integration caveats.

Crown input now uses right grip, avoiding right A teleport and right B session-menu bindings found on teammate branches. This mapping is compiled but has not been pressed on hardware.

Final recheck: both sandbox builds succeeded with the original shared prefab registry restored. The real-health run passed all 30 checks again, and final Editor Play Mode plus standalone clients passed all 64 checks with zero errors. Shared Arena, shared FoundationPlayer, pinned package files and main are unchanged. Original draft .meta files match the handoff byte-for-byte.
