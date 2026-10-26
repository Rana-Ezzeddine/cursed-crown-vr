using UnityEngine;

namespace CursedCrown
{
    public sealed class FoundationMenu : MonoBehaviour
    {
        public FoundationSession Session;
        string code = "", address = "127.0.0.1";
        GUIStyle title, body;
        Vector2 scroll;

        void OnGUI()
        {
            if (Session == null || Application.isBatchMode ||
                (FoundationRig.Instance != null && FoundationRig.Instance.HasHeadset)) return;
            float scale = Mathf.Clamp(Screen.height / 850f, .7f, 1.4f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            body ??= new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            GUILayout.BeginArea(new Rect(18, 18, Mathf.Min(410, Screen.width / scale - 36),
                Screen.height / scale - 36), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("THE CURSED CROWN", title);
            GUILayout.Label(Session.Status, body);
            GUILayout.Label("Players: " + Session.PlayerCount + " / " + GameRules.MaxPlayers);
            if (!string.IsNullOrEmpty(Session.JoinCode)) GUILayout.Label("Relay code: " + Session.JoinCode, title);

            if (!Session.CanStart)
            {
                GUI.enabled = Session.Phase != SessionPhase.Leaving;
                if (GUILayout.Button(Session.Phase == SessionPhase.Connected ? "Leave session" : "Cancel connection",
                    GUILayout.Height(38))) Session.Leave();
                GUI.enabled = true;
                if (Session.Manager.IsHost)
                    GUILayout.Label("Leaving as host ends the session for everyone.", body);
            }
            else
            {
                GUILayout.Space(8);
                GUILayout.Label("LAN: connect on the same network", title);
                GUILayout.Label("Host's IPv4 address", body);
                address = GUILayout.TextField(address, 64);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host LAN", GUILayout.Height(38))) Session.StartLan(true);
                if (GUILayout.Button("Join LAN", GUILayout.Height(38))) { Session.Address = address; Session.StartLan(false); }
                GUILayout.EndHorizontal();
                GUILayout.Label("127.0.0.1 joins another game on this computer. For another device, enter the host's LAN IPv4 address from its network settings.", body);
                GUILayout.Space(8);
                GUILayout.Label("Relay: connect using a code", title);
                code = GUILayout.TextField(code, SessionInput.MaxRelayCodeLength).ToUpperInvariant();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host Relay", GUILayout.Height(38))) Session.StartRelay(true);
                if (GUILayout.Button("Join code", GUILayout.Height(38))) Session.StartRelay(false, code);
                GUILayout.EndHorizontal();
                GUILayout.Label("Relay needs internet and the team's linked Unity services project.", body);
            }
            GUILayout.Space(10);
            GUILayout.Label("Desktop preview: hold right mouse + WASD to move the simulated head; Q/E turns it.", body);
            GUILayout.Label("On Quest: aim the right controller and press its trigger. Press B to open/close the lobby while connected.", body);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
