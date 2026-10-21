# Setup status - 21 October 2026

## Confirmed

- Inspected local Unity installation and VR Assignment 2 project.
- Editor: 6000.6.2f1, revision 770e33f6875c. Android Build Support is installed.
- Existing course manifest: XRI 3.6.1, OpenXR 1.18.0, XR Management 4.7.0, Input System 1.20.0, URP 17.6.0.
- Private GitHub repository created under Rana-Ezzeddine/cursed-crown-vr. No public license added.
- Reused lean XR and URP settings with their actual asset GUIDs. Removed old cloud project association, local tutorial state and Unity Connect preferences; cleared unused inherited credential fields. Enabled XR loader automatic initialization in the starter copy.
- Added rule constants, draft system contracts, a local tracked preview rig, an editor arena generator and team documents.
- Configured visible .meta files and force-text serialization. Kept original Assignment 2 unchanged.

## Validation attempted

A batch Unity import/compile was attempted. Unity exited before compilation because this execution environment denied the Package Manager's local IPC socket:

`listen EPERM: operation not permitted /tmp/Unity-Upm-73018.sock`

This does not establish whether the starter compiles. The scene generator has not executed successfully; Arena.unity and its materials must be generated in an unrestricted normal Unity Editor session. No APK or headset test was performed. The baseline lockfile is copied from the resolved course project, not newly resolved for this starter.

## Networking installation - pending

Unity's official documentation lists Netcode for GameObjects 2.7.0 and Multiplayer Services 1.2.0 for Unity 6. These are candidate exact versions to resolve and test, not a verified installed combination for this starter. The manifest deliberately retains the existing resolved baseline until a normal Package Manager session can install and resolve the new dependencies.

1. In a normal Editor session, install com.unity.netcode.gameobjects 2.7.0 and com.unity.services.multiplayer 1.2.0 from the Unity registry.
2. Let Unity resolve the compatible Transport and Authentication dependencies; inspect their exact versions.
3. Compile, review XR Project Validation, and commit BOTH manifest.json and the real updated packages-lock.json.
4. Use the unified Multiplayer Services SDK rather than the deprecated standalone Relay package for Unity 6.
5. Link a separate appropriate Unity services project before Relay testing. No paid services or billing changes have been authorized or enabled.
6. Person 1 implements five-player connection approval, host/join UI and networked player prefab.

References:
- https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo
- https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.netcode.gameobjects.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.services.multiplayer.html

## Inputs still needed

Verified teammate GitHub usernames, final role assignments, and the announced deadline (when available). Headsets must be physically available for runtime validation. These do not block reviewing the prepared source and dividing initial work.
