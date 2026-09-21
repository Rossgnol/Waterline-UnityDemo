using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // A repeatable art pass on Day08. All new geometry decorates existing surfaces or overhead space.
    // No new colliders, lights, interactables or runtime rules are introduced.
    public static partial class HarborPresentationBuilder
    {
        private const string Folder = "Assets/Waterline/Generated/Presentation";
        private const string RootName = "Harbor presentation - dock and watch hall";
        private const string BoatRootName = "Inspection boat detailing";
        private static Transform root;
        private static Material steel, ivory, oxide, brass, blue, glass, rubber, light;

        [MenuItem("Waterline/19 Apply dock and hall presentation")]
        public static void Apply()
        {
            ExplorationRewardsBuilder.Open();
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var dock = Object.FindFirstObjectByType<DockSession>();
            var baseline = PhysicsSignature();
            if (GameObject.Find(RootName) == null) Capture("before");
            foreach (string name in new[] { RootName, BoatRootName })
            { var old = GameObject.Find(name); if (old != null) Object.DestroyImmediate(old); }
            LoadMaterials();
            root = new GameObject(RootName).transform;
            Hall(); Dock(); Boat(dock);
            if (PhysicsSignature() != baseline) throw new InvalidOperationException("Presentation changed original physics or interaction transforms");
            foreach (var group in new[] { GameObject.Find(RootName), GameObject.Find(BoatRootName) })
                if (group.GetComponentsInChildren<Collider>(true).Length != 0 || group.GetComponentsInChildren<Light>(true).Length != 0)
                    throw new InvalidOperationException("Decor must not change collision or light perception");
            ExplorationRewardsBuilder.Validate();
            AssetDatabase.SaveAssets();
            // Capture restores the original camera pose before the scene is saved.
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ExplorationRewardsBuilder.ScenePath);
            Capture("after");
            Debug.Log("[Waterline] PRESENTATION PASS: original collider/interaction transforms unchanged; dock, hall and boat detail saved.");
        }

        private static void LoadMaterials()
        {
            steel = Mat("Graphite steel", new Color(.13f,.19f,.21f), .65f);
            ivory = Mat("Aged enamel", new Color(.68f,.70f,.62f), .25f);
            oxide = Mat("Oxide hull", new Color(.29f,.12f,.085f), .35f);
            brass = Mat("Safety ochre", new Color(.8f,.47f,.12f), .35f);
            blue = Mat("Service blue", new Color(.08f,.35f,.46f), .4f);
            glass = Mat("Smoked marine glass", new Color(.055f,.14f,.18f), .6f);
            rubber = Mat("Fender rubber", new Color(.025f,.038f,.041f));
            light = Mat("Fixture lens", new Color(.78f,.83f,.72f));
            light.EnableKeyword("_EMISSION"); light.SetColor("_EmissionColor", new Color(.28f,.32f,.25f));
        }

        private static void Hall()
        {
            // Structure is above head clearance; existing gantry remains the central silhouette.
            foreach (float z in new[] { 8f, 14f, 20f })
            {
                Box("Hall roof rib", new Vector3(0,5.13f,z), new Vector3(27.6f,.28f,.26f), steel);
                for (int i=0;i<7;i++)
                    Box("Rib gusset", new Vector3(-12+i*4,4.95f,z), new Vector3(.18f,.42f,.34f), steel);
            }
            foreach (float x in new[] {-12.8f,12.8f})
            {
                Pipe("High service main",new Vector3(x,4.45f,6.4f),new Vector3(x,4.45f,21.6f),.11f,x<0?blue:brass);
                for(int i=0;i<6;i++)
                    Box("Pipe wall bracket",new Vector3(x,4.45f,7.5f+i*2.5f),new Vector3(.4f,.38f,.07f),steel);
            }
            foreach (float x in new[] {-3f,3f})
            {
                Box("Gantry foot shoe",new Vector3(x,.16f,15),new Vector3(.44f,.3f,.44f),steel);
                for(int i=0;i<4;i++)
                    Box("Gantry warning band",new Vector3(x,.45f+i*.24f,14.767f),new Vector3(.43f,.09f,.02f),rubber);
                Box("Crane runway",new Vector3(x,4.88f,15),new Vector3(.2f,.15f,6),steel);
            }
            Box("Hoist trolley",new Vector3(0,4.62f,15),new Vector3(2.8f,.35f,.85f),steel);
            Pipe("Hoist drum",new Vector3(-.6f,4.3f,15),new Vector3(.6f,4.3f,15),.24f,brass);
            // Thin surface dressing on the existing motor end, outside the walkable routes.
            Pipe("Pump end flange",new Vector3(-1.64f,2.1f,15),new Vector3(-1.6f,2.1f,15),.93f,ivory);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Pipe("Flange fastener",new Vector3(-1.64f,2.1f+Mathf.Cos(a)*.77f,15+Mathf.Sin(a)*.77f),
                    new Vector3(-1.68f,2.1f+Mathf.Cos(a)*.77f,15+Mathf.Sin(a)*.77f),.055f,steel);
            }
            // Painted lifting bay and route markers are flush with the floor.
            foreach(float x in new[]{-4.1f,4.1f}) Box("Lifting bay paint",new Vector3(x,.014f,15),new Vector3(.07f,.008f,5),brass);
            foreach(float z in new[]{12.5f,17.5f}) Box("Lifting bay paint",new Vector3(0,.014f,z),new Vector3(8.2f,.008f,.07f),brass);
            for(int i=0;i<12;i++) Box("No storage hatch",new Vector3(-3.7f+i*.67f,.015f,12.8f),new Vector3(.28f,.008f,.42f),brass);
            // Surface-mounted panels avoid the door apertures and sightline to the route desk.
            foreach(float x in new[]{-13.84f,13.84f})
                foreach(float z in new[]{8.5f,10.5f,17.5f,19.5f})
                {
                    Box("Riveted wall panel",new Vector3(x,1.25f,z),new Vector3(.035f,2.2f,1.75f),steel);
                    Box("Wing color strip",new Vector3(x-Mathf.Sign(x)*.024f,2.27f,z),new Vector3(.022f,.13f,1.75f),x<0?blue:brass);
                    for(int j=0;j<4;j++)Box("Panel vent",new Vector3(x-Mathf.Sign(x)*.025f,.45f+j*.16f,z),new Vector3(.025f,.045f,1.35f),rubber);
                }
            Board(new Vector3(-5.5f,2.15f,21.82f),0,"01 / WEST WORKS","PARTS > AUX POWER",blue);
            Board(new Vector3(5.5f,2.15f,21.82f),0,"02 / EAST SURVEY","INSTRUMENTS > TIDE",brass);
            Board(new Vector3(0,2.1f,6.18f),180,"03 / RETURN TO DOCK","PRESSURE > FLOOD > DEPART",ivory);
            // Radio and writing pad dress the existing solid desk, leaving its readable note exposed.
            Box("Desk radio",new Vector3(-7.95f,1.17f,10.17f),new Vector3(.55f,.34f,.3f),steel);
            for(int i=0;i<5;i++) Box("Radio speaker slot",new Vector3(-8.13f+i*.065f,1.17f,10.012f),new Vector3(.024f,.21f,.012f),rubber);
            Pipe("Radio aerial",new Vector3(-7.75f,1.34f,10.2f),new Vector3(-7.75f,1.85f,10.2f),.013f,steel);
        }

        private static void Dock()
        {
            // The retaining wall already occupies z=5.36. Use its basin face for seams, drains and scale.
            for(int i=0;i<15;i++)
            {
                float x=-19.5f+i*2.8f;
                Box("Seawall expansion seam",new Vector3(x,-1.82f,5.095f),new Vector3(.045f,3.4f,.015f),rubber);
                Box("Seawall cap plate",new Vector3(x,-.28f,5.07f),new Vector3(1.3f,.24f,.035f),ivory);
                Pipe("Wall drain outlet",new Vector3(x,-2.9f,5.05f),new Vector3(x,-2.9f,4.98f),.13f,rubber);
            }
            foreach(float x in new[]{-5.5f,5.5f})
            {
                Box("Draft scale backing",new Vector3(x,-1.8f,5.065f),new Vector3(.42f,3.1f,.03f),ivory);
                for(int i=0;i<13;i++)
                    Box("Draft scale graduation",new Vector3(x,-3.22f+i*.235f,5.041f),new Vector3(i%4==0?.38f:.21f,.04f,.016f),oxide);
            }
            // Upper service-room structure sits beyond the existing head clearance.
            foreach(float x in new[]{-11.1f,11.1f})
            {
                Pipe("Dock overhead service",new Vector3(x,3.12f,-10.5f),new Vector3(x,3.12f,3.9f),.095f,x<0?blue:brass);
                foreach(float z in new[]{-9f,-5f,-1f,3f})
                {
                    Box("Dock utility bracket",new Vector3(x,3.29f,z),new Vector3(.28f,.34f,.07f),steel);
                    Box("Dock ceiling batten",new Vector3(x,3.35f,z),new Vector3(7.7f,.16f,.12f),steel);
                }
            }
        }

        private static void Boat(DockSession dock)
        {
            root=new GameObject(BoatRootName).transform;root.SetParent(dock.boat,false);
            var cabin=dock.boat.Find("Cabin").GetComponent<Renderer>();
            var hull=dock.boat.Find("Hull").GetComponent<MeshRenderer>();
            var deck=dock.boat.Find("Deck").GetComponent<MeshRenderer>();
            hull.sharedMaterial=oxide; deck.sharedMaterial=ivory; cabin.sharedMaterial=ivory;
            hull.GetComponent<MeshFilter>().sharedMesh=HullMesh();
            var c=cabin.bounds;var h=hull.bounds;var d=deck.bounds;
            Box("Wheelhouse roof rim",new Vector3(c.center.x,c.max.y+.035f,c.center.z),new Vector3(c.size.x+.12f,.07f,c.size.z+.12f),steel);
            foreach(float side in new[]{-1f,1f})
            {
                float x=c.center.x+side*(c.extents.x+.013f);
                for(int i=0;i<2;i++)
                    Box("Side wheelhouse window",new Vector3(x,c.center.y+.16f,c.center.z-.48f+i*.95f),new Vector3(.024f,.46f,.72f),glass);
                for(int i=0;i<4;i++)
                {
                    float z=h.center.z-2.3f+i*1.45f;
                    Pipe("Rubber side fender",new Vector3(h.center.x+side*(h.extents.x-.035f),h.max.y-.13f,z),
                        new Vector3(h.center.x+side*(h.extents.x-.035f),h.max.y-.63f,z),.12f,rubber);
                }
                // Deck handrail feet stay over the solid boat footprint.
                float rx=d.center.x+side*(d.extents.x-.16f);
                for(int i=0;i<5;i++)
                    Pipe("Boat rail stanchion",new Vector3(rx,d.max.y,d.center.z-2.8f+i*1.3f),new Vector3(rx,d.max.y+.58f,d.center.z-2.8f+i*1.3f),.025f,steel);
                Pipe("Boat handrail",new Vector3(rx,d.max.y+.58f,d.center.z-2.8f),new Vector3(rx,d.max.y+.58f,d.center.z+2.4f),.03f,steel);
            }
            for(int i=0;i<4;i++)
                Box("Deck plank seam",new Vector3(d.center.x-.95f+i*.63f,d.max.y+.008f,d.center.z),new Vector3(.025f,.008f,d.size.z-.15f),steel);
            Pipe("Mast",new Vector3(c.center.x,c.max.y,c.center.z),new Vector3(c.center.x,c.max.y+.75f,c.center.z),.035f,steel);
            Box("Mast navigation lens",new Vector3(c.center.x,c.max.y+.75f,c.center.z),new Vector3(.14f,.14f,.14f),light);
            Box("Foredeck hatch",new Vector3(d.center.x,d.max.y+.035f,d.center.z+2.4f),new Vector3(1.05f,.06f,.8f),steel);
            Label("WL-07",new Vector3(c.center.x,c.center.y-.25f,c.max.z+.022f),180,.05f);
        }

        private static Mesh HullMesh()
        {
            string path=Folder+"/Inspection hull.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh!=null)return mesh;
            // Chamfered bow/stern inside the unchanged original hull collider.
            var ring=new[]{new Vector2(-.34f,-.5f),new Vector2(.34f,-.5f),new Vector2(.5f,-.36f),new Vector2(.5f,.3f),new Vector2(.23f,.5f),new Vector2(-.23f,.5f),new Vector2(-.5f,.3f),new Vector2(-.5f,-.36f)};
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 e){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,e});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];
                Face(new Vector3(a.x,.5f,a.y),new Vector3(b.x,.5f,b.y),new Vector3(b.x*.83f,-.5f,b.y*.9f),new Vector3(a.x*.83f,-.5f,a.y*.9f));
                Face(Vector3.up*.5f,new Vector3(b.x,.5f,b.y),new Vector3(a.x,.5f,a.y),Vector3.up*.5f);
            }
            mesh=new Mesh{name="Chamfered inspection hull"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        private static Material Mat(string name,Color color,float metal=0)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",.28f);return mat;
        }
        private static GameObject Box(string name,Vector3 p,Vector3 s,Material mat)
        {return Primitive(PrimitiveType.Cube,name,p,s,Quaternion.identity,mat);}
        private static void Pipe(string name,Vector3 a,Vector3 b,float r,Material mat)
        {Primitive(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(r*2,(b-a).magnitude*.5f,r*2),Quaternion.FromToRotation(Vector3.up,b-a),mat);}
        private static GameObject Primitive(PrimitiveType type,string name,Vector3 p,Vector3 s,Quaternion q,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,q);go.transform.localScale=s;
            go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static void Board(Vector3 p,float yaw,string title,string subtitle,Material color)
        {
            var box=Box("Wing wayfinding board",p,new Vector3(5,1.25f,.045f),steel);box.transform.rotation=Quaternion.Euler(0,yaw,0);
            Vector3 front=Quaternion.Euler(0,yaw,0)*Vector3.back;
            var stripe=Box("Board color header",p+Vector3.up*.5f+front*.03f,new Vector3(4.8f,.15f,.02f),color);stripe.transform.rotation=box.transform.rotation;
            Label(title,p+Vector3.up*.12f+front*.032f,yaw,.037f);Label(subtitle,p-Vector3.up*.3f+front*.032f,yaw,.025f);
        }
        private static void Label(string value,Vector3 p,float yaw,float size)
        {
            var previous=AnnexBuilder.root;AnnexBuilder.root=root;AnnexBuilder.Label(value,p,yaw,size);AnnexBuilder.root=previous;
        }
        private static string PhysicsSignature()
        {
            return string.Join("\n",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID())
                .Select(c=>c.GetInstanceID()+"/"+c.enabled+"/"+c.transform.localToWorldMatrix.ToString("F5")))+"\n"+
                string.Join("\n",Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(i=>i.action).Select(i=>i.action+"/"+i.transform.position.ToString("F5")));
        }
        public static void CaptureAfter(){ExplorationRewardsBuilder.Open();Capture("after");}
        private static void Capture(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;
            Directory.CreateDirectory("TestResults/Presentation");
            Shot("dock",new Vector3(-10,1.7f,-7),new Vector3(4,-1,-3));
            Shot("hall",new Vector3(-11,1.7f,9),new Vector3(0,2.3f,17));
            Shot("hall-return",new Vector3(8,1.7f,20),new Vector3(-1,2.3f,8));
            Shot("boat",new Vector3(5,-1.2f,-9),new Vector3(0,-2,-3));
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;
            void Shot(string name,Vector3 p,Vector3 look)
            {
                camera.transform.position=p;camera.transform.LookAt(look);camera.fieldOfView=72;
                var target=new RenderTexture(1280,720,24);var active=RenderTexture.active;var oldTarget=camera.targetTexture;
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("TestResults/Presentation/"+name+"-"+phase+".png",image.EncodeToPNG());
                camera.targetTexture=oldTarget;RenderTexture.active=active;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
