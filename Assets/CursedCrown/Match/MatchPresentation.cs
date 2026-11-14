using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace CursedCrown
{
    // Scene-local presentation; authoritative state remains on MatchController.
    [DefaultExecutionOrder(100)]
    public sealed class MatchPresentation : MonoBehaviour
    {
        public Material CrownMaterial, PanelMaterial;
        Transform crown, panel;
        TempleCharacter characterPreview;
        Transform keyboard;
        TextMesh nameEntry;
        bool editingName;
        string draftName="";
        readonly System.Collections.Generic.HashSet<Collider> nameKeys=new();
        TextMesh label;
        Material textMaterial;
        readonly Dictionary<Collider, Action> buttons = new();
        readonly List<Material> ownedMaterials = new();
        readonly List<Collider> buttonColliders = new();
        Renderer[] panelRenderers;
        sealed class ButtonVisual
        {
            public Collider Collider; public Material Material; public TextMesh Text;
            public Vector3 Scale; public string Title; public float PressedUntil;
        }
        readonly List<ButtonVisual> visuals = new();
        TextMesh feedback;
        MatchPhase previousPhase;
        float feedbackUntil;
        bool ready;
        uint lastRound;
        MatchController previousMatch;
        void Start()
        {
            crown = new GameObject("Crown display").transform;
            crown.SetParent(transform, false);
            var art = gameObject.AddComponent<ArenaArt>();
            art.Build(CrownMaterial, PanelMaterial);
            art.BuildCrown(crown);
            panel = new GameObject("Match controls").transform;
            panel.SetParent(transform, false);
            var text = new GameObject("Match status");
            text.transform.SetParent(panel, false);
            label = text.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMaterial = new Material(Shader.Find("CursedCrown/MenuOverlay"));
            textMaterial.mainTexture=label.font.material.mainTexture;textMaterial.renderQueue=4003;
            textMaterial.SetFloat("_AlphaOnly",1);
            ownedMaterials.Add(textMaterial);
            Font.textureRebuilt += RefreshFontTexture;
            label.GetComponent<Renderer>().sharedMaterial = textMaterial;
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 128;
            label.characterSize = .0045f;
            label.lineSpacing=1.15f;
            AddButton("READY / NOT READY", -.72f, () => { ready = !ready; MatchController.Instance?.SetReady(ready); });
            AddButton("START (HOST)", -.94f, () => MatchController.Instance?.TryStartMatch());
            AddButton("RESTART (HOST)", -1.16f, () => MatchController.Instance?.TryReturnToLobby());
            TempleUi.Frame(panel,new Vector2(0,-.56f),new Vector2(1.48f,1.76f),ownedMaterials);
            TempleUi.Frame(panel,new Vector2(1.5f,-.56f),new Vector2(1.43f,1.76f),ownedMaterials);
            var message = new GameObject("Menu feedback"); message.transform.SetParent(panel,false);
            message.transform.localPosition = new Vector3(0,-1.36f,-.04f);
            feedback=message.AddComponent<TextMesh>();feedback.font=label.font;
            feedback.GetComponent<Renderer>().sharedMaterial=textMaterial;
            feedback.anchor=TextAnchor.MiddleCenter;feedback.alignment=TextAlignment.Center;
            feedback.fontSize=128;feedback.characterSize=.0028f;feedback.color=TempleUi.Gold;
            AddButton("CHARACTER", -.94f, () => {
                var local=MatchController.FindAvatar(MatchController.Instance.NetworkManager.LocalClientId);
                if(local!=null)local.ChooseCharacter((local.CharacterChoice.Value+1)%TempleCharacter.Names.Length);
            });
            var choiceButton=visuals[visuals.Count-1];
            choiceButton.Collider.transform.localPosition+=Vector3.right*1.5f;
            choiceButton.Text.transform.localPosition+=Vector3.right*1.5f;
            choiceButton.Text.characterSize=.0032f;
            var preview=new GameObject("Your character preview");
            characterPreview=preview.AddComponent<TempleCharacter>();characterPreview.Build();
            foreach(var renderer in characterPreview.GetComponentsInChildren<Renderer>())renderer.sharedMaterial.renderQueue=4002;
            characterPreview.Pose(Vector3.up*1.65f,Quaternion.identity,new Vector3(-.3f,.95f,.1f),Quaternion.identity,
                new Vector3(.3f,.95f,.1f),Quaternion.identity,0,false);
            preview.transform.SetParent(panel,false);preview.transform.localPosition=new Vector3(1.5f,-.75f,-.08f);
            preview.transform.localRotation=Quaternion.Euler(0,180,0);preview.transform.localScale=Vector3.one*.43f;
            AddButton("NAME",-1.16f,()=>{
                editingName=!editingName;
                if(editingName)draftName=MatchController.Instance.PlayerName(MatchController.Instance.NetworkManager.LocalClientId);
            });
            var nameButton=visuals[visuals.Count-1];
            nameButton.Collider.transform.localPosition+=Vector3.right*1.5f;
            nameButton.Text.transform.localPosition+=Vector3.right*1.5f;
            nameButton.Text.characterSize=.0032f;
            BuildKeyboard();
            panelRenderers = panel.GetComponentsInChildren<Renderer>();
        }
        void AddButton(string title, float y, Action action)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name=title+" button";
            tile.transform.SetParent(panel, false);
            tile.transform.localPosition = new Vector3(0, y, 0);
            tile.transform.localScale = new Vector3(1.25f, .18f, .035f);
            var material = new Material(Shader.Find("CursedCrown/MenuOverlay"));
            material.renderQueue=4002; ownedMaterials.Add(material);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            var collider = tile.GetComponent<Collider>();
            buttons.Add(collider, action); buttonColliders.Add(collider);
            var go = new GameObject(title);
            go.transform.SetParent(panel, false);
            go.transform.localPosition = new Vector3(0, y, -.025f);
            var text = go.AddComponent<TextMesh>();
            text.font = label.font;
            text.GetComponent<Renderer>().sharedMaterial = textMaterial;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 128; text.characterSize = .0035f; text.text = title;
            visuals.Add(new ButtonVisual { Collider=collider, Material=material, Text=text,
                Title=title, Scale=tile.transform.localScale });
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
            bool showPanel = vr && match.Phase != MatchPhase.Playing;
            foreach (var renderer in panelRenderers) renderer.enabled = showPanel;
            foreach (var collider in buttonColliders) collider.enabled = showPanel;
            if(!active || match.Phase!=MatchPhase.Lobby)editingName=false;
            keyboard.gameObject.SetActive(showPanel && editingName);
            foreach(var r in characterPreview.GetComponentsInChildren<Renderer>())r.enabled=showPanel && !editingName;
            foreach(var key in nameKeys)key.enabled=showPanel && editingName;
            var characterButton=visuals.Find(v=>v.Title=="CHARACTER");
            characterButton.Collider.enabled=showPanel && !editingName;
            characterButton.Collider.GetComponent<Renderer>().enabled=showPanel && !editingName;
            characterButton.Text.GetComponent<Renderer>().enabled=showPanel && !editingName;
            if (!active) return;
            foreach(var button in visuals)
            {
                bool relevant=button.Title.StartsWith("READY")||button.Title.StartsWith("START") ? match.Phase==MatchPhase.Lobby :
                    button.Title.StartsWith("RESTART") ? match.Phase==MatchPhase.Finished : true;
                button.Collider.gameObject.SetActive(relevant);button.Text.gameObject.SetActive(relevant);
                if(!relevant)button.Collider.enabled=false;
            }
            nameEntry.text="INSCRIBE YOUR NAME\n"+(draftName.Length==0?"_":draftName)+"  |\n"+draftName.Length+" / 16";
            var state = match.Snapshot;
            var avatar = state.Carried ? MatchController.FindAvatar(state.Carrier) : null;
            crown.position = avatar != null ? avatar.Head.position + Vector3.up * .3f : state.CrownPosition;
            crown.rotation = Quaternion.Euler(0, Time.time * 22, 0);
            label.text = Status(match);
            var localAvatar=MatchController.FindAvatar(match.NetworkManager.LocalClientId);
            if(localAvatar!=null && characterPreview.Choice!=localAvatar.CharacterChoice.Value)
                characterPreview.SetChoice(localAvatar.CharacterChoice.Value);
            foreach (var row in match.Scores)
                if (row.Id == match.NetworkManager.LocalClientId) ready = row.Ready;
            if (match.Phase != previousPhase)
            {
                previousPhase = match.Phase;
                feedback.text = match.Phase == MatchPhase.Finished ? "ROUND COMPLETE" : "NEW ROUND • READY UP";
                feedbackUntil = Time.unscaledTime + 3;
            }
            if (!vr) return;
            panel.position = rig.transform.TransformPoint(new Vector3(
                0, rig.Head.localPosition.y + .45f, 1.8f));
            panel.rotation = rig.transform.rotation;
            if (rig.PickupPressed && match.Phase == MatchPhase.Playing) match.RequestPickup();
            if (!showPanel) return;
            Collider hovered = null;
            if (rig.RightTracked && TryGetButtonHit(new Ray(rig.RightHand.position, rig.RightHand.forward), out var hit)) hovered = hit.collider;
            bool allReady = match.Scores.Count > 0;
            foreach(var row in match.Scores) if(row.Connected && !row.Ready) allReady=false;
            foreach(var button in visuals)
            {
                if(nameKeys.Contains(button.Collider))continue;
                bool isName = button.Title == "NAME";
                bool isCharacter = button.Title == "CHARACTER";
                bool isReadyButton = button.Title.StartsWith("READY");
                bool isStart = button.Title.StartsWith("START");
                bool enabled = isName ? match.Phase==MatchPhase.Lobby : isCharacter ? match.Phase == MatchPhase.Lobby && localAvatar!=null : isReadyButton ? match.Phase == MatchPhase.Lobby :
                    match.IsServer && (isStart ? match.Phase == MatchPhase.Lobby && allReady : match.Phase == MatchPhase.Finished);
                button.Text.text = isName ? (editingName?"CANCEL NAME ENTRY":"NAME: "+match.PlayerName(match.NetworkManager.LocalClientId)) : isCharacter ? TempleCharacter.Names[characterPreview.Choice]+"  >" : isReadyButton ? (ready ? "READY  /  CLICK TO CANCEL" : "READY UP") :
                    isStart ? (match.IsServer ? "START MATCH" : "HOST STARTS THE MATCH") : "PLAY AGAIN";
                bool hover = hovered == button.Collider && enabled;
                var color = !enabled ? new Color(.085f,.075f,.06f) :
                    isReadyButton && ready ? TempleUi.Jade : TempleUi.Button;
                if(hover) color = new Color(.46f,.31f,.12f);
                if(Time.unscaledTime < button.PressedUntil) color = TempleUi.Gold;
                button.Material.color = color;
                button.Text.color = enabled ? TempleUi.Ivory : TempleUi.Muted;
                button.Collider.transform.localScale = Vector3.Lerp(button.Collider.transform.localScale,
                    button.Scale * (hover ? 1.035f : 1), Time.unscaledDeltaTime * 16);
                if(hover && rig.TriggerPressed)
                {
                    button.PressedUntil=Time.unscaledTime+.18f;
                    actionsFor(button.Collider);
                    feedback.text=isName ? "ENTER A NAME • THEN SAVE" : isCharacter ? "CHARACTER SELECTED" : isReadyButton ? (ready ? "YOU ARE READY" : "READINESS CANCELLED") : isStart ? "MATCH STARTED" : "NEW ROUND • READY UP";
                    feedbackUntil=Time.unscaledTime+2;
                    var controller=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                    if(controller.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) controller.SendHapticImpulse(0,.3f,.06f);
                }
            }
            foreach(var button in visuals)
            {
                if(!nameKeys.Contains(button.Collider))continue;
                bool hover=editingName && hovered==button.Collider;
                button.Material.color=hover?TempleUi.Gold:TempleUi.Button;
                button.Text.color=TempleUi.Ivory;
                if(hover && rig.TriggerPressed)actionsFor(button.Collider);
            }
            if(Time.unscaledTime > feedbackUntil) feedback.text = match.Phase == MatchPhase.Lobby ?
                (allReady ? "ALL READY • HOST CAN START" : "WAITING FOR PLAYERS TO READY UP") : "READY FOR ANOTHER ROUND?";

        }
        void actionsFor(Collider collider) { if(buttons.TryGetValue(collider,out var action)) action(); }

        public bool IsControlCollider(Collider collider) => buttons.ContainsKey(collider);

        // UI selection must not be swallowed by an avatar, weapon, or arena collider.
        public bool TryGetButtonHit(Ray ray, out RaycastHit hit)
        {
            Physics.SyncTransforms();
            hit = default;
            float nearest = 8;
            bool found = false;
            foreach (var collider in buttonColliders)
                if (collider.enabled && collider.Raycast(ray, out var candidate, nearest))
                { hit = candidate; nearest = candidate.distance; found = true; }
            return found;
        }

        internal string Status(MatchController match)
        {
            bool finished=match.Phase==MatchPhase.Finished;
            var text=new StringBuilder("<color=#E8BA65>THE CURSED CROWN</color>\n");
            text.AppendLine(finished?"HALL OF CHAMPIONS":"THE SANCTUM  /  LOBBY");
            text.AppendLine("<color=#A59474>————————————</color>");
            var roster=new List<MatchController.PlayerRow>();foreach(var row in match.Scores)roster.Add(row);
            var scores=roster.OrderByDescending(r=>finished?r.Score:0).ThenBy(r=>r.Id).ToArray();
            int lastScore=-1,rank=0;
            for(int i=0;i<scores.Length;i++)
            {
                var row=scores[i];if(row.Score!=lastScore){rank=i+1;lastScore=row.Score;}
                string name=match.PlayerLabel(row.Id);
                string line=finished ? rank+".  "+name+"   "+row.Score+" pts" : name+"   "+(!row.Connected?"LEFT":row.Ready?"READY":"WAITING");
                if(finished && rank==1)line="<color=#E8BA65>"+line+"</color>";
                text.AppendLine(line);
            }
            if(finished && scores.Length>0)text.AppendLine("<color=#E8BA65>"+(scores.Count(r=>r.Score==scores[0].Score)>1?"SHARED VICTORY":"CROWN CHAMPION")+"</color>");
            return text.ToString();
        }
        void BuildKeyboard()
        {
            keyboard=new GameObject("Name keyboard").transform;keyboard.SetParent(panel,false);
            var title=new GameObject("Name entry");title.transform.SetParent(keyboard,false);title.transform.localPosition=new Vector3(1.5f,.07f,-.09f);
            nameEntry=title.AddComponent<TextMesh>();nameEntry.font=label.font;nameEntry.fontSize=128;nameEntry.characterSize=.0035f;
            nameEntry.anchor=TextAnchor.UpperCenter;nameEntry.alignment=TextAlignment.Center;nameEntry.color=TempleUi.Ivory;
            nameEntry.GetComponent<Renderer>().sharedMaterial=textMaterial;
            const string keys="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for(int i=0;i<keys.Length;i++)
            {
                string key=keys[i].ToString();
                AddNameKey(key,new Vector2(1.5f+(i%6-2.5f)*.20f,-.25f-(i/6)*.10f),.18f,()=>{if(draftName.Length<16)draftName+=key;});
            }
            AddNameKey("SPACE",new Vector2(1.10f,-.86f),.36f,()=>{if(draftName.Length<16)draftName+=" ";});
            AddNameKey("DELETE",new Vector2(1.50f,-.86f),.36f,()=>{if(draftName.Length>0)draftName=draftName[..^1];});
            AddNameKey("CLEAR",new Vector2(1.90f,-.86f),.36f,()=>draftName="");
            AddNameKey("SAVE NAME",new Vector2(1.5f,-1.02f),1.15f,SaveName);
        }
        void SaveName()
        {
            string clean=MatchController.CleanName(draftName);
            if(clean.Length==0){feedback.text="PLEASE ENTER A NAME";feedbackUntil=Time.unscaledTime+2;return;}
            MatchController.Instance.SetPlayerName(clean);editingName=false;
            feedback.text="NAME SUBMITTED";feedbackUntil=Time.unscaledTime+2;
        }
        void AddNameKey(string title,Vector2 position,float width,Action action)
        {
            AddButton("KEY "+title,position.y,action);var button=visuals[visuals.Count-1];nameKeys.Add(button.Collider);
            button.Collider.transform.SetParent(keyboard,false);button.Text.transform.SetParent(keyboard,false);
            button.Collider.transform.localPosition=new Vector3(position.x,position.y,-.04f);
            button.Collider.transform.localScale=new Vector3(width,.085f,.025f);button.Scale=button.Collider.transform.localScale;
            button.Text.transform.localPosition=new Vector3(position.x,position.y,-.065f);button.Text.text=title;button.Text.characterSize=.0027f;
        }
        void OnGUI()
        {
            var match = MatchController.Instance;
            if (Application.isBatchMode || match == null || !match.IsSpawned || (FoundationRig.Instance != null && FoundationRig.Instance.IsTracked)) return;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(1, Screen.height / 800f));
            GUILayout.BeginArea(new Rect(400, 18, 360, 520), GUI.skin.box);
            GUILayout.Label(Status(match));
            if (match.Phase == MatchPhase.Lobby)
            {
                draftName=GUILayout.TextField(draftName,16);
                if(GUILayout.Button("Save player name"))SaveName();
                var local=MatchController.FindAvatar(match.NetworkManager.LocalClientId);
                if(local!=null && GUILayout.Button("Character: "+TempleCharacter.Names[local.CharacterChoice.Value]+" >"))
                    local.ChooseCharacter((local.CharacterChoice.Value+1)%TempleCharacter.Names.Length);
                if (GUILayout.Button(ready ? "Not ready" : "Ready")) { ready = !ready; match.SetReady(ready); }
                if (match.IsServer && GUILayout.Button("Start when everyone is ready")) match.TryStartMatch();
            }
            if (match.Phase == MatchPhase.Playing && GUILayout.Button("Take crown (hand within 1 metre)")) match.RequestPickup();
            if (match.Phase == MatchPhase.Finished && match.IsServer && GUILayout.Button("Restart → Lobby")) match.TryReturnToLobby();
            GUILayout.EndArea(); GUI.matrix = old;
        }
        void RefreshFontTexture(Font font)
        { if(label!=null && font==label.font && textMaterial!=null)textMaterial.mainTexture=font.material.mainTexture; }
        void OnDestroy() { Font.textureRebuilt -= RefreshFontTexture; foreach (var material in ownedMaterials) Destroy(material); }
    }
}
