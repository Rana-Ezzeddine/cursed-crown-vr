# The Cursed Crown

Five-player full-VR laser tag for CMPS297AG, targeting Meta Quest 2 and Quest 3.

**Shared multiplayer foundation verified in Unity on macOS. Physical Quest, Android APK and Relay tests are still pending.** Gameplay systems remain team assignments.

## Start here

1. Clone this private repository into a local folder outside cloud sync.
2. Add the repository root in Unity Hub and use **6000.6.2f1 (770e33f6875c)**. Install Android Build Support, SDK/NDK and OpenJDK for Quest development.
3. Allow the committed manifest and lockfile to resolve. Do not upgrade packages independently.
4. Open `Assets/CursedCrown/Scenes/Arena.unity`, press Play, and choose **Host LAN**.
5. Work on your feature branch in `Assets/CursedCrown/Tests/PersonN/PersonN_Sandbox.unity`. Integrate through pull requests into the shared Arena scene.

Read [FOUNDATION_HANDOFF.md](Docs/FOUNDATION_HANDOFF.md) for controls, architecture, services setup, five-person integration points and validation commands. Read [FIRST_TASKS.md](Docs/FIRST_TASKS.md) for ownership and milestones.

## Included

- Saved arena, five covers and spawn markers, pedestal and crown visual placeholder.
- Local XR head/controller tracking with desktop pose simulation; network player prefab has no duplicate cameras/listeners.
- LAN host/join, five-player admission cap, distinct initial spawns, replicated avatar poses, disconnect cleanup and host-departure handling.
- Desktop lobby plus controller-operated VR lobby and Relay code keypad. Relay implementation requires linking a suitable Unity services project before testing.
- Shared rule constants/interfaces, five personal sandbox scenes and repeatable Editor validation/build/network checks.

Teleportation, firing/recharge, damage, health/respawn, crown possession/scoring, match flow and restart are assigned to the team. The starter does not implement a complete match.

## Pinned versions

| Component | Version |
|---|---|
| Unity | 6000.6.2f1 |
| Netcode for GameObjects | 2.13.2 |
| Multiplayer Services | 2.3.2 |
| Transport (resolved) | 6.6.0 |
| XRI / OpenXR | 3.6.1 / 1.18.0 |
| XR Management / Input System | 4.7.0 / 1.20.0 |
| URP | 17.6.0 |

The lockfile was resolved by this Editor. Android settings enable OpenXR for Quest 2/3, ARM64 + IL2CPP, minimum API 32 and target API 34. Mac desktop simulation disables automatic XR startup. Run XR Project Validation and test physical devices before distributing an APK.

## Verified before upload

Unity import/compilation, scene/prefab validation, desktop Play Mode rendering, macOS development build, five simultaneous local player processes, sixth-player rejection, distinct spawns, tracked-pose replication, client departure and host departure. See [SETUP_STATUS.md](Docs/SETUP_STATUS.md), [TEST_MATRIX.md](Docs/TEST_MATRIX.md) and [VALIDATION_RESULTS.json](Docs/VALIDATION_RESULTS.json).

Commit assets with their `.meta` files. Keep Library, Temp, Logs, builds, account state and credentials out of Git. Coordinate shared scene/prefab, contract and package edits. No public project license has been selected; record sources and AI assistance in [CONTRIBUTIONS.md](Docs/CONTRIBUTIONS.md).
