# Person 5: crown and match implementation

Status (29 October 2026): Unity compilation, sandbox build, five-desktop-process networking, a full 240-second match and Editor Play Mode pass. Headsets remain unverified. See PERSON5_VERIFICATION.md and PERSON5_TEAM_INTEGRATION.md.

## Verified independently

Run `python3 Tools/check_match_rules.py` using the SDK bundled with Unity 6000.6.2f1. Set UNITY_DOTNET_SDK if Unity is installed elsewhere. The checks compile the actual GameRules, MatchClock and CrownMatchRules sources using Roslyn and run deterministic tests. They do not load Unity or Netcode.

## Prepare the isolated sandbox

1. Use Unity 6000.6.2f1 and let package resolution finish.
2. Run `Cursed Crown > Person 5 > Prepare Match Flow Sandbox`.
3. This saves only Person5_Sandbox.unity and creates MatchTest.prefab beside it. It leaves the shared Arena and player prefab alone. Rerunning preserves an existing match prefab and its tuning.
4. The sandbox prefab starts with 240-second rounds with AllowMockAlive and AllowDesktopPickup enabled. This lets Person 5 test independently of Person 4. The production default for MatchController is 240 seconds with mock health disabled.
5. Start host/client instances from the same prepared sandbox. Both players must mark ready before the host can start.

The match prefab is registered on each device before joining and spawned only by the host. This is necessary because the foundation disables automatic NGO scene management. Only the host writes the replicated match snapshot and player rows. Late joiners receive those variables from NGO.

Desktop controls sit alongside the original lobby panel. VR controls sit to the left of the original lobby panel; point the right controller and press the trigger. The right grip button requests crown pickup, keeping it separate from the gun/menu trigger. The host requires the right tracked hand to be within one metre with no intervening solid collider. Move physically within the safe play area or use Person 2's locomotion once integrated. Desktop simulation remains the foundation's right-mouse/WASD head-offset simulation; it is not a full movement implementation.

## Team integration

- Person 4: the provided Person5HealthBridge exposes PlayerHealth through IPlayerState when attached beside MatchController; the host checks IsAlive before pickup and while carrying. For immediate death handling, call `MatchController.Instance.NotifyPlayerDied(playerId, roundId)` on the host. Repeated and stale-round notifications are safe. Disable AllowMockAlive and AllowDesktopPickup before integration acceptance.
- Person 2: implement ISpawnService.GetValidCrownDrop. Without it, or if it returns non-finite coordinates, a dropped crown returns to the pedestal. The service owns reachable placement and the visual centre/ground clearance.
- Person 1: FoundationSession.PlayerJoined/PlayerLeft are already connected on the host. Host departure uses the foundation's session termination behavior; no host migration is added.
- All owners: read IMatchState.Phase to gate gameplay. Implement IRoundResettable to reset your own host-owned state when the match returns to Lobby. Each owner must replicate that reset and cancel pending round actions. Ready/pickup requests already carry the round number; weapon and respawn requests must do the same in their own systems.

Points are earned per completed second of each possession; fractional seconds are discarded on drop. Scores survive disconnects until reset. Ties share victory, including an all-zero round. Restart resets readiness, scores, crown and timer, removes departed participants and increments the round number while keeping connected clients.

## Acceptance coverage

The following desktop checks have passed; headset items remain pending.

- Compile runtime/editor scripts; generate the sandbox and confirm no missing references.
- Two clients: readiness synchronization, host-only start/restart, shared countdown, duplicate start rejection.
- Crown: near/far pickup, blocked reach, competing pickup, carrier display and matching scores.
- Death/disconnect: release exactly once, safe fallback position, pickup by another player.
- Deadline/late join/restart: no overtime scoring, late join receives current state, ties, multiple rounds, reconnect sessions.
- Quest 2/3: controller buttons, panel readability, crown position, profiling and full five-headset integration.

Compilation, sandbox build, standalone networking and Editor Play Mode have passed. See PERSON5_VERIFICATION.md for exact evidence and remaining hardware checks. Recording was cancelled by the user.

The avatar pose protocol now includes RightTracked. Use the same build on every peer. Production pickup rejects untracked right-hand poses; the host still trusts client tracking reports only within the existing pose plausibility limits. Call RefreshServices after dynamically installing/replacing teammate services; services are rediscovered on spawn and round reset.

## Repeatable verification commands

Use `Cursed Crown > Person 5 > Build Desktop Sandbox` for `Builds/Person5Sandbox.app`; the foundation build menu targets Arena and is not this test.

- `python3 Tools/check_person5_network.py` launches independent local host/client players, then a late joiner, and drives real ready/pickup RPCs.
- Add `--full-duration` to include a complete 240-second possession round.
- Add `--editor` to use Unity Editor Play Mode for the host and the current standalone build for clients. Close this repository's Editor first; do not close another project's unsaved scene.

The opt-in driver requires a development build and `-p5Mode`, runs only in Person5_Sandbox, and uses local JSON commands/reports in an OS temporary folder. It is absent from release builds. Its host-only placement commands simulate Person 2 movement; its IPlayerState and ISpawnService methods are explicit mocks. Ready/pickup commands use the ordinary production RPC entry points and network sender IDs. Test round durations are changed only at runtime. The saved sandbox prefab remains 240 seconds.

The separate Person5_HealthIntegration scene uses Person 4’s unchanged real PlayerHealth with Rana’s IPlayerState bridge. See PERSON5_TEAM_INTEGRATION.md for the exact dependency commit, guarded hit boundary and remaining Person 2 service gap.
