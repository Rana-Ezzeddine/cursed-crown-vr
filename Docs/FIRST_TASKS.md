# First tasks and ownership

Rana handles repository, exact versions, starter configuration and teammate access separately from the five roles. Rana may also occupy one of the five roles. Teammate usernames and role assignments are not yet supplied.

| Person | Owns | First deliverable | Depends on |
|---|---|---|---|
| 1 | Sessions and avatars | Local host/client connection, five-player cap, IDs, tracked head/hands, join/leave events and host-loss UI | Networking packages + free service setup if Relay used |
| 2 | Arena and movement | Refine arena; host spawn reservations, valid drop positions and teleport-request validation | Mock player IDs initially |
| 3 | Guns and shots | Controller fire/recharge; host shooter/charge/fire-rate validation; cover-blocked raycast; one hit event | Mock alive state and player identity |
| 4 | Health and respawn | Hitboxes, host health/alive state, one death event, action gating, HUD, timed reserved respawn | Mock validated hits; Person 2 spawn contract |
| 5 | Crown and match | Host pickup/ownership, full-second scoring, ready/start/timer/winner/restart and scoreboard | Mock death/leave events; Person 2 drop positions |

Everyone implements networking for their own system. Person 1 does not own all networking.

## Setup gate (Rana)

- [x] Inspect installed editor and existing course manifest/lockfile.
- [x] Create private repository without a public license.
- [x] Prepare XR/URP configuration and source scaffolding.
- [ ] Complete a successful fresh Unity import and compile.
- [ ] Install/resolve networking packages and commit exact final versions.
- [ ] Link a suitable Unity services project and verify free service availability; no paid plan is authorized.
- [ ] Add teammates after verified usernames are provided.
- [ ] Reserve five headsets; confirm deadline when announced.

## Early integration gates

- [ ] Agree event API and shared scene/player prefab editing schedule.
- [ ] Test two headsets, including Quest 2 + Quest 3, on the university network.
- [ ] Two-player loop: teleport, cover, shots, charge, damage, death, respawn, crown exchange.
- [ ] Start/timer/winner/restart, competing actions and simultaneous shots.
- [ ] Five-player mixed-device test, disconnect/host-loss test, Quest 2 profiling.
- [ ] Feature freeze, exact APK test, technical report and demo rehearsal.

Use branches such as feature/p1-sessions and feature/p2-arena. Before editing shared contracts or scene/prefab files, coordinate in the team channel. Each owner documents reset behavior and invalid-request handling. Review effort at milestones and redistribute small tasks when needed.
