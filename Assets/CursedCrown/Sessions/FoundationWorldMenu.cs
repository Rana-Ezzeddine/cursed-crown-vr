using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    // Simple world-space controller menu; no desktop keyboard is required for Relay joining.
    public sealed class FoundationWorldMenu : MonoBehaviour
    {
        public FoundationSession Session;
        public Material MenuMaterial;
        TextMesh status,entry;
        string relayCode="";
        readonly Dictionary<Collider,Action> actions=new();
        readonly List<Material> materials=new();
        LineRenderer pointer;
        Renderer[] panelRenderers;
        Collider[] panelColliders;
        void Start()
        {
            status=Label("Status",new Vector3(0,.78f,0),.035f);
            entry=Label("Code",new Vector3(0,.5f,0),.05f);
            Tile("HOST RELAY",new Vector3(-.62f,.2f,0),()=>Session.StartRelay(true),new Vector3(.58f,.15f,.04f));
            Tile("JOIN CODE",new Vector3(0,.2f,0),()=>Session.StartRelay(false,relayCode),new Vector3(.58f,.15f,.04f));
            Tile("LEAVE",new Vector3(.62f,.2f,0),()=>Session.Leave(),new Vector3(.58f,.15f,.04f));
            const string keys="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for(int i=0;i<keys.Length;i++)
            {
                string key=keys[i].ToString();
                Tile(key,new Vector3((i%9-4)*.205f,-.06f-(i/9)*.18f,0),()=>{if(relayCode.Length<12)relayCode+=key;},new Vector3(.18f,.15f,.04f));
            }
            Tile("DELETE",new Vector3(-.45f,-.83f,0),()=>{if(relayCode.Length>0)relayCode=relayCode[..^1];},new Vector3(.8f,.15f,.04f));
            Tile("HOST LAN",new Vector3(.45f,-.83f,0),()=>Session.StartLan(true),new Vector3(.8f,.15f,.04f));
            panelRenderers=GetComponentsInChildren<Renderer>();
            panelColliders=GetComponentsInChildren<Collider>();
            pointer=new GameObject("Controller Menu Ray").AddComponent<LineRenderer>();
            pointer.startWidth=.004f;pointer.endWidth=.002f;pointer.positionCount=2;
            var rayMaterial=new Material(MenuMaterial);rayMaterial.color=Color.cyan;materials.Add(rayMaterial);pointer.sharedMaterial=rayMaterial;
        }
        void Update()
        {
            status.text="THE CURSED CROWN\n"+Wrap(Session.Status,48)+"\nPlayers "+Session.PlayerCount+" / 5";
            entry.text=string.IsNullOrEmpty(Session.JoinCode)?"JOIN CODE: "+relayCode:"HOST CODE: "+Session.JoinCode;
            var rig=FoundationRig.Instance;
            if(rig!=null)
            {
                transform.position=rig.transform.TransformPoint(new Vector3(0,1.6f,2.5f));
                transform.rotation=rig.transform.rotation;
            }
            bool visible=rig!=null&&rig.IsTracked;
            foreach(var renderer in panelRenderers)renderer.enabled=visible;
            foreach(var collider in panelColliders)collider.enabled=visible;
            pointer.enabled=visible;
            if(!pointer.enabled)return;
            var hand=rig.RightHand;
            var ray=new Ray(hand.position,hand.forward);
            Vector3 end=ray.GetPoint(8);
            if(Physics.Raycast(ray,out var hit,8)&&actions.TryGetValue(hit.collider,out var action))
            {end=hit.point;if(rig.TriggerPressed)action();}
            pointer.SetPosition(0,ray.origin);pointer.SetPosition(1,end);
        }
        static string Wrap(string value,int length)
        {if(value.Length<=length)return value;int split=value.LastIndexOf(' ',Mathf.Min(length,value.Length-1));if(split<1)split=length;return value[..split]+"\n"+Wrap(value[(split+1)..],length);}
        TextMesh Label(string name,Vector3 position,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=position+Vector3.back*.035f;
            var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.GetComponent<Renderer>().sharedMaterial=text.font.material;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=size*.12f;text.color=Color.white;return text;
        }
        void Tile(string text,Vector3 position,Action action,Vector3 scale)
        {
            var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);tile.name=text;tile.transform.SetParent(transform,false);tile.transform.localPosition=position;tile.transform.localScale=scale;
            var material=new Material(MenuMaterial);material.color=new Color(.07f,.18f,.23f);materials.Add(material);tile.GetComponent<Renderer>().sharedMaterial=material;
            actions.Add(tile.GetComponent<Collider>(),action);Label(text,position,.025f).text=text;
        }
        void OnDestroy(){if(pointer!=null)Destroy(pointer.gameObject);foreach(var material in materials)Destroy(material);}
    }
}
