# Starting material, contribution and AI record

Starting point: Rana's local VR Assignment 2 configuration, Unity 6000.6.2f1. XR/OpenXR settings, renderer configuration and baseline package manifests were reused. The Assignment 2 demo scene/gameplay and large template demonstrations were not copied. Unity package license terms remain with the package distributions; no open-source license is asserted for the team project.

Foundation assistance: ChatGPT/Codex authored repository setup, scene/prefab generation, tracking/desktop simulation, LAN and Relay session code, validated avatar pose replication, menus, rule constants, draft interfaces, automated checks and team documentation. Unity import/compile, desktop build, five-process networking, extra-client rejection and disconnect checks passed on 22 October 2026. Physical headsets, Android and Relay verification remain pending. All five members must read, explain, verify and adapt their contribution; this foundation must not be represented as their unaided original work.

Version selection used the installed Unity 6000.6.2f1 manual. Relay integration reference: https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo . Android manifest reference: https://developers.meta.com/vr/resources/publish-mobile-manifest/ . SDK implementation and package licenses remain with Unity packages. No external art assets were added.

| Member | Original work | Reused sources/licenses | AI assistance + verification | Relevant PRs |
|---|---|---|---|---|
| Person 1 (Carla) | Session/lobby extension prepared for review; Carla's explanation, adaptation and Unity/device verification pending | Existing team foundation and Unity SDKs | Codex wrote lifecycle/menu/avatar/test changes and beginner guide; standalone logic checks passed; actual Unity/Relay/Quest tests pending. Do not claim unaided authorship. | Member 1 draft PR |
| Person 2 | Pending | Pending | Pending | Pending |
| Person 3 (Jad) | Host-validated weapon firing/recharge, raycasts, charge and tracer effects; reviewed round/tracking safeguards and integration with health/match systems | Existing team foundation, Unity Netcode and XR input APIs; P3 weapon sources from `feature/p3-weapons` | ChatGPT/Codex assisted with implementation, debugging and integration checks. Jad reviewed changes and ran Unity build/runtime checks (46 arena checks and 13 regression tests passed); detailed physical headset/weapon acceptance remains pending. | PRs #2, #7, #8 |
| Person 4 | Host-authoritative player health, HUD, validated damage, death and five-second respawn; P3 hit and P5 round/death integration | Original `PlayerHealth.cs` and `HealthHUD.cs` from `feature/p4-health-respawn`; current team branch preserves those sources unchanged | Person 4 reported a successful two-instance test of damage, death, respawn and match restart on `feature/p4-team-integration`. The new final-game integration bridge is on `feature/p4-final-integration` and still needs a combined P2 production-spawn runtime rerun. Codex-assisted integration is documented. | P4 health/respawn draft PR; P4 final-integration draft PR |
| Person 5 — Rana | Crown possession/scoring, readiness, host match flow, synchronized deadline, scoreboard/ties, round reset and teammate adapters | Existing team foundation, Unity/NGO and primitive visuals; no third-party art | Codex-assisted implementation and tests; actual Unity build and five-process loopback checks passed. See PERSON5_VERIFICATION.md; headset/real placement integration pending; 30 real PlayerHealth integration checks passed | feature/person5-crown-match |

Record assets, tutorials, libraries, code snippets and adaptations when added. Every member should understand the authority model and complete a small live change/debug task.

Person 4 dependency: PlayerHealth.cs and its metadata were imported byte-for-byte from feature/p4-health-respawn at d00a040 for Person 5 integration. Credit remains with Person 4. Rana’s added work is the IPlayerState/round-validation adapter and isolated integration tests; no health-system authorship is claimed.


P4/P3/P5 integration note: `feature/p4-final-integration` adds P3's `NetworkGun` and `GunEffects` to the existing production `FoundationPlayer`, and attaches `Person4MatchHealthIntegration` to the P5 arena match prefab. The bridge forwards host-validated P3 hits through `Person5HealthBridge` to the unchanged P4 `PlayerHealth`, synchronizes gun/health round IDs with `MatchController.RoundId`, and reports player deaths to the match. It deliberately does not implement `ISpawnService`; P2's existing `ArenaMatchServices` remains the production service. The final combined branch still requires a fresh two-client Unity run before merge.

November 2026 character-selection addition (Rana-requested, Codex-assisted):
`TempleCharacter.cs` generates five stylized humanoid temple characters from Unity
primitive meshes, with original procedural arrangement/materials and no external
character assets. `FoundationAvatar` synchronizes a host-validated cosmetic choice;
`MatchPresentation` provides lobby selection and a preview. These additions extend
the team's existing player/networking work rather than replacing its authorship.
Heads/hands follow tracked poses; limbs are approximated. Multi-headset visual and
late-join acceptance testing remains necessary. See UNITY_HEADSET_BUILD.md.

Rana-requested UI/name update, Codex-assisted: original procedural obsidian/gold
menu decoration (`TempleUi`), in-world name keyboard, host-validated replicated
player names in match rows, named avatar/HUD labels and ranked/tied final results.
No external UI art/font assets were introduced; the built-in Unity runtime font
is reused. Automated Unity tests exercise name validation, actual controller-ray
save, phase restrictions and a five-row leaderboard fixture. The fixture is not
evidence of five connected headsets; multi-headset acceptance remains pending.
