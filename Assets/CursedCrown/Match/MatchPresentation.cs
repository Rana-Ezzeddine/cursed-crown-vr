using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CursedCrown
{
    // Scene-local presentation; authoritative state remains on MatchController.
    public sealed class MatchPresentation : MonoBehaviour
    {
        public Material CrownMaterial, PanelMaterial;
        Transform crown, panel;
        TextMesh label;
        readonly Dictionary<Collider, Action> buttons = new();
        readonly List<Material> ownedMaterials = new();
        readonly List<Collider> buttonColliders = new();
        Renderer[] panelRenderers;
        bool ready;
        uint lastRound;
        MatchController previousMatch;
        void Start()
        {
            crown = new GameObject("Crown display").transform;
            crown.SetParent(transform, false);
            // A gold band with six raised points, with no pickup collider to block rays.
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(part.GetComponent<Collider>());
                part.transform.SetParent(crown, false);
                part.transform.localPosition = new Vector3(Mathf.Sin(angle) * .16f, i % 2 == 0 ? .07f : .015f, Mathf.Cos(angle) * .16f);
                part.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
                part.transform.localScale = new Vector3(.095f, i % 2 == 0 ? .18f : .07f, .04f);
                part.GetComponent<Renderer>().sharedMaterial = CrownMaterial;
            }
            panel = new GameObject("Match controls").transform;
            panel.SetParent(transform, false);
            var text = new GameObject("Match status");
            text.transform.SetParent(panel, false);
            label = text.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = .0042f;
            AddButton("READY / NOT READY", -.72f, () => { ready = !ready; MatchController.Instance?.SetReady(ready); });
            AddButton("START (HOST)", -.94f, () => MatchController.Instance?.TryStartMatch());
            AddButton("RESTART (HOST)", -1.16f, () => MatchController.Instance?.TryReturnToLobby());
            panelRenderers = panel.GetComponentsInChildren<Renderer>();
        }
        void AddButton(string title, float y, Action action)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.SetParent(panel, false);
            tile.transform.localPosition = new Vector3(0, y, 0);
            tile.transform.localScale = new Vector3(1.25f, .18f, .035f);
            var material = new Material(PanelMaterial);
            ownedMaterials.Add(material);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            var collider = tile.GetComponent<Collider>();
            buttons.Add(collider, action); buttonColliders.Add(collider);
            var go = new GameObject(title);
            go.transform.SetParent(panel, false);
            go.transform.localPosition = new Vector3(0, y, -.025f);
            var text = go.AddComponent<TextMesh>();
            text.font = label.font;
            text.GetComponent<Renderer>().sharedMaterial = text.font.material;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64; text.characterSize = .003f; text.text = title;
        }
        void Update()
        {
            var match = MatchController.Instance;
            bool active = match != null && match.IsSpawned;
            if (active && (previousMatch != match || lastRound != match.RoundId))
            { ready = false; previousMatch = match; lastRound = match.RoundId; }
            crown.gameObject.SetActive(active);
            var rig = FoundationRig.Instance;
            bool vr = active && rig != null && rig.IsTracked;
            foreach (var renderer in panelRenderers) renderer.enabled = vr;
            foreach (var collider in buttonColliders) collider.enabled = vr;
            if (!active) return;
            var state = match.Snapshot;
            var avatar = state.Carried ? MatchController.FindAvatar(state.Carrier) : null;
            crown.position = avatar != null ? avatar.Head.position + Vector3.up * .3f : state.CrownPosition;
            label.text = Status(match);
            if (!vr) return;
            panel.position = rig.transform.TransformPoint(new Vector3(-1.75f, 2, 2.5f));
            panel.rotation = rig.transform.rotation;
            if (rig.PickupPressed && match.Phase == MatchPhase.Playing) match.RequestPickup();
            if (!rig.TriggerPressed) return;
            var hand = rig.RightHand;
            if (Physics.Raycast(hand.position, hand.forward, out var hit, 8) && buttons.TryGetValue(hit.collider, out var action)) action();

        }
        string Status(MatchController match)
        {
            var text = new StringBuilder("THE CURSED CROWN\n");
            text.AppendLine(match.Phase + " | " + Math.Ceiling(match.RemainingSeconds) + "s");
            text.AppendLine(match.Snapshot.Carried ? "Carrier: " + match.Snapshot.Carrier : "Crown available");
            foreach (var row in match.Scores)
                text.AppendLine("P" + row.Id + ": " + row.Score + (row.Connected ? row.Ready ? " ready" : "" : " left"));
            text.AppendLine(match.ResultsText());
            text.AppendLine("Pickup: right grip button, within 1m");
            if (match.AllowMockAlive || match.AllowDesktopPickup) text.AppendLine("SANDBOX: simulated health / tracking");
            return text.ToString();
        }
        void OnGUI()
        {
            var match = MatchController.Instance;
            if (Application.isBatchMode || match == null || !match.IsSpawned || (FoundationRig.Instance != null && FoundationRig.Instance.IsTracked)) return;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(1, Screen.height / 800f));
            GUILayout.BeginArea(new Rect(400, 18, 320, 420), GUI.skin.box);
            GUILayout.Label(Status(match));
            if (match.Phase == MatchPhase.Lobby)
            {
                if (GUILayout.Button(ready ? "Not ready" : "Ready")) { ready = !ready; match.SetReady(ready); }
                if (match.IsServer && GUILayout.Button("Start when everyone is ready")) match.TryStartMatch();
            }
            if (match.Phase == MatchPhase.Playing && GUILayout.Button("Take crown (hand within 1 metre)")) match.RequestPickup();
            if (match.Phase == MatchPhase.Finished && match.IsServer && GUILayout.Button("Restart → Lobby")) match.TryReturnToLobby();
            GUILayout.EndArea(); GUI.matrix = old;
        }
        void OnDestroy() { foreach (var material in ownedMaterials) Destroy(material); }
    }
}
