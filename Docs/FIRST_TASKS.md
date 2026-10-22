# First tasks and ownership

Rana handles repository, exact versions, starter configuration and teammate access separately from the five roles. Rana may also occupy one of the five roles. Teammate usernames and role assignments are not yet supplied.

| Person | Owns | First deliverable | Depends on |
|---|---|---|---|
| 1 | Sessions and avatars | Extend verified session/pose baseline: Relay setup, lobby readiness, late join, friendly failures, avatar polish and headset/network compatibility | Existing foundation + suitable service setup if Relay used |
| 2 | Arena and movement | Refine arena; host spawn reservations, valid drop positions and teleport-request validation | Mock player IDs initially |
| 3 | Guns and shots | Controller fire/recharge; host shooter/charge/fire-rate validation; cover-blocked raycast; one hit event | Mock alive state and player identity |
| 4 | Health and respawn | Hitboxes, host health/alive state, one death event, action gating, HUD, timed reserved respawn | Mock validated hits; Person 2 spawn contract |
| 5 | Crown and match | Host pickup/ownership, full-second scoring, ready/start/timer/winner/restart and scoreboard | Mock death/leave events; Person 2 drop positions |

Everyone implements networking for their own system. Person 1 does not own all networking.

## Setup gate (Rana)

- [x] Inspect installed editor and existing course manifest/lockfile.
- [x] Create private repository without a public license.
- [x] Prepare XR/URP configuration and source scaffolding.
- [x] Complete a successful clean Unity import and compile.
- [x] Install/resolve compatible networking packages and save the real lockfile.
- [x] Generate scenes/prefab; build desktop player and verify five-player admission, poses and disconnects.
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

## Balance and parallel work

The starter reduces Person 1's initial plumbing work. Person 1 now owns lobby hardening, late-join/error tests, service integration and the final session compatibility matrix. Each owner budgets comparable effort for implementation, networking, invalid-request/reset tests, peer review and report/demo evidence. Rebalance at the two-player integration milestone based on remaining work, not file count.

Do not wait for all features before integrating: merge small working slices daily. Start with local/mock adapters, then connect the agreed contracts. Every owner first tests their own sandbox, then opens a PR with a repeatable acceptance test. The shared Arena is the integration scene; sandbox edits do not automatically appear there.
