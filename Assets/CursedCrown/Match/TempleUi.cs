using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    // Shared readable obsidian, aged gold and ivory styling for world-space UI.
    public static class TempleUi
    {
        public static readonly Color Stone=new(.055f,.047f,.038f), Gold=new(.83f,.62f,.29f), Ivory=new(.97f,.89f,.72f);
        public static readonly Color Button=new(.19f,.14f,.085f), Muted=new(.43f,.39f,.32f), Jade=new(.10f,.34f,.25f);
        static Texture2D stone;
        public static Material Material(List<Material> owned,Color color,int order=4000,bool textured=false)
        {
            var m=new Material(Shader.Find("CursedCrown/MenuOverlay")){color=color,renderQueue=order};
            if(textured)
            {
                if(stone==null)
                {
                    stone=new Texture2D(64,64,TextureFormat.RGB24,false);stone.name="Procedural menu stone";
                    for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                    {float v=.82f+.18f*Mathf.PerlinNoise(x*.23f,y*.23f);stone.SetPixel(x,y,new Color(v,v,v));}
                    stone.Apply();
                }
                m.mainTexture=stone;
            }
            owned.Add(m);return m;
        }
        public static void Frame(Transform parent,Vector2 center,Vector2 size,List<Material> owned)
        {
            var stoneMat=Material(owned,Stone,4000,true);var trim=Material(owned,Gold,4001);
            ArenaArt.Part("Carved obsidian tablet",PrimitiveType.Cube,parent,new Vector3(center.x,center.y,.065f),new Vector3(size.x,size.y,.025f),stoneMat);
            foreach(int side in new[]{-1,1})
            {
                ArenaArt.Part("Gilded edge",PrimitiveType.Cube,parent,new Vector3(center.x+side*(size.x/2-.025f),center.y,.04f),new Vector3(.009f,size.y-.05f,.01f),trim);
                ArenaArt.Part("Gilded edge",PrimitiveType.Cube,parent,new Vector3(center.x,center.y+side*(size.y/2-.025f),.04f),new Vector3(size.x-.05f,.009f,.01f),trim);
                foreach(int vertical in new[]{-1,1})
                {
                    var jewel=ArenaArt.Part("Corner seal",PrimitiveType.Cube,parent,new Vector3(center.x+side*(size.x/2-.055f),center.y+vertical*(size.y/2-.055f),.025f),new Vector3(.045f,.045f,.012f),trim);
                    jewel.localRotation=Quaternion.Euler(0,0,45);
                }
            }
        }
    }
}
