using System.Collections.Generic;
using UnityEngine;

namespace CursedCrown
{
    // Cosmetic only: existing network avatar hitboxes remain authoritative.
    public sealed class TempleCharacter : MonoBehaviour
    {
        public static readonly string[] Names = { "Sun Guardian", "Jade Sentinel", "Crimson Warden", "Moon Ranger", "Royal Keeper" };
        static readonly Color[] Colors = { new(.85f,.55f,.12f), new(.08f,.48f,.32f), new(.65f,.09f,.13f), new(.18f,.36f,.65f), new(.43f,.18f,.62f) };
        static readonly Dictionary<PrimitiveType,Mesh> meshes = new();
        readonly List<Material> materials = new();
        Transform face, torso, left, right, leftArm, rightArm, leftLeg, rightLeg, helmet, crest;
        Transform root;
        Material cloth, skin;
        Renderer[] renderers;
        public int Choice { get; private set; } = -1;
        public void Build()
        {
            if(root != null) return;
            root = new GameObject("Temple warrior visuals").transform; root.SetParent(transform,false);
            cloth = Mat(Colors[0]); skin = Mat(new Color(.65f,.43f,.29f));
            var metal = Mat(new Color(.65f,.49f,.24f)); var dark = Mat(new Color(.075f,.065f,.06f));
            torso=Part("Armoured tunic",root,PrimitiveType.Capsule,Vector3.zero,new Vector3(.39f,.38f,.24f),cloth);
            Part("Chest plate",torso,PrimitiveType.Cube,new Vector3(0,.2f,.44f),new Vector3(.8f,.65f,.18f),metal);
            face=Part("Face",root,PrimitiveType.Sphere,Vector3.zero,new Vector3(.21f,.25f,.21f),skin);
            Part("Nose",face,PrimitiveType.Sphere,new Vector3(0,-.03f,.46f),new Vector3(.18f,.20f,.22f),skin);
            for(int side=-1;side<=1;side+=2)
                Part("Eye",face,PrimitiveType.Sphere,new Vector3(side*.20f,.09f,.43f),new Vector3(.10f,.08f,.06f),dark);
            helmet=Part("Helmet",face,PrimitiveType.Sphere,new Vector3(0,.31f,-.06f),new Vector3(1.08f,.58f,1.08f),metal);
            crest=Part("Crest",face,PrimitiveType.Cube,new Vector3(0,.68f,-.04f),new Vector3(.18f,.42f,.85f),cloth);
            left=Part("Left glove",root,PrimitiveType.Sphere,Vector3.zero,new Vector3(.09f,.11f,.15f),cloth);
            right=Part("Right glove",root,PrimitiveType.Sphere,Vector3.zero,new Vector3(.09f,.11f,.15f),cloth);
            leftArm=Part("Left sleeve",root,PrimitiveType.Cylinder,Vector3.zero,Vector3.one,cloth);
            rightArm=Part("Right sleeve",root,PrimitiveType.Cylinder,Vector3.zero,Vector3.one,cloth);
            leftLeg=Part("Left boot and leg",root,PrimitiveType.Cylinder,Vector3.zero,Vector3.one,dark);
            rightLeg=Part("Right boot and leg",root,PrimitiveType.Cylinder,Vector3.zero,Vector3.one,dark);
            renderers=root.GetComponentsInChildren<Renderer>();
            SetChoice(0);
        }
        Material Mat(Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color;
            m.SetFloat("_Smoothness",.25f); materials.Add(m); return m;
        }
        static Transform Part(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material mat)
        {
            if(!meshes.TryGetValue(type,out var mesh) || mesh==null)
            {
                var source=GameObject.CreatePrimitive(type);source.SetActive(false);
                mesh=source.GetComponent<MeshFilter>().sharedMesh;meshes[type]=mesh;Destroy(source);
            }
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        public void SetChoice(int choice)
        {
            Build(); Choice=Mathf.Clamp(choice,0,Names.Length-1);cloth.color=Colors[Choice];
            skin.color=Color.Lerp(new Color(.35f,.20f,.13f),new Color(.85f,.64f,.45f),Choice/4f);
            crest.localScale=new Vector3(Choice==2?.65f:.18f,Choice==4?.7f:.42f,Choice==3?.35f:.85f);
            helmet.localScale=new Vector3(1.08f,Choice==3?.35f:.58f,1.08f);
        }
        public void Pose(Vector3 head,Quaternion rotation,Vector3 lh,Quaternion lr,Vector3 rh,Quaternion rr,float floor,bool owner)
        {
            Build();face.SetPositionAndRotation(head,rotation);left.SetPositionAndRotation(lh,lr);right.SetPositionAndRotation(rh,rr);
            var yaw=Quaternion.Euler(0,rotation.eulerAngles.y,0);
            float height=Mathf.Clamp(head.y-floor,.65f,2.4f);
            var chest=new Vector3(head.x,floor+height*.67f,head.z);
            torso.SetPositionAndRotation(chest,yaw);
            Link(leftArm,chest+yaw*new Vector3(-.22f,.12f,0),lh,.075f);
            Link(rightArm,chest+yaw*new Vector3(.22f,.12f,0),rh,.075f);
            var hip=new Vector3(head.x,floor+height*.40f,head.z);
            Link(leftLeg,hip+yaw*new Vector3(-.10f,0,0),new Vector3(head.x,floor+.07f,head.z)+yaw*new Vector3(-.12f,0,.02f),.09f);
            Link(rightLeg,hip+yaw*new Vector3(.10f,0,0),new Vector3(head.x,floor+.07f,head.z)+yaw*new Vector3(.12f,0,.02f),.09f);
            foreach(var r in renderers) r.enabled=!owner || r.transform==left || r.transform==right;
        }
        static void Link(Transform part,Vector3 a,Vector3 b,float radius)
        {part.position=(a+b)*.5f;part.up=(b-a).normalized;part.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);}
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
    }
}
