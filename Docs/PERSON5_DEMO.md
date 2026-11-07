# Explain and demonstrate Person 5

## What Rana's system does

The host owns one crown and one match. A player requests readiness or pickup; the host derives the player ID from the RPC sender, checks the current round and game rules, and publishes accepted state. Clients render that state. A client never sends a score or chooses another player's identity.

`MatchClock` controls Lobby → Playing → Finished → Lobby. `CrownMatchRules` controls readiness, possession and points without Unity dependencies. `MatchController` connects those rules to NGO, tracked poses, physics, sessions and teammate services. `MatchPresentation` draws the crown and scoreboard and sends input requests. `MatchBootstrap` registers/spawns the match object because the foundation does not use automatic network scene management.

## Open your contribution

1. Open this repository in Unity 6000.6.2f1.
2. Open `Assets/CursedCrown/Tests/Person5/Person5_Sandbox.unity`.
3. Run **Cursed Crown → Person 5 → Validate Match Sandbox**.
4. Press Play and select **Host LAN**. A second current sandbox build joins `127.0.0.1` on this Mac, or the host's LAN address on a second computer.
5. Both players select **Ready**. Only the host can start. The scoreboard displays phase, remaining seconds, carrier and each player's score. At Finished it displays all tied winners. The host's Restart returns everyone to the lobby without disconnecting them.

The saved sandbox has 240-second rounds and explicitly permits mock alive state and desktop hand simulation. The ordinary desktop rig cannot walk from its initial spawn to the centre: it only simulates bounded physical head movement. Use the automated test fixture to demonstrate reachable desktop pickup, or integrate Person 2's actual movement. Do not enlarge the one-metre pickup range to hide missing movement.

In VR, aim at lobby/match tiles and press the right trigger. Crown pickup uses the **right grip button**. The host requires the right hand within one metre and a clear reach; tracking is mandatory when AllowDesktopPickup is disabled. Headset input and readability still need physical verification.

## Explain scoring with an example

Hold for 2.7 seconds: earn 2 points. Drop: discard the remaining 0.7 seconds. Hold again for 0.6 seconds: earn no new point. The score remains 2. Scoring uses the host's clock, never frame count. At the deadline, elapsed possession is capped to that deadline, so a slow frame cannot award overtime points.

## Integration checklist

- Person 1: keep `FoundationSession` join/leave events and a host-validated avatar pose. All peers need the same updated pose protocol containing RightTracked.
- Person 2: provide `ISpawnService.GetValidCrownDrop`; the returned position is the crown's visual centre. Missing/invalid placement returns it to the pedestal.
- Person 4: provide `IPlayerState`. Call `NotifyPlayerDied(playerId, roundId)` on the host when death occurs, using the round that produced the event. Host polling also drops a carrier whose alive state becomes false.
- Everyone: gate actions on `IMatchState.Phase`, carry `MatchController.RoundId` with delayed/network actions, and implement `IRoundResettable` to cancel old timers/reservations and replicate your reset. Services installed dynamically should call `RefreshServices`.
- Disable both AllowMockAlive and AllowDesktopPickup for real integration acceptance. The opt-in probe is test code, not a replacement for teammates' systems.

## Small live modification/debug exercise

Change the sandbox prefab's RoundSeconds in the Inspector to 15, run two peers from the rebuilt sandbox, and explain why both timers use the same host deadline. Restore 240 afterwards.

For rejected pickup, check in order: Playing phase, matching round, crown unowned, connected sender, alive service, accepted right-hand tracking/pose, one-metre distance, and physics obstruction. A hand inside a wall is rejected too. For a missing crown on a client, check prefab registration and that MatchController.Instance is spawned. For failed start, inspect every connected player's Ready flag, including late joiners in the lobby.

## Honest demonstration limits

Read PERSON5_VERIFICATION.md for executed tests. Desktop loopback and Editor Play Mode do not prove Android installation, Quest tracking/controllers, mixed Quest 2/3 networking, five-headset performance, real health/teleport integration, Relay, or university Wi-Fi.
