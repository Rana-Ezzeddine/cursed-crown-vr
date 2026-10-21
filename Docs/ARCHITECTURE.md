# Authority and handoffs

One host owns gameplay state. Clients request actions; the host validates and replicates accepted results. Relay transports messages and does not decide hits or scores. A departing host ends the session; no host migration.

Client-authoritative input is limited to tracked head/hand poses. Validate plausibility and use host-owned virtual root positions for gameplay. Never trust client-supplied damage, score, charge, player ID or hit target. Resolve sender identity from RPC receive metadata.

| Owner | Provides | Consumers |
|---|---|---|
| Person 1 | player IDs, avatar roots/poses, player-connected/player-left events | Everyone |
| Person 2 | host spawn reservations, accepted teleport, valid crown-drop positions | Persons 4 and 5 |
| Person 3 | ValidatedHit, host charge state, shot effects | Person 4 |
| Person 4 | host health/alive state, one death event per death | Persons 2, 3 and 5 |
| Person 5 | match phase, round ID, round-reset event | Everyone |

SystemContracts.cs supplies initial interfaces and a hit payload; it does not implement RPCs or event dispatch. Agree event subscriptions and DTO serialization in the first integration PR. Use isolated mocks while other systems are incomplete.

Host processes competing pickups/teleports serially. Reserve destinations immediately when accepting a move or spawn so two requests cannot claim the same location. Validate a safe occupied volume, floor, walls and bounds. Release reservations on movement/death/disconnect. Reject actions outside Playing or while dead. Track round ID and per-shooter sequence to reject stale/duplicate shots. Cover and hitboxes share a raycast mask; the first obstruction wins. No self-hits or client-awarded damage.

Death transition happens once, drops the crown once, disables firing/pickup/teleport and schedules respawn after three seconds. Reserve a free fixed spawn before restoring health and charge; wait when none is free. Disconnect cleanup is idempotent. Drop the carrier's crown at a valid point, otherwise the pedestal. On host loss, all clients return to an explanatory session-ended view.

Restart: Person 5 changes round ID, clears scores/timer/crown and signals reset. Each owner clears its own pending timers, reservations, shot sequences and player state. Do not accept requests from the previous round.

## Proposed rules

Four-minute free-for-all; 100 HP; 25 damage per accepted hit; 10 shots; manual recharge takes two seconds; respawn after three seconds. Claim crown by controller button within one metre and unobstructed line of sight. It floats above the carrier. No throwing or manual dropping. Carrier can shoot and teleport. Score one point per completed second of continuous possession; a new possession starts a fresh counter. No elimination points. Highest crown score wins; tied leaders share the win. Host starts only after all connected players are ready; final demo uses five. Handle early disconnects without hanging the match.

These values are tuning proposals and can change independently of the instructor requirements.
