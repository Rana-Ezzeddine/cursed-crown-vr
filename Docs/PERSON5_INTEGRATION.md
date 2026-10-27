# Person 5: crown and match implementation

Status: pure C# rules compiled and tested. Unity adapters and presentation are drafts awaiting Unity compilation, Play Mode, networking, and headset tests. Do not treat these drafts as a verified build.

## Verified independently

Run `python3 Tools/check_match_rules.py` using the SDK bundled with Unity 6000.6.2f1. Set UNITY_DOTNET_SDK if Unity is installed elsewhere. The checks compile the actual GameRules, MatchClock and CrownMatchRules sources using Roslyn and run deterministic tests. They do not load Unity or Netcode.

## Prepare the isolated sandbox

1. Use Unity 6000.6.2f1 and let package resolution finish.
2. Run `Cursed Crown > Person 5 > Prepare Match Flow Sandbox`.
3. This saves only Person5_Sandbox.unity and creates MatchTest.prefab beside it. It leaves the shared Arena and player prefab alone. Rerunning preserves an existing match prefab and its tuning.
4. The sandbox prefab starts with 30-second rounds and AllowMockAlive enabled. This lets Person 5 test independently of Person 4. The production default for MatchController is 240 seconds with mock health disabled.
5. Start host/client instances from the same prepared sandbox. Both players must mark ready before the host can start.

The match prefab is registered on each device before joining and spawned only by the host. This is necessary because the foundation disables automatic NGO scene management. Only the host writes the replicated match snapshot and player rows. Late joiners receive those variables from NGO.

Desktop controls sit alongside the original lobby panel. VR controls sit to the left of the original lobby panel; point the right controller and press the trigger. Away from the match panel, trigger requests crown pickup. The host requires the right tracked hand to be within one metre with no intervening solid collider. Move physically within the safe play area or use Person 2's locomotion once integrated. Desktop simulation remains the foundation's right-mouse/WASD head-offset simulation; it is not a full movement implementation.

## Team integration

- Person 4: implement IPlayerState on an active scene component; the host checks IsAlive before pickup and while carrying. For immediate death handling, call `MatchController.Instance.NotifyPlayerDied(playerId)` on the host. Repeated notifications are safe. Disable AllowMockAlive before integration acceptance.
- Person 2: implement ISpawnService.GetValidCrownDrop. Without it, or if it returns non-finite coordinates, a dropped crown returns to the pedestal. The service owns reachable placement and the visual centre/ground clearance.
- Person 1: FoundationSession.PlayerJoined/PlayerLeft are already connected on the host. Host departure uses the foundation's session termination behavior; no host migration is added.
- All owners: read IMatchState.Phase to gate gameplay. Implement IRoundResettable to reset your own host-owned state when the match returns to Lobby. Each owner must replicate that reset and cancel pending round actions. Ready/pickup requests already carry the round number; weapon and respawn requests must do the same in their own systems.

Points are earned per completed second of each possession; fractional seconds are discarded on drop. Scores survive disconnects until reset. Ties share victory, including an all-zero round. Restart resets readiness, scores, crown and timer, removes departed participants and increments the round number while keeping connected clients.

## Unity verification still required

- Compile runtime/editor scripts; generate the sandbox and confirm no missing references.
- Two clients: readiness synchronization, host-only start/restart, shared countdown, duplicate start rejection.
- Crown: near/far pickup, blocked reach, competing pickup, carrier display and matching scores.
- Death/disconnect: release exactly once, safe fallback position, pickup by another player.
- Deadline/late join/restart: no overtime scoring, late join receives current state, ties, multiple rounds, reconnect sessions.
- Quest 2/3: controller buttons, panel readability, crown position, profiling and full five-headset integration.

Unity adapters have not passed these checks in this session. Recording was cancelled by the user.
