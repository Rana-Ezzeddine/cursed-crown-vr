# Starting material, contribution and AI record

Starting point: Rana's local VR Assignment 2 configuration, Unity 6000.6.2f1. XR/OpenXR settings, renderer configuration and baseline package manifests were reused. The Assignment 2 demo scene/gameplay and large template demonstrations were not copied. Unity package license terms remain with the package distributions; no open-source license is asserted for the team project.

Foundation assistance: ChatGPT/Codex authored repository setup, scene/prefab generation, tracking/desktop simulation, LAN and Relay session code, validated avatar pose replication, menus, rule constants, draft interfaces, automated checks and team documentation. Unity import/compile, desktop build, five-process networking, extra-client rejection and disconnect checks passed on 22 October 2026. Physical headsets, Android and Relay verification remain pending. All five members must read, explain, verify and adapt their contribution; this foundation must not be represented as their unaided original work.

Version selection used the installed Unity 6000.6.2f1 manual. Relay integration reference: https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo . Android manifest reference: https://developers.meta.com/vr/resources/publish-mobile-manifest/ . SDK implementation and package licenses remain with Unity packages. No external art assets were added.

| Member | Original work | Reused sources/licenses | AI assistance + verification | Relevant PRs |
|---|---|---|---|---|
| Person 1 | Pending | Pending | Pending | Pending |
| Person 2 | Pending | Pending | Pending | Pending |
| Person 3 | Pending | Pending | Pending | Pending |
| Person 4 | Pending | Pending | Pending | Pending |
| Person 5 — Rana | Crown possession/scoring, readiness, host match flow, synchronized deadline, scoreboard/ties, round reset and teammate adapters | Existing team foundation, Unity/NGO and primitive visuals; no third-party art | Codex-assisted implementation and tests; actual Unity build and five-process loopback checks passed. See PERSON5_VERIFICATION.md; headset/real placement integration pending; 30 real PlayerHealth integration checks passed | feature/person5-crown-match |

Record assets, tutorials, libraries, code snippets and adaptations when added. Every member should understand the authority model and complete a small live change/debug task.

Person 4 dependency: PlayerHealth.cs and its metadata were imported byte-for-byte from feature/p4-health-respawn at d00a040 for Person 5 integration. Credit remains with Person 4. Rana’s added work is the IPlayerState/round-validation adapter and isolated integration tests; no health-system authorship is claimed.
