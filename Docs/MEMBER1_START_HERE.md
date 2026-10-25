# Member 1: Carla's starting guide

Your part gets players connected and shows their head/hand movement. The guns, health, teleportation and crown rules belong to the other four members. Each member implements their own feature's network messages.

This branch contains a Member 1 implementation for review. It has not yet been compiled/run in Unity or exercised on a headset. Historical foundation passes on main do not certify these changes.

## 1. Get the project

1. Install GitHub Desktop and sign in with your invited GitHub account.
2. File > Clone repository > URL: `https://github.com/Rana-Ezzeddine/cursed-crown-vr`.
3. Use a local folder outside OneDrive, e.g. `C:\UnityProjects\cursed-crown-vr`.
4. Select the Member 1 pull request's branch in Current branch. Click Fetch origin first if it is missing.
5. In Unity Hub, install **6000.6.2f1**, then Projects > Add > Add project from disk. Select the folder containing Assets, Packages and ProjectSettings.
6. Keep the committed package versions. Wait until import/compilation finishes.
7. Open `Assets/CursedCrown/Scenes/Arena.unity`. Inspect Console for red errors.

Do not use Generate Foundation to apply this update. The existing scenes reference the same scripts, so the new menus load automatically. That generator replaces the shared Arena and player prefab.

## 2. See one working host

1. Press Play at the top of the Editor. Select the Game tab.
2. Choose Host LAN. Expect a hosting message and one player after the avatar spawns.
3. Hold the right mouse button and use WASD to offset the simulated head; Q/E rotates it. This is a pose preview, not the teleport feature.
4. Choose Leave session. Expect the lobby camera to return to its original position and Host/Join to become available after shutdown.
5. Host again to check that the session can restart.
6. Stop Play Mode before making saved scene changes.

If compilation fails, copy the first red Console error. If a network test fails, keep its summary and exact commit; do not describe a failed or unrun test as passing.

## 3. Run the automated desktop checks

Use Cursed Crown > Member 1 > Check Inputs and Pose Guards first.

Then select Cursed Crown > Run All Foundation Checks (build + network). It builds a desktop player appropriate for the operating system running the Editor: Windows x64, macOS or Linux x64. Install matching desktop build support if Unity requests it. The command validates the common Arena and may ask you to save current scene edits.

The suite launches ten processes over three stages:

1. Host plus four clients; reject a sixth participant; inspect distinct spawns and remote poses; observe a client leaving.
2. Host departure; the remaining client must end its session.
3. A client joins an already-running host, leaves, and rejoins without restarting the executable. It must receive a fresh avatar and valid roster/poses.

It takes about two minutes. The Console prints the temporary report folder. `summary.txt` reports assertions; a missing report is a failure. Logs can contain SDK account data, so do not commit raw logs. The binaries are under Builds/ and are ignored by Git.

These checks are LAN loopback tests. They do not establish actual Wi-Fi, Relay or headset compatibility.

## 4. Connect two desktop copies yourself

After building, run the executable separately while using Play Mode in Unity:

- Windows: `Builds/Windows/CursedCrownTest.exe`.
- macOS: `Builds/CursedCrownTest.app`.
- Linux: `Builds/Linux/CursedCrownTest.x86_64`.

Host LAN in one copy and Join LAN in the other. On the same computer use `127.0.0.1`. On different devices use the host's LAN IPv4 address, such as `192.168.1.10`, from its network settings. Both use port 7777 by default. On Windows, `ipconfig` also shows the adapter's IPv4 address.

Expect two avatars, distinct starting positions, and visible movement from the other copy. Confirm a late join sees the existing avatar, then leave/rejoin. University Wi-Fi can isolate devices; a failed connection needs investigation on the intended network.

## 5. Relay setup and test

Relay carries traffic through Unity's service so joining does not depend on entering a host's local IP. The host still decides game state; Relay is not a replacement host.

The repository currently has no team services project linked. Coordinate with Rana/the Unity organization owner to link the intended team project in the Editor's Services settings, grant the required project access, and check Authentication/Relay availability in that project's dashboard. These are account setup steps, not additional game code. No paid plan is enabled by this change.

After setup:

