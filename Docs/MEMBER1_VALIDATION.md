# Member 1 implementation validation — 25 October 2026

Scope: session lifecycle, menus, late-join roster events, reconnect cleanup, avatar smoothing/tracking handling, cross-platform desktop test runner. This is an implementation awaiting Unity integration verification.

| Check | Result | Evidence / limit |
|---|---|---|
| Session logic executable tests | 26 passed | Actual FoundationSession/SessionInput/GameRules compiled with .NET 8 and explicit Unity/SDK doubles; no live network or Unity assemblies. |
| C# syntax scan | Passed, 16 source files | Tree-sitter parses project C# sources and standalone tests; this is not an API/type or Unity compile check. |
| Git whitespace / metadata / changed-file review | Passed | Package versions, saved scenes, prefab and other owners' gameplay systems stay outside this change. |
| Unity 6000.6.2f1 clean import/compile | Pending | Unity Editor is unavailable in the implementation workspace. |
| Editor and headset menu appearance | Pending | Requires real rendering and controller checks. |
| New three-stage desktop network suite | Pending | Must run on the Member 1 branch; old main-branch results do not cover it. |
| Windows build and network suite | Pending | Platform-aware code is provided; no Windows Editor is available here. |
| Relay host/join, cancel, reconnect and LAN fallback | Pending | Requires linked team Unity services project, service access and internet. |
| Android APK and Quest 2 + Quest 3 | Pending | Requires Android modules, approved installation setup and devices. |
| University Wi-Fi and five headsets | Pending | Requires intended network/devices. |

For each pending test record: date, tested commit, Editor version, device/OS, network, steps, expected result, actual result and tester. Do not paste raw authentication/service logs.

Acceptance checklist:

- Host plus four clients; sixth is rejected; a freed place accepts a new/rejoining player.
- Late joiner sees existing avatars at valid distinct roots and receives their movements.
- Disconnect removes the avatar once. Local leave restores the lobby. Host departure informs every remaining client.
- Leave/rejoin and host-again work without restarting the app; cancelled/timed-out Relay work never starts an obsolete session later.
- Invalid IPv4/code and service failures show actionable text without exception contents.
- Quest IP/code entry, trigger selection and B-toggle work; menu is hidden during ordinary connected play.
- Tracking loss does not synthesize desktop movement while the headset device remains present.
- Member 5 confirms the agreed match behaviour for late joins; readiness and match rules remain theirs.
