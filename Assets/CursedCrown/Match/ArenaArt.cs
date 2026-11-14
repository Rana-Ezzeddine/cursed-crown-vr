using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CursedCrown
{
    // Deterministic temple layout shared by every peer. Solid architecture participates in movement and weapon queries.
    public sealed class ArenaArt : MonoBehaviour
    {
        readonly List<Material> materials = new();
        readonly List<Mesh> meshes = new();
        public Material Ink, Stone, Gold, Jade;
        public void Build(Material lit, Material unlit)
        {
            Ink = Textured(lit,"monastery_stone_floor",new Color(.82f,.8f,.74f));
            Stone = Textured(lit,"stone_block_wall",new Color(.82f,.77f,.65f));
            var rubble = Textured(lit,"castle_wall_slates",new Color(.73f,.7f,.62f));
            Gold = Make(Resources.Load<Material>("TempleGold") ?? lit,new Color(.7f,.43f,.14f));
            Gold.SetFloat("_Metallic",.82f);Gold.SetFloat("_Smoothness",.56f);
            Gold.SetTexture("_BumpMap",Resources.Load<Texture2D>("TempleTextures/aged_metal_normal"));
            Gold.EnableKeyword("_NORMALMAP");Gold.SetFloat("_BumpScale",.15f);
            Jade = Make(lit,new Color(.025f,.23f,.16f));Jade.SetFloat("_Smoothness",.88f);Jade.SetFloat("_Metallic",.25f);
            var floor=GameObject.Find("Floor");if(floor==null)return;
            Surface(floor,Ink);
            foreach(var name in new[]{"North Wall","South Wall","East Wall","West Wall"})
            {
                var wall=GameObject.Find(name);if(wall!=null)Surface(wall,Stone);
            }
            for(int i=1;i<=5;i++)
            {
                var cover=GameObject.Find("Cover "+i);if(cover==null)continue;
                Surface(cover,rubble);
                // Chipped capstones make the old cover read as broken masonry.
                var top=cover.transform.position+Vector3.up*.78f;
                for(int j=0;j<3;j++)
                    Block("Broken cover capstone",top+cover.transform.right*(j-1)*.59f,new Vector3(.56f,.16f,.98f),rubble,false,cover.transform.eulerAngles.y+(j-1)*3);
            }
            var pedestal=GameObject.Find("Pedestal");if(pedestal!=null)Surface(pedestal,rubble);
            Block("Altar carved crown slab",new Vector3(0,1.06f,0),new Vector3(1.45f,.12f,1.45f),Stone,false);
            Ring("Altar bronze inlay",transform,1.9f,.018f,.015f,Gold);
            // Four chambers: front and side doors give every hiding place an escape route.
            for(int side=0;side<4;side++)
            {
                var room=new GameObject("Temple chamber "+(side+1)).transform;
                room.SetParent(transform,false);room.localRotation=Quaternion.Euler(0,side*90,0);
                Chamber(room,Stone,rubble);
            }
            // Tall colonnade framing the open-air sanctum.
            for(int x=-1;x<=1;x+=2)
                for(int z=-1;z<=1;z+=2)
                    Column(transform,new Vector3(x*3.3f,0,z*3.3f),4.4f,Stone,rubble);
            Block("Sanctum lintel",new Vector3(0,4.65f,3.3f),new Vector3(7.5f,.5f,1.1f),Stone,true);
            Block("Broken lintel",new Vector3(-2.5f,4.65f,-3.3f),new Vector3(2.4f,.5f,1.1f),rubble,true);
            // Perimeter buttresses and irregular parapet silhouette.
            var random=new System.Random(298);
            for(int side=0;side<4;side++)
            {
                var edge=new GameObject("Temple enclosure").transform;edge.SetParent(transform,false);
                edge.localRotation=Quaternion.Euler(0,side*90,0);
                for(int x=-12;x<=12;x+=4)
                {
                    Column(edge,new Vector3(x,0,14.5f),4.25f,Stone,rubble);
                    for(int j=0;j<3;j++)
                        Block("Weathered parapet stone",edge.TransformPoint(new Vector3(x+j-1,4.15f,14.9f)),
                            new Vector3(.9f,.3f+(float)random.NextDouble()*.3f,.5f),rubble,false,side*90);
                }
                for(int i=0;i<8;i++)
                {
                    var position=edge.TransformPoint(new Vector3(-12+i*3.4f,.12f,13.9f));
                    Block("Collapsed stone rubble",position,new Vector3(.3f+(float)random.NextDouble()*.5f,.25f,.45f),rubble,true,(float)random.NextDouble()*180);
                }
            }
            for(int i=1;i<=5;i++)
            {
                var marker=GameObject.Find("Spawn Marker "+i);if(marker!=null)marker.GetComponent<Renderer>().enabled=false;
            }
            var sun=GameObject.Find("Directional Light")?.GetComponent<Light>();
            if(sun!=null){sun.transform.rotation=Quaternion.Euler(34,-28,0);sun.color=new Color(1,.86f,.65f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.43f,.48f,.56f);
            RenderSettings.ambientEquatorColor=new Color(.34f,.32f,.28f);
            RenderSettings.ambientGroundColor=new Color(.19f,.17f,.14f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=22;RenderSettings.fogEndDistance=65;
            RenderSettings.fogColor=new Color(.53f,.55f,.56f);
            if(Camera.main!=null){Camera.main.clearFlags=CameraClearFlags.Skybox;Camera.main.backgroundColor=RenderSettings.fogColor;}
            BatchDecorations();
            Physics.SyncTransforms();
        }
        Material Textured(Material source,string name,Color color)
        {
            var material=Make(Resources.Load<Material>("TempleStone") ?? source,color);
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("TempleTextures/"+name+"_albedo"));
            material.SetTexture("_BumpMap",Resources.Load<Texture2D>("TempleTextures/"+name+"_normal"));
            material.SetTexture("_OcclusionMap",Resources.Load<Texture2D>("TempleTextures/"+name+"_ao"));
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCCLUSIONMAP");
            material.SetFloat("_BumpScale",.85f);material.SetFloat("_Smoothness",.15f);
            return material;
        }
        void Surface(GameObject go,Material material)
        {
            go.GetComponent<Renderer>().sharedMaterial=material;
            WorldUV(go.GetComponent<MeshFilter>());
        }
        void WorldUV(MeshFilter filter)
        {
            var mesh=Instantiate(filter.sharedMesh);mesh.name="Metre-scaled stone UVs";
            var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
            for(int i=0;i<uv.Length;i++)
            {
                var p=Vector3.Scale(vertices[i],filter.transform.lossyScale);
                var n=normals[i];
                uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z)*.5f:
                    Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f;
            }
            mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=mesh;meshes.Add(mesh);
        }
        Transform Block(string name,Vector3 position,Vector3 scale,Material material,bool solid,float yaw=0)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);
            go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));go.transform.localScale=scale;
            go.GetComponent<Collider>().enabled=solid;
            Surface(go,material);return go.transform;
        }
        Transform LocalBlock(Transform room,string name,Vector3 position,Vector3 scale,Material material,bool solid=true)
        {return Block(name,room.TransformPoint(position),scale,material,solid,room.eulerAngles.y);}
        void Chamber(Transform room,Material stone,Material rubble)
        {
            // Interior x/z 7..13. Open doorway at (10,7), second exit at (7,10).
            LocalBlock(room,"Chamber back wall",new Vector3(10,1.8f,13),new Vector3(6.7f,3.6f,.6f),stone);
            LocalBlock(room,"Chamber outer wall",new Vector3(13,1.8f,10),new Vector3(.6f,3.6f,6.7f),stone);
            LocalBlock(room,"Chamber front pier",new Vector3(7.95f,1.8f,7),new Vector3(1.9f,3.6f,.6f),stone);
            LocalBlock(room,"Chamber front pier",new Vector3(12.05f,1.8f,7),new Vector3(1.9f,3.6f,.6f),stone);
            LocalBlock(room,"Chamber side pier",new Vector3(7,1.8f,7.95f),new Vector3(.6f,3.6f,1.9f),stone);
            LocalBlock(room,"Chamber side pier",new Vector3(7,1.8f,12.05f),new Vector3(.6f,3.6f,1.9f),stone);
            Arch(room,new Vector3(10,0,7),0,stone);
            Arch(room,new Vector3(7,0,10),90,stone);
            LocalBlock(room,"Partial chamber roof",new Vector3(11.65f,3.85f,10),new Vector3(3.5f,.45f,6.5f),rubble);
            LocalBlock(room,"Interior hiding sarcophagus",new Vector3(11.8f,.65f,11.8f),new Vector3(1.7f,1.3f,1.5f),rubble);
            LocalBlock(room,"Sarcophagus lid",new Vector3(11.8f,1.35f,11.8f),new Vector3(1.85f,.15f,1.65f),stone);
        }
        void Arch(Transform room,Vector3 position,float yaw,Material stone)
        {
            var rotation=room.rotation*Quaternion.Euler(0,yaw,0);
            var centre=room.TransformPoint(position)+Vector3.up*2.35f;
            for(int i=0;i<11;i++)
            {
                float angle=(i+.5f)*Mathf.PI/11;
                var part=Block("Arch voussoir",centre+rotation*new Vector3(Mathf.Cos(angle)*1.28f,Mathf.Sin(angle)*1.28f,0),
                    new Vector3(.39f,.36f,.8f),stone,true);
                part.rotation=rotation*Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
            }
        }
        void Column(Transform parent,Vector3 position,float height,Material stone,Material rubble)
        {
            var root=new GameObject("Weathered temple column").transform;root.SetParent(transform,false);root.position=parent.TransformPoint(position);
            Block("Column plinth",root.position+Vector3.up*.18f,new Vector3(1.15f,.36f,1.15f),rubble,true);
            for(int j=0;j<7;j++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Carved column drum";go.transform.SetParent(root,false);
                go.transform.localPosition=Vector3.up*(.4f+(j+.5f)*(height-.8f)/7);
                go.transform.localScale=new Vector3(.78f,(height-.8f)/14,.78f);
                Surface(go,stone);
            }
            Block("Column capital",root.position+Vector3.up*(height-.18f),new Vector3(1.2f,.36f,1.2f),stone,true);
        }
        void BatchDecorations()
        {
            var filters=GetComponentsInChildren<MeshFilter>();
            foreach(var material in materials)
            {
                var instances=new List<CombineInstance>();
                foreach(var filter in filters)
                {
                    if(filter.GetComponent<Renderer>().sharedMaterial!=material)continue;
                    instances.Add(new CombineInstance { mesh=filter.sharedMesh,
                        transform=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix });
                    filter.GetComponent<Renderer>().enabled=false;
                }
                if(instances.Count==0)continue;
                var mesh=new Mesh { name="Batched temple stone", indexFormat=IndexFormat.UInt32 };
                mesh.CombineMeshes(instances.ToArray());meshes.Add(mesh);
                var go=new GameObject("Sanctum decoration",typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                go.GetComponent<Renderer>().sharedMaterial=material;
                go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
            }
        }
        Material Make(Material source, Color color)
        {
            var material = new Material(source) { color = color, enableInstancing = true };
            materials.Add(material); return material;
        }
        public static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
            go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }
        public void Ring(string name, Transform parent, float radius, float thickness, float y, Material material)
        {
            const int segments = 64;
            for (int i=0;i<segments;i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var part=Part(name,PrimitiveType.Cube,parent,new Vector3(Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius),
                    new Vector3(radius * 2 * Mathf.PI / segments * 1.02f, thickness, thickness),material);
                part.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
        }
        public void BuildCrown(Transform parent)
        {
            parent.localScale=Vector3.one*1.25f;
            // Continuous band with eight sculpted points; both faces are visible.
            const int n=96;
            var vertices=new Vector3[(n+1)*4]; var uv=new Vector2[vertices.Length]; var triangles=new List<int>();
            for(int i=0;i<=n;i++)
            {
                float angle=i*Mathf.PI*2/n;
                float peak=Mathf.Pow((Mathf.Cos(angle*8)+1)*.5f,3);
                float top=.10f+peak*.18f;
                for(int k=0;k<4;k++)
                {
                    float r=k<2?.23f:.205f; float y=k%2==0?0:top;
                    vertices[i*4+k]=new Vector3(Mathf.Sin(angle)*r,y,Mathf.Cos(angle)*r);
                    uv[i*4+k]=new Vector2((float)i/n*3,y*4);
                }
                if(i==n)continue;
                for(int face=0;face<4;face++)
                {
                    int[] a={0,3,1,2}, b={1,2,3,0};
                    int x=i*4+a[face],z=i*4+b[face];
                    triangles.AddRange(new[]{x,z+4,z,x,x+4,z+4});
                }
            }
            var mesh=new Mesh {name="Eight-point royal crown"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject("Sculpted gold crown",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=Gold;
            Ring("Crown lower filigree",parent,.222f,.025f,.025f,Gold);
            Ring("Crown upper filigree",parent,.223f,.012f,.065f,Gold);
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16;
                Part("Crown beaded setting",PrimitiveType.Sphere,parent,new Vector3(Mathf.Sin(a)*.23f,.045f,Mathf.Cos(a)*.23f),Vector3.one*.013f,Gold);
            }
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                var setting=Part("Jewel bezel",PrimitiveType.Sphere,parent,new Vector3(Mathf.Sin(angle)*.232f,.10f,Mathf.Cos(angle)*.232f),new Vector3(.067f,.087f,.035f),Gold);
                setting.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                var gem=Part("Emerald crown jewel",PrimitiveType.Sphere,parent,new Vector3(Mathf.Sin(angle)*.235f,.10f,Mathf.Cos(angle)*.235f),new Vector3(.055f,.075f,.035f),Jade);
                gem.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var m in meshes)Destroy(m);}
    }
}