1. Run the same branch/build on both devices.
2. On the host, select Host Relay; wait for the code.
3. On the client, enter that code and select Join code.
4. Verify the roster, movement, late join, leaving, host loss and reconnect.
5. Try an invalid/expired code and cancel while connecting. Errors should leave a retryable menu, not an endless busy screen.
6. Leave Relay and try LAN to verify switching modes.

For two standalone Relay players sharing one computer's saved account data, launch with distinct optional `-ccAuthProfile` values, such as `member1-host` and `member1-client`. Use letters, digits, underscores or hyphens, maximum 30 characters. These profiles are local Authentication SDK identities, not GitHub accounts. The automated LAN suite does not need authentication profiles.

Cancellation prevents stale SDK results from starting a session. It does not cancel an HTTP request already sent; an unused Relay allocation can remain until the service expires it.

## 6. Quest tests

Install Android Build Support, Android SDK & NDK Tools and OpenJDK for this Editor through Unity Hub. Use the existing Android/OpenXR configuration and run XR Project Validation before building. Build/install an APK using the team's approved Quest developer setup.

The headset menu uses the right controller's trigger:

- Host LAN starts a local session.
- Edit IP selects numeric IPv4 entry. Digits, dot, Delete and Clear enter the other device's address; Join LAN connects.
- Edit Code selects alphanumeric Relay entry. Join Code connects; Host Relay creates a code.
- Leave/Cancel stops a session or pending connection.
- The menu hides after connecting. Press the right controller's **B** button to reopen/close it. Leaving as host ends the session for everyone.

Test Quest 2 plus Quest 3 early. Check head height/origin, both hand poses, controller selection, B-toggle, tracking loss/recovery and return to lobby. Keep headset boundaries enabled. Then repeat on the intended university network and finally with five players. APK creation/installation, tracking, menu layout, controller mapping, performance and real Relay remain pending until recorded on hardware.

## 7. Learn the code in this order

| File | What it does |
|---|---|
| Sessions/SessionInput.cs | Checks an entered IPv4 address or Relay code. |
| Sessions/FoundationSession.cs | Starts/stops sessions, tracks connection phases, times out stuck attempts, handles Relay, limits capacity and allocates initial spawn slots. |
| Sessions/FoundationMenu.cs | Desktop connection controls. |
| Sessions/FoundationWorldMenu.cs | Controller-operated connection controls and keypads. |
| Core/FoundationRig.cs | Reads local headset/controllers or desktop simulation. |
| Sessions/FoundationAvatar.cs | Sends the owner's valid poses to the host and displays replicated poses. |
| Editor/FoundationChecks.cs | Launches real desktop processes and checks their reports. |

In FoundationSession, read `StartLan`, `Connected`, `Leave`, `StopSession`, then `CompleteStop` before the async Relay path.

Connection phases are Idle/Failed -> Starting -> Connecting -> Connected. Stop/failure passes through Leaving and waits for NGO shutdown plus a frame before returning to Idle/Failed. Pending Relay continuations must still belong to the current attempt before touching transport or status. Service initialization/sign-in is shared across overlapping cancelled/retried attempts.

`PlayerJoined`/`PlayerLeft` are emitted once per local avatar spawn/despawn, including existing avatars received by a late joiner. A spawn event means the avatar is available on that peer; it is not a global message or a gameplay-ready signal. `SessionEnded` is emitted once after local shutdown, including intentional leaving. Only the host may run authoritative gameplay cleanup. Member 5 owns ready/start rules, match phases and late-join gameplay eligibility; Member 2 owns occupancy-aware gameplay spawning. Coordinate before changing their contracts or the shared scene/prefab.

## Verification available without Unity

With a .NET 8 SDK, run `dotnet run --project Tests/Member1/Member1.Tests.csproj` from the repository root. This compiles the actual SessionInput and FoundationSession files against explicit test doubles and exercises their logic. It does not compile the project against Unity assemblies or validate NGO serialization/networking. The test doubles are outside Assets and never ship in the game.

Record actual Unity/hardware outcomes in MEMBER1_VALIDATION.md. Read and explain the code, and record AI assistance in CONTRIBUTIONS.md before your individual demonstration.
