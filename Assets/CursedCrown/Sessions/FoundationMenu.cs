using UnityEngine;

namespace CursedCrown
{
    public sealed class FoundationMenu : MonoBehaviour
    {
        public FoundationSession Session;
        string code="",address="127.0.0.1";
        GUIStyle title,body;
        void OnGUI()
        {
            if(Application.isBatchMode || (FoundationRig.Instance!=null && FoundationRig.Instance.IsTracked))return;
            float scale=Mathf.Max(1,Screen.height/800f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            title ??= new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold};
            body ??= new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            GUILayout.BeginArea(new Rect(18,18,365,440),GUI.skin.box);
            GUILayout.Label("THE CURSED CROWN",title);
            GUILayout.Label("FOUNDATION SANDBOX",body);
            GUILayout.Space(8);
            GUILayout.Label(Session.Status,body);
            GUILayout.Label("Players: "+Session.PlayerCount+" / "+GameRules.MaxPlayers);
            if(!string.IsNullOrEmpty(Session.JoinCode))GUILayout.Label("Relay code: "+Session.JoinCode,title);
            if(Session.Manager.IsListening || Session.Busy || Session.Manager.ShutdownInProgress)
            {
                if(GUILayout.Button("Leave / cancel",GUILayout.Height(38)))Session.Leave();
            }
            else
            {
                GUILayout.Label("LAN host address (same Wi-Fi)");
                address=GUILayout.TextField(address,64);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Host LAN",GUILayout.Height(36))){Session.Address=address;Session.StartLan(true);}
                if(GUILayout.Button("Join LAN",GUILayout.Height(36))){Session.Address=address;Session.StartLan(false);}
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
                GUILayout.Label("Relay (requires linked Unity services project)");
                code=GUILayout.TextField(code,12).ToUpperInvariant();
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Host Relay",GUILayout.Height(36)))Session.StartRelay(true);
                if(GUILayout.Button("Join code",GUILayout.Height(36)))Session.StartRelay(false,code);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            GUILayout.Label("Desktop simulation: hold right mouse + WASD to offset head, Q/E to turn. No gameplay movement is implemented.",body);
            GUILayout.Label("VR: aim right controller at lobby tiles and press trigger. Keep Quest boundaries enabled.",body);
            GUILayout.EndArea();
        }
    }
}
