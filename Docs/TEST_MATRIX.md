# Test evidence — 22 October 2026

Record date, exact commit/APK, headset models, network, steps, expected result, actual result and tester. Do not mark a test passed based on reading code.

| Test | Required result | Status |
|---|---|---|
| Clean local copy + import | Pinned packages resolve; no compile errors | Passed in normal Unity Editor |
| Scene / prefab / pose guards | One local rig/camera/listener; correct network wiring; invalid poses rejected | Passed |
| macOS development build | Build succeeds and separate players launch | Passed |
| Local five-process session | Host + four clients; distinct spawns and remote pose motion | Passed |
| Local sixth participant | Admission rejected with five-player capacity reason | Passed |
| Local client / host departure | Host observes client leave; remaining client ends on host loss | Passed |
| Editor visual preview | Arena and desktop lobby render without overlapping VR UI | Passed |
| Two devices | Quest 2 + Quest 3 share arena and avatars | Pending |
| Five devices | Five simultaneous headsets; sixth rejected | Pending |
| Teleport validity | Reject wall/outside/occupied destinations; one winner on contention | Pending |
| Cover | First obstruction blocks raycast; no through-wall damage | Pending |
| Charge | 10 accepted shots; reject empty/too-fast fire; 2s recharge | Pending |
| Simultaneous fire | Consistent host ordering, damage and death; no double application | Pending |
| Death/respawn | Disabled actions; 3s delay; reserve free spawn; restore health/charge | Pending |
| Crown pickup | Range + line of sight; exactly one carrier for competing requests | Pending |
| Crown scoring | Completed continuous seconds only; fresh counter each possession | Pending |
| Carrier disconnect | Crown drops validly or returns to pedestal; no stale ownership | Pending |
| Headset host disconnect | Session ends; polish friendly error text and verify headset view | Pending |
| Restart | All owners reset; no old timer, stale request or reservation | Pending |
| Quest 2 performance | Measure frame timing during five-player action; record target/results | Pending |
| Individual demo | Each member explains architecture and modifies/debugs own contribution | Pending |

The passed networking rows use separate built macOS processes on loopback, not physical devices. See VALIDATION_RESULTS.json for sanitized evidence. Relay, Android builds, VR controller interaction and all gameplay rows remain unverified. A fresh teammate clone/import is still required on each teammate machine.
