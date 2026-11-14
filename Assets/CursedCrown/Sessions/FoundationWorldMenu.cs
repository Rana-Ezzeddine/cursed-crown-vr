using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    // Runtime-created controls work in existing Arena and sandbox scenes without rewiring.
    [DefaultExecutionOrder(200)]
    public sealed class FoundationWorldMenu : MonoBehaviour
    {
        public FoundationSession Session;
        public Material MenuMaterial;
        TextMesh status, entry;
        Material textMaterial;
        string relayCode = "", lanAddress = "";
        bool editingAddress, connectedMenu, wasConnected;
        sealed class Button
        {
            public Action Invoke;
            public Func<bool> Enabled;
            public Material Material;
            public TextMesh Text; public float PressedUntil;
        }
        readonly Dictionary<Collider, Button> actions = new();
        readonly List<Material> materials = new();
        LineRenderer pointer;
        Renderer[] panelRenderers;
        Collider[] panelColliders;

        void Start()
        {
            textMaterial=TempleUi.Material(materials,TempleUi.Ivory,4003);
            textMaterial.SetFloat("_AlphaOnly",1);
            textMaterial.mainTexture=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;
            Font.textureRebuilt+=RefreshFont;
            TempleUi.Frame(transform,new Vector2(0,.12f),new Vector2(2.12f,2.5f),materials);
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
            panelColliders = new List<Collider>(actions.Keys).ToArray();
            pointer = new GameObject("Controller Menu Ray").AddComponent<LineRenderer>();
            pointer.startWidth = .004f; pointer.endWidth = .002f; pointer.positionCount = 2;
            var rayMaterial = new Material(MenuMaterial) { color = TempleUi.Gold };
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
            // Keep the shared aiming ray available for the match's Ready/Start controls.
            // Tracking loss must hide it rather than leaving a stale aim visible.
            pointer.enabled = rig != null && rig.IsTracked && rig.RightTracked;
            if (pointer.enabled)
            {
                var aim = new Ray(rig.RightHand.position, rig.RightHand.forward);
                var matchMenu = FindFirstObjectByType<MatchPresentation>();
                var endPoint = !visible && matchMenu != null && matchMenu.TryGetButtonHit(aim, out var menuHit)
                    ? menuHit.point : Physics.Raycast(aim, out var aimHit, 8) ? aimHit.point : aim.GetPoint(8);
                pointer.SetPosition(0, aim.origin);
                pointer.SetPosition(1, endPoint);
            }
            if (!visible) return;
            status.text = "THE CURSED CROWN\n" + Wrap(Session.Status, 52) +
                "\nPlayers " + Session.PlayerCount + " / " + GameRules.MaxPlayers;
            entry.text = !string.IsNullOrEmpty(Session.JoinCode) ? "HOST CODE: " + Session.JoinCode :
                (editingAddress ? "HOST IPv4: " + lanAddress : "JOIN CODE: " + relayCode);
            foreach (var button in actions.Values)
            {
                button.Material.color = Time.unscaledTime<button.PressedUntil?TempleUi.Gold:button.Enabled()?TempleUi.Button:new Color(.08f,.07f,.06f);
                button.Text.color=button.Enabled()?TempleUi.Ivory:TempleUi.Muted;
            }
            var hand = rig.RightHand;
            var ray = new Ray(hand.position, hand.forward);
            Vector3 end = ray.GetPoint(8);
            if (TryHit(ray, out var hit) && actions.TryGetValue(hit.collider, out var action))
            {
                end = hit.point;
                if(action.Enabled())action.Material.color=TempleUi.Gold;
                if (rig.RightTracked && rig.TriggerPressed && action.Enabled())
                {
                    action.PressedUntil=Time.unscaledTime+.18f;action.Invoke();
                    var controller=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                    if(controller.TryGetHapticCapabilities(out var caps)&&caps.supportsImpulse)controller.SendHapticImpulse(0,.25f,.05f);
                }
            }
            pointer.SetPosition(0, ray.origin); pointer.SetPosition(1, end);
        }

        bool TryHit(Ray ray,out RaycastHit hit)
        {
            Physics.SyncTransforms();hit=default;float nearest=8;bool found=false;
            foreach(var collider in actions.Keys)if(collider.enabled && collider.Raycast(ray,out var candidate,nearest))
            {hit=candidate;nearest=candidate.distance;found=true;}return found;
        }
        void RefreshFont(Font font){if(textMaterial!=null)textMaterial.mainTexture=font.material.mainTexture;}
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
            text.GetComponent<Renderer>().sharedMaterial = textMaterial;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.fontSize = 128; text.characterSize = size * .12f; text.color = TempleUi.Ivory;
            return text;
        }
        void Tile(string text, Vector3 position, Action action, Func<bool> enabled, float width)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name = text;
            tile.transform.SetParent(transform, false); tile.transform.localPosition = position;
            tile.transform.localScale = new Vector3(width, .15f, .04f);
            var material = TempleUi.Material(materials,TempleUi.Button,4002,true);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            var caption=Label(text, position, .025f);caption.text=text;
            actions.Add(tile.GetComponent<Collider>(), new Button { Invoke = action, Enabled = enabled, Material = material, Text=caption });
        }
        void OnDestroy()
        {
            Font.textureRebuilt-=RefreshFont;
            if (pointer != null) Destroy(pointer.gameObject);
            foreach (var material in materials) Destroy(material);
        }
    }
}
