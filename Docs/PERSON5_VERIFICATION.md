# Person 5 verification — 29 October 2026

## Compilation milestone

- Existing repository and `feature/person5-crown-match`, starting at `feeb3cc`.
- Preserved all nine pre-existing untracked files in a local backup before editing.
- Unity 6000.6.2f1 successfully resolved the pinned packages and compiled runtime/editor scripts using this repository's project path.
- `Person5Setup.Prepare` generated only the Person 5 sandbox and its match prefab. `Person5Setup.Validate` passed scene wiring, one camera/listener/rig/session/presentation/bootstrap, and missing-script checks.
- Standalone actual-C# checks passed: 1,010 clock assertions and 536 crown/match assertions.
- Old `cursed-crown-foundation-20261022` Editor was inspected and left untouched; its unsaved scene was not discarded.

Compilation does not establish working gameplay, networking, UI or Quest behavior. Runtime results will be recorded below after execution. Raw Unity logs, cloud settings, caches and generated platform credentials are excluded from Git.

## Missing integrations and hardware

This checkout contains the foundation session/avatar implementation, but no real Person 2 placement or Person 4 health implementation. The sandbox explicitly enables simulated alive state and desktop tracking. Automated health/placement tests use opt-in mocks, not teammate acceptance evidence. Real implementations must be connected and retested with both sandbox flags disabled.

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
