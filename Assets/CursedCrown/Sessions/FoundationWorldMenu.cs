using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    // Runtime-created controls work in existing Arena and sandbox scenes without rewiring.
    public sealed class FoundationWorldMenu : MonoBehaviour
    {
        public FoundationSession Session;
        public Material MenuMaterial;
        TextMesh status, entry;
        string relayCode = "", lanAddress = "";
        bool editingAddress, connectedMenu, wasConnected;
        sealed class Button
        {
            public Action Invoke;
            public Func<bool> Enabled;
            public Material Material;
        }
        readonly Dictionary<Collider, Button> actions = new();
        readonly List<Material> materials = new();
        LineRenderer pointer;
        Renderer[] panelRenderers;
        Collider[] panelColliders;

        void Start()
        {
            status = Label("Status", new Vector3(0, 1.12f, 0), .035f);
            entry = Label("Entry", new Vector3(0, .74f, 0), .04f);
            Tile("HOST LAN", new Vector3(-.72f, .5f, 0), () => Session.StartLan(true), () => Session.CanStart, .45f);
            Tile("JOIN LAN", new Vector3(-.24f, .5f, 0), () => { Session.Address = lanAddress; Session.StartLan(false); }, () => Session.CanStart, .45f);
            Tile("HOST RELAY", new Vector3(.24f, .5f, 0), () => Session.StartRelay(true), () => Session.CanStart, .45f);
            Tile("JOIN CODE", new Vector3(.72f, .5f, 0), () => Session.StartRelay(false, relayCode), () => Session.CanStart, .45f);
            Tile("EDIT IP", new Vector3(-.62f, .27f, 0), () => editingAddress = true, () => Session.CanStart, .58f);
            Tile("EDIT CODE", new Vector3(0, .27f, 0), () => editingAddress = false, () => Session.CanStart, .58f);
            Tile("LEAVE/CANCEL", new Vector3(.62f, .27f, 0), () => Session.Leave(),
                () => !Session.CanStart && Session.Phase != SessionPhase.Leaving, .58f);
            const string keys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i].ToString();
                bool digit = key[0] >= '0' && key[0] <= '9';
                Tile(key, new Vector3((i % 9 - 4) * .205f, .02f - (i / 9) * .18f, 0),
                    () => Append(key), () => Session.CanStart && (!editingAddress || digit), .18f);
            }
            Tile(".", new Vector3(-.72f, -.78f, 0), () => Append("."), () => Session.CanStart && editingAddress, .3f);
            Tile("DELETE", new Vector3(-.23f, -.78f, 0), Delete, () => Session.CanStart, .62f);
            Tile("CLEAR", new Vector3(.48f, -.78f, 0), () => { if (editingAddress) lanAddress = ""; else relayCode = ""; },
                () => Session.CanStart, .68f);
            Label("Help", new Vector3(0, -1f, 0), .025f).text = "RIGHT TRIGGER: SELECT   |   B: HIDE/SHOW WHEN CONNECTED";
            panelRenderers = GetComponentsInChildren<Renderer>();
            panelColliders = GetComponentsInChildren<Collider>();
            pointer = new GameObject("Controller Menu Ray").AddComponent<LineRenderer>();
            pointer.startWidth = .004f; pointer.endWidth = .002f; pointer.positionCount = 2;
            var rayMaterial = new Material(MenuMaterial) { color = Color.cyan };
            materials.Add(rayMaterial); pointer.sharedMaterial = rayMaterial;
        }

        void Append(string key)
        {
            if (editingAddress) { if (lanAddress.Length < 15) lanAddress += key; }
            else if (relayCode.Length < SessionInput.MaxRelayCodeLength) relayCode += key;
        }
        void Delete()
        {
            if (editingAddress) { if (lanAddress.Length > 0) lanAddress = lanAddress[..^1]; }
            else if (relayCode.Length > 0) relayCode = relayCode[..^1];
        }

        void Update()
        {
            bool connected = Session.Phase == SessionPhase.Connected;
            if (connected != wasConnected) { connectedMenu = false; wasConnected = connected; }
            var rig = FoundationRig.Instance;
            if (rig != null)
            {
                transform.position = rig.transform.TransformPoint(new Vector3(0, 1.6f, 2.5f));
                transform.rotation = rig.transform.rotation;
                if (connected && rig.MenuPressed) connectedMenu = !connectedMenu;
            }
            bool visible = rig != null && rig.IsTracked && (!connected || connectedMenu);
            foreach (var renderer in panelRenderers) renderer.enabled = visible;
            foreach (var collider in panelColliders) collider.enabled = visible;
            pointer.enabled = visible;
            if (!visible) return;
            status.text = "THE CURSED CROWN\n" + Wrap(Session.Status, 52) +
                "\nPlayers " + Session.PlayerCount + " / " + GameRules.MaxPlayers;
            entry.text = !string.IsNullOrEmpty(Session.JoinCode) ? "HOST CODE: " + Session.JoinCode :
                (editingAddress ? "HOST IPv4: " + lanAddress : "JOIN CODE: " + relayCode);
            foreach (var button in actions.Values)
                button.Material.color = button.Enabled() ? new Color(.07f, .18f, .23f) : new Color(.07f, .07f, .08f);
            var hand = rig.RightHand;
            var ray = new Ray(hand.position, hand.forward);
            Vector3 end = ray.GetPoint(8);
            if (Physics.Raycast(ray, out var hit, 8) && actions.TryGetValue(hit.collider, out var action))
            {
                end = hit.point;
                if (rig.TriggerPressed && action.Enabled()) action.Invoke();
            }
            pointer.SetPosition(0, ray.origin); pointer.SetPosition(1, end);
        }

        static string Wrap(string value, int length)
        {
            if (value.Length <= length) return value;
            int split = value.LastIndexOf(' ', Mathf.Min(length, value.Length - 1));
            if (split < 1) return value[..length] + "\n" + Wrap(value[length..], length);
            return value[..split] + "\n" + Wrap(value[(split + 1)..], length);
        }
        TextMesh Label(string name, Vector3 position, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false); go.transform.localPosition = position + Vector3.back * .035f;
            var text = go.AddComponent<TextMesh>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<Renderer>().sharedMaterial = text.font.material;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.fontSize = 64; text.characterSize = size * .12f; text.color = Color.white;
            return text;
        }
        void Tile(string text, Vector3 position, Action action, Func<bool> enabled, float width)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name = text;
            tile.transform.SetParent(transform, false); tile.transform.localPosition = position;
            tile.transform.localScale = new Vector3(width, .15f, .04f);
            var material = new Material(MenuMaterial); materials.Add(material);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            actions.Add(tile.GetComponent<Collider>(), new Button { Invoke = action, Enabled = enabled, Material = material });
            Label(text, position, .025f).text = text;
        }
        void OnDestroy()
        {
            if (pointer != null) Destroy(pointer.gameObject);
            foreach (var material in materials) Destroy(material);
        }
    }
}
