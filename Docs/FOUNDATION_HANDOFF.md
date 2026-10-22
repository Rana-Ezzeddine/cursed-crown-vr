# Shared foundation: how the five people start

The foundation contains one scene-local camera/tracking rig and a host-authoritative network session. Network player prefabs contain head, hand and body visuals, not cameras or audio listeners. All gameplay features build on this common base.

## Open and run

1. Clone the repository into a normal local folder, preferably outside cloud-sync folders.
2. Open the root in Unity Hub using Editor 6000.6.2f1 (770e33f6875c).
3. Let Package Manager resolve the committed manifest and lockfile. Do not upgrade packages independently.
4. Open Assets/CursedCrown/Scenes/Arena.unity. If it has not been generated, use Cursed Crown > 1 - Generate Foundation.
5. Press Play. Host LAN starts a session on UDP 7777. A second desktop player joins using 127.0.0.1 on the same computer, or the host's LAN IPv4 address on another computer.
6. Host Relay / Join code use the unified Multiplayer Services SDK. They require a suitable linked Unity services project and service access. The foundation does not enable a paid plan. Relay DTLS forwards traffic; the host still owns gameplay decisions.

Desktop simulation: hold the right mouse button and use WASD to offset your head within the local play area; Q/E turns your simulated head. This verifies avatar pose replication. It is not teleportation or gameplay locomotion.

VR: the camera and controllers use tracked poses. Aim the right controller at the lobby tiles and press the trigger. The world-space code keypad permits Relay joining without a desktop keyboard. The panel stays at a fixed offset from your virtual root rather than being attached to your head. Quest boundaries remain enabled.

## Personal sandboxes

The Generate Foundation command replaces the shared Arena and player prefab; use it only to reset the starter. Existing personal sandbox scenes are preserved.

After generation, each person has a copy of the same arena under Assets/CursedCrown/Tests/PersonN/PersonN_Sandbox.unity. Experiment in your own scene; coordinate edits to Arena.unity and FoundationPlayer.prefab. Use the common Arena scene for multiplayer integration so all devices see the same geometry.

| Owner | Build on | First integration point |
|---|---|---|
| 1: sessions/avatars | FoundationSession, FoundationAvatar, FoundationRig, menus | Harden lobby/service setup, remote avatar quality, late joining and connection failures |
| 2: arena/movement | Arena geometry; fixed foundation spawn layout | Replace basic slot allocation with the agreed spawn reservation service and host-validated teleportation |
| 3: weapons | Local rig RightHand and owner avatar; GameRules | Host-validated fire/recharge, cover raycast, one ValidatedHit per accepted shot |
| 4: health/respawn | IPlayerState, ISpawnService, ValidatedHit | Health/alive network state, hitboxes, HUD, one death event and reserved respawn |
| 5: crown/match | IMatchState, IRoundResettable, GameRules | Pickup/ownership, possession scoring, ready/start/timer/winner/reset |

Every person implements networking for their own system. Start with mocks when another owner's implementation is not ready. Keep public interfaces small and review changes to shared contracts before merging.

## Authority boundaries

- Clients own only their tracked input. Their avatar root/spawn is written by the host.
- A client can submit pose updates only for its own avatar. The host rejects non-finite coordinates, invalid rotations and poses outside the starter's configured play-area bounds; pose traffic is rate-limited.
- Host spawn slots are distinct and released on disconnect. These are initial placements; Person 2 must implement occupancy-aware gameplay spawning and teleport reservations.
- Session approval limits participation to five, including the host.
- A departing host ends the session. No host migration is provided.
- Pose replication runs at up to 20 sends/second; remote visuals interpolate. Owner visuals use local input immediately.
- Event callbacks are local process events. Gameplay state must still be replicated with network variables/RPCs by its owner.

## Validation commands

- Cursed Crown > 2 - Validate Foundation checks scene wiring, one local camera/listener/rig, required scripts and player prefab registration.
- Cursed Crown > 3 - Build Desktop Test Player produces Builds/CursedCrownTest.app on macOS.
- Cursed Crown > 4 - Run Local Six-Process Network Checks runs a host, four accepted clients and a rejected extra client; checks pose replication, distinct spawns and a client departure; then runs a separate host-departure check.
- Cursed Crown > Run All Foundation Checks validates, builds and runs those checks; it generates the foundation only if the Arena scene is missing.

The tests write per-process JSON plus a summary under the OS temporary directory; the Editor Console prints the report location. Raw service/authentication logs must not be committed. Record verified results in SETUP_STATUS.md and TEST_MATRIX.md.

## Android/Quest configuration

Android uses OpenXR with Meta Quest support, ARM64 and IL2CPP, minimum Android API 32 and target API 34. Quest 2 and Quest 3 are enabled in the OpenXR target settings. Desktop auto-start of XR is disabled for the Mac simulation; Android auto-start is enabled. Windows PC-VR development requires separately configuring a supported desktop OpenXR runtime.

Before distributing an APK, run XR Project Validation and verify a physical Quest 2 + Quest 3 session on the intended university network. Desktop tests do not certify tracking, controller interaction, Android installation, Wi-Fi isolation rules or headset performance.

## Intentionally assigned to the five owners

Teleportation, gun/ammunition logic, damage/health, death/respawn, crown ownership/scoring, ready/start, match end and restart remain implementation tasks. The crown object is a visual placeholder. The starter is a network foundation, not a completed laser-tag game.

Sources: Unity's installed 6000.6.2f1 package manual for version selection; Unity Multiplayer Services Relay/NGO integration documentation; Meta's current manifest recommendations. Package licenses remain distributed with their packages. Record adaptations and verification in CONTRIBUTIONS.md.
