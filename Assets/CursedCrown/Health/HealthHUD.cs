using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CursedCrown
{
    // Compact local HUD; the large lobby card is absent during a round.
    public sealed class HealthHUD : MonoBehaviour
    {
        public Text HealthText, StatusText;
        PlayerHealth player;
        Canvas hudCanvas;
        Text matchText, chargeText;
        Image background;
        Material panelMaterial, fontMaterial;
        readonly Image[] bars = new Image[4];
        int previousHealth = -1;
        float damageUntil;
        void Update()
        {
            if(player==null)player=GetComponent<PlayerHealth>();
            if(player==null || !player.IsSpawned || !player.IsOwner)return;
            if(hudCanvas==null)CreateHud();
            if(hudCanvas==null)return;
            var match=MatchController.Instance;
            bool playing=match!=null && match.IsSpawned && match.Phase==MatchPhase.Playing;
            hudCanvas.enabled=playing;
            if(!playing)return;
            if(previousHealth>=0 && player.CurrentHealth<previousHealth)damageUntil=Time.unscaledTime+.45f;
            previousHealth=player.CurrentHealth;
            bool hurt=Time.unscaledTime<damageUntil;
            background.color=hurt?new Color(.35f,.04f,.06f,.94f):new Color(.055f,.047f,.038f,.95f);
            HealthText.text=player.IsAlive?"VITALITY   "+player.CurrentHealth+" / "+GameRules.MaxHealth:"YOU FELL";
            float health=Mathf.Clamp01((float)player.CurrentHealth/GameRules.MaxHealth);
            for(int i=0;i<bars.Length;i++)bars[i].color=health>(i*.25f)?
                (health<=.25f?new Color(1,.3f,.25f):new Color(.83f,.62f,.29f)):new Color(.18f,.15f,.11f);
            int seconds=Mathf.CeilToInt((float)match.RemainingSeconds);
            matchText.text=$"{seconds/60:00}:{seconds%60:00}   •   "+(match.Snapshot.Carried?
                (match.Snapshot.Carrier==player.OwnerClientId?"YOU HOLD THE CROWN":"CROWN: "+match.PlayerLabel(match.Snapshot.Carrier)):"CROWN AT THE SANCTUM");
            var gun=player.GetComponent<NetworkGun>();
            chargeText.text=gun==null?"":gun.Recharging.Value?"RECHARGING…":gun.Charge.Value==0?"EMPTY • PRESS Y TO RECHARGE":"LASER  "+gun.Charge.Value+" / "+GameRules.MaxCharge+"   •   Y: RECHARGE";
            chargeText.color=gun!=null && gun.Charge.Value==0?new Color(1,.45f,.25f):TempleUi.Gold;
            int score=0;foreach(var row in match.Scores)if(row.Id==player.OwnerClientId)score=row.Score;
            if(player.IsAlive)
            {
                if(match.Snapshot.Carried)StatusText.text=score+" PTS • "+
                    (match.Snapshot.Carrier==player.OwnerClientId?"YOU HOLD THE CROWN":"CATCH THE CROWN CARRIER");
                else
                {
                    var hand=FoundationRig.Instance?.RightHand;
                    float distance=hand!=null?Vector3.Distance(hand.position,match.Snapshot.CrownPosition):float.PositiveInfinity;
                    StatusText.text=score+" PTS • "+(distance<=GameRules.CrownPickupDistance?
                        "RAISE HAND • GRIP TO CLAIM":"CROWN "+distance.ToString("F1")+"m • GET CLOSER");
                }
            }
            else
            {
                double remaining=player.RespawnAtServerTime.Value-NetworkManager.Singleton.ServerTime.Time;
                StatusText.text=remaining>0?"RETURNING IN "+remaining.ToString("F1")+"s":"FINDING A SAFE SPAWN";
            }
        }
        void CreateHud()
        {
            var rig=FoundationRig.Instance;if(rig==null || rig.Head==null)return;
            var go=new GameObject("Player Health HUD",typeof(RectTransform),typeof(Canvas));
            hudCanvas=go.GetComponent<Canvas>();hudCanvas.renderMode=RenderMode.WorldSpace;hudCanvas.sortingOrder=100;
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(480,190);
            go.transform.SetParent(rig.Head,false);go.transform.localPosition=new Vector3(0,-.32f,.95f);
            go.transform.localScale=Vector3.one*.0009f;
            background=Box("Obsidian HUD",rect,new Vector2(0,0),new Vector2(480,190),new Color(.055f,.047f,.038f,.95f));
            Box("Gold rule",rect,new Vector2(0,92),new Vector2(480,3),new Color(.9f,.65f,.25f));
            matchText=Label("Round and crown",rect,new Vector2(0,48),new Vector2(450,30),24,new Color(.95f,.77f,.43f));
            HealthText=Label("Vitality",rect,new Vector2(0,15),new Vector2(450,32),28,Color.white);
            for(int i=0;i<4;i++)bars[i]=Box("Vitality segment",rect,new Vector2(-168+i*112,-15),new Vector2(104,9),Color.white);
            StatusText=Label("Player state",rect,new Vector2(0,-48),new Vector2(450,28),21,TempleUi.Ivory);
            chargeText=Label("Weapon charge",rect,new Vector2(0,-78),new Vector2(450,25),21,TempleUi.Gold);
            Box("Lower gold rule",rect,new Vector2(0,-93),new Vector2(480,2),TempleUi.Gold);
            Box("Left gold rule",rect,new Vector2(-239,0),new Vector2(2,190),TempleUi.Gold);
            Box("Right gold rule",rect,new Vector2(239,0),new Vector2(2,190),TempleUi.Gold);
            panelMaterial=new Material(Shader.Find("CursedCrown/MenuOverlay"));panelMaterial.renderQueue=4100;
            fontMaterial=new Material(panelMaterial);fontMaterial.SetFloat("_AlphaOnly",1);fontMaterial.renderQueue=4101;
            foreach(var image in go.GetComponentsInChildren<Image>())image.material=panelMaterial;
            foreach(var text in go.GetComponentsInChildren<Text>())text.material=fontMaterial;
        }
        static Image Box(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static Text Label(string name,Transform parent,Vector2 position,Vector2 size,int fontSize,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            return text;
        }
        void OnDestroy(){if(hudCanvas!=null)Destroy(hudCanvas.gameObject);if(panelMaterial!=null)Destroy(panelMaterial);if(fontMaterial!=null)Destroy(fontMaterial);}
    }
}
