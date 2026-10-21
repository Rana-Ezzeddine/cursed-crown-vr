# The Cursed Crown

Five-player full-VR laser tag for CMPS297AG. Target: Meta Quest 2 and Quest 3.

**Status: starter scaffolding. No playable multiplayer build or headset test has been completed.**

## Open the project

1. Clone this private repository after Rana adds your verified GitHub account.
2. In Unity Hub, add the repository root (the folder containing Assets, Packages and ProjectSettings).
3. Use **Unity 6000.6.2f1** exactly. Install Android Build Support, SDK/NDK and OpenJDK through Hub.
4. Allow Package Manager to resolve the pinned packages. Do not upgrade packages individually.
5. If Arena.unity is not present, run **Cursed Crown > Create Starter Scene** once. This replaces the open scene after prompting to save it.
6. Open Assets/CursedCrown/Scenes/Arena.unity and press Play for a local arena preview.

This starter retains XR/OpenXR and URP configuration from Rana's Assignment 2. It creates a primitive arena, five spawn markers, cover, a central pedestal, a crown placeholder and a local tracked head/hand preview. No gameplay networking, teleport validation, weapons, health or scoring is implemented yet.

## Versions

- Editor: 6000.6.2f1 (770e33f6875c)
- XRI: 3.6.1
- OpenXR: 1.18.0
- XR Management: 4.7.0
- Input System: 1.20.0
- URP: 17.6.0

Packages/manifest.json and packages-lock.json are copied from the inspected course project, not fabricated. The initial lockfile covers the baseline only. Multiplayer dependencies are pending installation and resolution; see Docs/SETUP_STATUS.md.

## Team workflow

Read Docs/FIRST_TASKS.md and Docs/ARCHITECTURE.md first. Each person has their own local clone and feature branch. Use pull requests into main; request one teammate review. Make small merges. Coordinate changes to the arena scene, shared contracts, package files and eventual player prefab. Keep personal experiments under Assets/CursedCrown/Tests/PersonN. Commit .meta files with assets. Do not commit Library, generated builds, secrets or local Unity account state.

## Android build and join

After successful package resolution and compilation: choose Android in Build Profiles; confirm OpenXR and Meta Quest support, ARM64 + IL2CPP, and run XR Project Validation. Build an APK with the arena scene included. A headset must keep its Quest boundary enabled and use a cleared physical area. **Joining is not implemented**: Person 1 must implement and document the host/join flow before the first two-headset test.

## Completion gates

1. Resolve/import/compile baseline and generate arena.
2. Install and pin compatible Netcode + Multiplayer Services dependencies; commit the resulting real lockfile.
3. Connect Quest 2 and Quest 3 on the intended university network.
4. Complete all five role systems, then a full five-player mixed-device match.
5. Verify simultaneous fire, competing pickups/teleports, disconnects, host loss and restart.

No public project license has been selected. Preserve third-party notices and record sources and adaptations in Docs/CONTRIBUTIONS.md.
