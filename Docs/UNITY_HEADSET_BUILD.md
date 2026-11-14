# Unity headset build and test

Use branch `integration/final-game` and Unity 6000.6.2f1 with Android Build Support.

1. Open `Assets/CursedCrown/Tests/Person5/Person5_ArenaIntegration.unity`.
2. Open **File > Build Profiles**, select Android and switch platform if needed.
3. The shared scene list contains only the final integration scene. If using a custom
   build profile with an overridden scene list, select that same scene there too.
4. Enable Developer Mode/USB debugging on the Quest, connect a USB data cable,
   and authorize this computer inside the headset. University-managed headsets may
   require the lab administrator. A private release-channel installation does not
   grant debugging access.
5. Select the Quest under **Run Device**, then **Build And Run**. Choose a local APK path.
   The existing **Cursed Crown > Build Quest APK** menu only builds an APK; it does
   not install or launch it.

## Controls and acceptance

- Right-controller ray + index trigger: select menu buttons. The ray remains visible
  after hosting/joining and hides when right-controller tracking is lost.
- Select Host LAN on one device. Other devices use Edit IP and Join LAN with the host
  device's network IP. The network must permit peer-to-peer traffic.
- Everyone selects Ready; the host selects Start. A single player can start for a smoke test.
- Left thumbstick does not move the player. Right thumbstick: 30-degree snap turns;
  centre it before the next turn. Locomotion is teleport-only while alive/Playing.
- Hold right A, aim at clear floor, release to teleport. Network teleport is allowed
  only while alive during Playing. Offline foundation exploration is unchanged.
- Right trigger fires; left Y recharges. Right grip picks up a nearby crown.
- Right B toggles the connection menu.
- Arena: 30 x 30 metres with wider cover spacing and teleport range of 25 metres.
- Test two headsets: start, snap turn, teleport, wall/cover blocking, four accepted hits causing death,
  no teleport while dead, respawn, crown pickup/drop/scoring, round expiry and restart.

## Automated verification

`CursedCrown.Editor.QuestReadinessChecks.Run` runs a batch Play Mode check of the
saved final scene (invoke without `-quit`). It starts a real local host, simulates
tracked-controller flags/poses to click Ready, checks pointer visibility and tracking
loss, and directly exercises the same server handler used by teleport RPCs for wrong
owner, Lobby, dead, Playing, non-finite destination and Finished requests.
It uses shortened rounds and explicitly changes host health for the death gate.
It does not simulate real headset hardware or remote RPC transport.

Also run `python3 Tools/check_match_rules.py`, the Member1 .NET test project,
and `python3 -m unittest discover -s Tools -p 'test_*.py'`.
Physical controller feel, device performance, and a full two-headset match remain
manual acceptance checks; passing compilation does not certify those.

## Verification on 14 November 2026

- Saved-scene Unity Play Mode: 60 checks passed, including broad floor coverage, walking
  speed/ownership validation, headset-pivot snap turns, real Ready-button selection,
  pointer tracking recovery, and the server teleport gates listed above. No runtime errors.
- Pure C# rules: 1,010 clock assertions and 536 crown/match assertions passed.
- Member1: 26 session tests and 11 runner regression tests passed.
- Python report/command I/O: 13 tests passed.
- An initial test harness compile error and a preliminary run affected by Unity Search
  startup indexing were fixed before the clean Play Mode run; neither counted as a pass.
- Subsequent builds were installed and launched on Quest 2 through Unity Build And Run.
  Full two-headset acceptance is still pending.
- Fresh Android ARM64/IL2CPP APK build succeeded after these fixes; APK v2 signature verified.

## Presentation update

- Obsidian, gold, and emerald arena decoration; sculpted crown and spawn medallions.
- Ready state, hover enlargement, press flash, disabled actions, and controller haptics.
- Match card hides during Playing and returns for results/restart.
- Local HUD shows segmented vitality, damage feedback, timer, crown carrier, and respawn state.
- Preview renders and Play Mode checks verify the menu remains visible through arena geometry.
- Verify text clarity, haptics, frame rate, damage feedback, and complete round/restart in the headset.

## Ruined temple update

