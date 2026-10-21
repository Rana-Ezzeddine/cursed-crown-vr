# Test evidence (all runtime rows currently pending)

Record date, exact commit/APK, headset models, network, steps, expected result, actual result and tester. Do not mark a test passed based on reading code.

| Test | Required result | Status |
|---|---|---|
| Fresh clone + import | Pinned packages resolve; no compile errors | Pending |
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
| Host disconnect | Clear end-session message on remaining clients | Pending |
| Restart | All owners reset; no old timer, stale request or reservation | Pending |
| Quest 2 performance | Measure frame timing during five-player action; record target/results | Pending |
| Individual demo | Each member explains architecture and modifies/debugs own contribution | Pending |
