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
- Hold right A, aim at clear floor, release to teleport. Network teleport is allowed
  only while alive during Playing. Offline foundation exploration is unchanged.
- Right trigger fires; left Y recharges. Right grip picks up a nearby crown.
- Right B toggles the connection menu.
- Test two headsets: start, teleport, cover blocking, four accepted hits causing death,
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

- Saved-scene Unity Play Mode: 15 checks passed, including real Ready-button selection,
  pointer tracking recovery, and the server teleport gates listed above. No runtime errors.
- Pure C# rules: 1,010 clock assertions and 536 crown/match assertions passed.
- Member1: 26 session tests and 11 runner regression tests passed.
- Python report/command I/O: 13 tests passed.
- An initial test harness compile error and a preliminary run affected by Unity Search
  startup indexing were fixed before the clean Play Mode run; neither counted as a pass.
- Hardware deployment and multiplayer headset acceptance were not performed: no
  authorized headset was connected to ADB during this work.
- Fresh Android ARM64/IL2CPP APK build succeeded after these fixes; APK v2 signature verified.