Four stone chambers have front and side entrances, partial roofs, and interior cover.
Columns, arches and walls are solid; all gameplay remains on the ground floor.
Scanned stone albedo/normal/AO maps are from Poly Haven (CC0); sources are recorded
in Assets/CursedCrown/Resources/TempleTextures/SOURCES.txt. Android uses 1K ASTC textures.
The crown uses aged gold, jade settings and beaded trim. Materials are included in
Resources so their normal-map shader variants are available in Android builds.

Scoring: one point per whole second of possession; four-minute rounds; highest score
wins, including ties. No points for shooting. Health starts at 100, validated hits
remove 10 (ten hits to defeat a full-health player), death drops the crown at a safe drop point, and respawn restores 100 after
five seconds if a safe spawn is available. Earned points persist until round restart.

Temple tests cover clear interiors, both doorways, rejection of wall destinations,
and physical line-of-sight blocking. Full multiplayer pursuit/cover and Quest frame
rate still need on-device acceptance.

## Teleport-only controls

Hold A to reveal safe landing rings across the courtyard, corridors and all four chambers.
Aim at clear floor and release A to teleport; the rings are guidance, not the only
allowed destinations. Small near-obstacle aiming errors snap to nearby clear floor
when unobstructed. Right-stick snap turning remains. Left-stick walking is removed
from both client input and server RPC/update handling. Physical headset tracking remains.

Teleport-only validation found 87 safe guidance locations. All four chamber interiors
and both entrances passed destination validation; near-cover assistance and removal
of the walking RPC/update loop passed.

Eight close altar approach rings now supplement the broad grid. The saved-scene
test teleports to every approach, faces the altar, supplies a tracked right hand
at crown height, and verifies the actual server pickup handler accepts possession.
Far-away and inside-altar pickup requests remain rejected. HUD shows hand-to-crown
distance and a grip prompt when within range.

## Temple character selection

After Host LAN or Join LAN, point at the character-name button beside the lobby
card and press the right trigger to cycle Sun Guardian, Jade Sentinel, Crimson
Warden, Moon Ranger and Royal Keeper. A miniature preview shows the selection.
Then press Ready. Selection is cosmetic, allows duplicates and is locked during
play/results; return to the lobby to change it. Desktop testing has the same
selection button. The host validates the owner, index and lobby phase and
replicates the choice, including to late joiners. Existing hitboxes are unchanged.
Remote players have a labelled humanoid with tracked head/hands and approximated
arms/body/legs, not full-body tracking. Your own head/body are hidden to avoid
blocking your view. Player labels currently use player IDs, not entered names.
These are original procedural stylized models, not scanned realistic people.
All peers must rebuild with this version before testing together.

Character update verification: 69 production-scene Unity host checks passed,
including all five choices, invalid-index/wrong-owner rejection, selection locked
during play, humanoid creation, and the existing teleport/crown/round checks.
Rendered lobby preview inspected. These use simulated poses and one local host;
they do not certify multi-headset replication, late joining or Quest performance.
The installed APK must be rebuilt to include this feature.

## Temple UI, player names and final leaderboard

In the connected lobby select **NAME**, type with the right-controller trigger on
the in-world keyboard, then select **SAVE NAME**. SPACE, DELETE and CLEAR are
available. Desktop Play Mode also has a name text field. Names allow up to 16
Latin letters, digits, spaces, hyphens or apostrophes; blank names are rejected.
A default Player number is used until a name is entered. Names can only change in
the lobby and are host-validated against the requesting client's identity.
The name is replicated with the match roster, so it remains in round results if
its player disconnects. Duplicate names receive a player-number suffix for
identification. Names last for the current session, not across app launches.

The Hall of Champions ranks crown points from highest to lowest. Equal scores
share a rank and tied leaders share victory. Avatar labels and crown-carrier HUD
use these names. Menus use obsidian/gold frames with ivory text and hover/press
feedback. The HUD includes remaining shots, empty-gun guidance and recharge status.
As with any changed network schema, all participating headsets need the same new
build before joining together. Final physical headset readability and multi-client
name/leaderboard acceptance remain necessary.

UI/name verification: 82 Unity production-scene checks passed. New coverage
includes constrained/safe names, blank/unknown-sender rejection, saving through
the real controller ray, lobby-only changes, winner names, score ordering, tied
ranks, departed rows and duplicate-name suffixes. Keyboard and five-row results
renders were visually inspected; the results fixture uses sample scores.
