# Member 1 implementation validation — updated 26 October 2026

Scope: session lifecycle, menus, late-join roster events, reconnect cleanup, avatar smoothing/tracking handling, cross-platform desktop test runner. This is an implementation awaiting Unity integration verification.

| Check | Result | Evidence / limit |
|---|---|---|
| Session logic executable tests | 26 passed | Actual FoundationSession/SessionInput/GameRules compiled with .NET 8 and explicit Unity/SDK doubles; no live network or Unity assemblies. |
| Test scheduling regression tests | 11 passed | The actual pure-C# SmokeTestPlan is tested with delayed starts, incomplete rosters/reports, sixth acceptance, rejection ordering, old builds, host loss and rejoin. This is not a live-network test. |
| C# syntax scan | See latest PR validation | Tree-sitter parses source files; this is not an API/type or Unity compile check. |
| Git whitespace / metadata / changed-file review | Passed | Package versions, saved scenes, prefab and other owners' gameplay systems stay outside this change. |
| Original Member 1 ZIP: Editor host and Windows build | User-observed success on 26 October | Screenshots show the hosting UI with 1/5 players and CC_DESKTOP_BUILD_PASS. The exact local commit was not independently recorded. This does not certify the subsequent runner repair. |
| Revised runner Unity import/compile | Pending | The new scheduling/report changes must be imported and rebuilt in the user's Unity Editor. |
| Editor and headset menu appearance | Pending | Requires real rendering and controller checks. |
| Original fixed-timer network suite on Windows | Failed on 26 October | Reports 0/2/3/4 were missing; client 1 reached five players with valid poses/motion and no errors; intended rejected client 5 connected. The run did not establish capacity rejection. |
| Revised readiness-driven network suite | Pending | Fixed independent process exit timers and the 50-second stage cutoff. New controller waits for all five to be live before testing rejection and collects progress/final/exit details. Requires rerun in Unity. |
| Relay host/join, cancel, reconnect and LAN fallback | Pending | Requires linked team Unity services project, service access and internet. |
| Android APK and Quest 2 + Quest 3 | Pending | Requires Android modules, approved installation setup and devices. |
| University Wi-Fi and five headsets | Pending | Requires intended network/devices. |

For each pending test record: date, tested commit, Editor version, device/OS, network, steps, expected result, actual result and tester. Do not paste raw authentication/service logs.

The fixed timing flaw can explain the reported failure: a short-lived client can leave before a slow sixth joins, and the runner can kill still-running players before they write reports. Without their logs this is not a confirmed sole cause. The repair preserves the five-player limit and all acceptance assertions; a sixth connecting while five are confirmed present still fails.

Acceptance checklist:

- Host plus four clients; sixth is rejected; a freed place accepts a new/rejoining player.
- Late joiner sees existing avatars at valid distinct roots and receives their movements.
- Disconnect removes the avatar once. Local leave restores the lobby. Host departure informs every remaining client.
- Leave/rejoin and host-again work without restarting the app; cancelled/timed-out Relay work never starts an obsolete session later.
- Invalid IPv4/code and service failures show actionable text without exception contents.
- Quest IP/code entry, trigger selection and B-toggle work; menu is hidden during ordinary connected play.
- Tracking loss does not synthesize desktop movement while the headset device remains present.
- Member 5 confirms the agreed match behaviour for late joins; readiness and match rules remain theirs.
