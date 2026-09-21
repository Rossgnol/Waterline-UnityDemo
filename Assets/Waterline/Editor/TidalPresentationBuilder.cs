using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // Applies only to the delivered Day11 scene. Visual children never add physics.
    public static class TidalPresentationBuilder
    {
        private const string Folder = "Assets/Waterline/Generated/TidalPresentation";
        private const string RootName = "Tidal equipment presentation";
        private static Transform root;
        private static Material steel, enamel, rubber, blue, amber, pale, concrete, floor;
        private static Mesh cube;

        [MenuItem("Waterline/27 Repair tidal equipment presentation")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();
            string baseline = PhysicsSignature();
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            // Pickup dressing belongs to the pickup so it disappears with the reward.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Tidal pickup dressing").ToArray()) Object.DestroyImmediate(t.gameObject);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            root = new GameObject(RootName).transform;
            Materials();
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = primitive.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(primitive);
            Surfaces();
            var layout = Object.FindFirstObjectByType<HarborLayout>();
            LowerEquipment(layout.Planner);
            LowerSigns(layout);
            ToolBench();
            DockSigns();
            PumpBank();
            BridgeRails(layout.Planner);
            if (baseline != PhysicsSignature()) throw new InvalidOperationException("Art pass changed a collider or interaction transform");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Decor has colliders");
            TidalRouteBuilder.Validate();
            Audit();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] TIDAL ART PASS: collision and interaction transforms unchanged; surfaces, mounted controls, connected pipes, pumps and bridge rails saved.");
        }

        public static void ApplyAndCapture(){Apply();TidalRouteVisualCheck.RunArt();}

        private static void DockSigns()
        {
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sign ")))
            {
                label.GetComponent<WorldLabel>()?.Refresh();
                var p=label.transform.position;var q=label.transform.rotation;var front=q*Vector3.back;
                var size=label.GetComponent<MeshRenderer>().localBounds.size;
                size=Vector3.Scale(size,label.transform.lossyScale);
                var width=Mathf.Max(.85f,size.x+.16f);var height=Mathf.Max(.32f,size.y+.12f);
                Primitive(PrimitiveType.Cube,"Console sign backing",p-front*.04f,new Vector3(width,height,.06f),q,rubber);
                Label(label.text,p-front*.081f,label.transform.eulerAngles.y+180,label.characterSize);
                Pipe("Console sign stem",p-front*.04f-Vector3.up*height*.5f,p-front*.04f-Vector3.up*(height*.5f+.45f),.023f,steel);
            }
        }

        private static void Materials()
        {
            steel = Mat("Machined steel", new Color(.22f,.28f,.29f), .55f, .36f);
            enamel = Mat("Worn enamel", new Color(.44f,.53f,.50f), .3f, .29f);
            rubber = Mat("Recess and rubber", new Color(.035f,.05f,.055f), .05f, .2f);
            blue = Mat("Inlet blue", new Color(.08f,.48f,.58f), .35f, .32f);
            amber = Mat("Drain ochre", new Color(.83f,.48f,.10f), .3f, .3f);
            pale = Mat("Gauge face", new Color(.76f,.79f,.69f), .08f, .24f);
            concrete = Mat("Lower concrete", new Color(.42f,.46f,.46f), 0, .12f);
            floor = Mat("Lower slab", new Color(.35f,.40f,.41f), .05f, .18f);
            concrete.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Waterline/Generated/Harbor/Concrete grain.png"));
            floor.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Waterline/Generated/Harbor/Floor joints.png"));
        }

        private static void Surfaces()
        {
            var group = GameObject.Find("DAY 10 / tidal maintenance chambers");
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = r.name;
                bool slab = n.Contains("floor");
                bool wall = n.Contains("wall") || n.Contains("divider") || n == "Lower portal lintel";
                if (!slab && !wall) continue;
                // Each face uses physical dimensions: thin end caps no longer stretch a wall's UV scale.
                r.sharedMaterial = slab ? floor : concrete;
                r.GetComponent<MeshFilter>().sharedMesh = MetricCube(r.transform.lossyScale);
            }
            foreach (float x in new[] {-13.81f,13.81f})
            {
                Box("Lower wall skirting", new Vector3(x,-3.29f,12.7f), new Vector3(.045f,.3f,14.1f), steel);
                for (int i=0;i<5;i++) Box("Concrete vertical joint", new Vector3(x,-1.94f,6.4f+i*3), new Vector3(.024f,2.7f,.025f), rubber);
            }
            // Light sources already existed; fit a housing and lens around each source.
            foreach (var lamp in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name=="Maintenance lamp"))
            {
                var p = lamp.transform.position;
                Box("Maintenance luminaire", p+Vector3.up*.16f, new Vector3(1.5f,.15f,.38f), steel);
                Box("Maintenance diffuser", p+Vector3.up*.07f, new Vector3(1.32f,.035f,.25f), pale);
                foreach(float dx in new[]{-.62f,.62f}) Pipe("Luminaire fixing",p+new Vector3(dx,.2f,0),p+new Vector3(dx,.3f,0),.025f,steel);
            }
        }

        private static void LowerEquipment(TidalRoutePlanner planner)
        {
            foreach (string name in new[] {"Blue inlet header","Blue inlet drop","Orange drain header","Orange drain drop","Pipe riser"}) Hide(name);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r=>r.name=="Chamber wall equipment"))
            {
                var b=r.bounds; r.sharedMaterial=enamel;
                float x=b.max.x+.012f;
                Box("Cabinet door seam",new Vector3(x,b.center.y,b.center.z),new Vector3(.016f,b.size.y*.9f,.025f),rubber);
                for(int j=0;j<5;j++) Box("Cabinet ventilation",new Vector3(x,b.max.y-.18f-j*.09f,b.center.z),new Vector3(.018f,.025f,b.size.z*.74f),rubber);
                Pipe("Cabinet handle",new Vector3(x+.055f,b.center.y-.2f,b.center.z+.3f),new Vector3(x+.055f,b.center.y+.05f,b.center.z+.3f),.022f,steel);
            }
            foreach(string name in new[]{"Valve service bench","Pressure test pedestal","Rigging workbench","Drain record desk"})
            {
                var r=GameObject.Find(name).GetComponent<Renderer>();var b=r.bounds;r.sharedMaterial=enamel;
                Box("Equipment worktop",new Vector3(b.center.x,b.max.y+.035f,b.center.z),new Vector3(b.size.x+.07f,.07f,b.size.z+.07f),steel);
                Box("Plinth kick plate",new Vector3(b.center.x,b.min.y+.12f,b.min.z-.012f),new Vector3(b.size.x*.9f,.18f,.025f),rubber);
                for(int j=0;j<3;j++) Box("Bench service vent",new Vector3(b.center.x,b.center.y-.1f+j*.08f,b.min.z-.016f),new Vector3(b.size.x*.65f,.022f,.025f),rubber);
            }
            planner.intakeHandle=Valve(AnnexAction.ToggleFloatIntake,9,blue,planner.intakeLabel);
            planner.drainHandle=Valve(AnnexAction.ToggleFloatDrain,12,amber,planner.drainLabel);
            // Continuous headers sit above the portal; drops terminate on the mounted valve bodies.
            Circuit(new[]{new Vector3(9,-2.25f,14.05f),new Vector3(9,-2.25f,14.65f),new Vector3(9,-.79f,14.65f),new Vector3(9,-.79f,17),new Vector3(.2f,-.79f,17),new Vector3(.2f,-.79f,14.65f),new Vector3(.2f,-2.45f,14.65f),new Vector3(2,-2.45f,14.65f),new Vector3(2,-2.45f,14.2f)},blue);
            Circuit(new[]{new Vector3(2.32f,-2.4f,14.2f),new Vector3(2.32f,-2.4f,14.9f),new Vector3(0,-2.4f,14.9f),new Vector3(0,-.96f,14.9f),new Vector3(0,-.96f,17.4f),new Vector3(12,-.96f,17.4f),new Vector3(12,-.96f,14.65f),new Vector3(12,-2.25f,14.65f),new Vector3(12,-2.25f,14.05f)},amber);
            foreach (float x in new[]{4f,7f})
            {
                Pipe("Inlet header hanger",new Vector3(x,-.4f,17),new Vector3(x,-.79f,17),.018f,steel);
                Pipe("Drain header hanger",new Vector3(x,-.4f,17.4f),new Vector3(x,-.96f,17.4f),.018f,steel);
            }
            var test = Interaction(AnnexAction.TestFloatCircuit);test.GetComponent<Renderer>().enabled=false;
            Box("Pressure pump housing",new Vector3(2,-2.46f,14),new Vector3(.72f,.32f,.64f),enamel);
            Pipe("Test lever shaft",new Vector3(2,-2.45f,14),new Vector3(2,-2.12f,14),.035f,steel);
            Pipe("Test lever grip",new Vector3(1.78f,-2.12f,14),new Vector3(2.22f,-2.12f,14),.055f,amber);
            Gauge(new Vector3(2,-2.35f,13.66f),.16f);
            Pipe("Indicator bracket",new Vector3(2,-1.4f,14.25f),new Vector3(2,-1.85f,14.25f),.035f,steel);
            Box("Indicator housing",new Vector3(2,-1.85f,14.2f),new Vector3(.38f,.25f,.14f),rubber);
            foreach(float x in new[]{.6f,3.4f}) Pipe("Pressure display support",new Vector3(x,-3.5f,14.25f),new Vector3(x,-1.4f,14.25f),.035f,steel);
            var reward=planner.supplyPickup;reward.GetComponent<Renderer>().enabled=false;
            var saved=root;root=new GameObject("Tidal pickup dressing").transform;root.SetParent(reward.transform,false);
            // Preserve the interaction's transform and hit volume; the case fills the gap down to the worktop.
            Box("Decoy transport case",new Vector3(-10,-2.44f,16),new Vector3(.59f,.34f,.47f),blue);
            Box("Case lid",new Vector3(-10,-2.26f,16),new Vector3(.61f,.035f,.49f),steel);
            foreach(float x in new[]{-10.2f,-9.8f}) Box("Case latch",new Vector3(x,-2.34f,15.755f),new Vector3(.055f,.1f,.025f),pale);
            Pipe("Case handle",new Vector3(-10.13f,-2.19f,16),new Vector3(-9.87f,-2.19f,16),.024f,rubber);
            root=saved;
            // The record was also floating; a sloped reading support connects it to its desk.
            Box("Record lectern",new Vector3(11.5f,-2.48f,16),new Vector3(.72f,.28f,.6f),steel);
            var note=Interaction(AnnexAction.ReadFloodPlan);note.GetComponent<Renderer>().sharedMaterial=pale;
        }

        private static void LowerSigns(HarborLayout layout)
        {
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(label.text=="TOOLS / PIN + OPTIONAL NORTH" || label.text=="NORTH / OPTIONAL ESCAPE PREPARATION" || label.text=="FLOOD RETRACTS BRIDGE / PREPARE FLOAT OR WALK SOUTH")label.GetComponent<Renderer>().enabled=false;
            foreach(var door in layout.doors.Where(d=>d.position.y<0))
            {
                // Both faces sit outside the lintel, so letters don't intersect the beam or read mirrored.
                var p=door.position;p.y=-.79f;
                var front=door.vertical?Vector3.left:Vector3.back;
                door.sign.transform.position=p+front*.18f;door.sign.characterSize=.02f;
                var board=Primitive(PrimitiveType.Cube,"Lower portal sign backing",p,new Vector3(2.55f,.34f,.33f),Quaternion.Euler(0,door.vertical?90:0,0),rubber);
                Label(door.sign.text,p-front*.18f,door.vertical?-90:180,.02f);
            }
            Label("TOOLS / MAIN PIN",new Vector3(-12.45f,-1.28f,5.06f),0,.026f);
            Box("Tool room identity backing",new Vector3(-12.45f,-1.28f,5.09f),new Vector3(2.2f,.38f,.04f),rubber);
        }

        private static void ToolBench()
        {
            var older=GameObject.Find("Workbench").GetComponent<Renderer>();
            var newer=GameObject.Find("Tools pin workbench").GetComponent<Renderer>();
            var bounds=older.bounds;bounds.Encapsulate(newer.bounds);older.enabled=false;newer.enabled=false;
            Box("Unified tool workbench",bounds.center,bounds.size,enamel);
            Box("Tool bench top",new Vector3(bounds.center.x,bounds.max.y+.025f,bounds.center.z),new Vector3(bounds.size.x+.06f,.05f,bounds.size.z+.06f),steel);
            var pin=Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==DockAction.TakePin);
            pin.GetComponent<Renderer>().enabled=false;
            var saved=root;root=new GameObject("Tidal pickup dressing").transform;root.SetParent(pin.transform,false);
            var p=pin.transform.position;p.y=bounds.max.y+.13f;
            Pipe("Coupling pin shaft",p-Vector3.right*.27f,p+Vector3.right*.27f,.08f,pale);
            foreach(float dx in new[]{-.23f,.23f})Pipe("Coupling pin collar",p+Vector3.right*(dx-.025f),p+Vector3.right*(dx+.025f),.115f,amber);
            root=saved;
        }

        private static Transform Valve(AnnexAction action,float x,Material color,TextMesh label)
        {
            var interaction=Interaction(action);interaction.GetComponent<Renderer>().enabled=false;
            Pipe("Valve mounting foot",new Vector3(x,-2.62f,14.05f),new Vector3(x,-2.25f,14.05f),.09f,steel);
            Pipe("Valve body",new Vector3(x,-2.25f,13.86f),new Vector3(x,-2.25f,14.18f),.16f,color);
            var rotor=new GameObject("Valve rotating handwheel").transform;rotor.SetParent(root);rotor.position=new Vector3(x,-2.25f,13.76f);
            var saved=root;root=rotor;
            Ring("Valve rim",rotor.position,.265f,.027f,color);
            Pipe("Valve spindle",rotor.position+Vector3.back*.025f,rotor.position+Vector3.forward*.09f,.05f,steel);
            for(int i=0;i<3;i++)
            {float a=i*Mathf.PI*2/3;Pipe("Wheel spoke",rotor.position,rotor.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.24f,.022f,color);}
            Box("Valve position index",rotor.position+Vector3.up*.26f,new Vector3(.07f,.075f,.065f),pale);
            root=saved;
            label.transform.position=new Vector3(x,-1.5f,14.20f);
            Box("Valve status backing",new Vector3(x,-1.5f,14.24f),new Vector3(1.75f,.34f,.055f),rubber);
            foreach(float dx in new[]{-.7f,.7f}) Pipe("Valve label support",new Vector3(x+dx,-2.61f,14.24f),new Vector3(x+dx,-1.5f,14.24f),.022f,steel);
            return rotor;
        }

        private static void PumpBank()
        {
            // These Day08 motor parts intersected the Day11 vessels. Keep their old physics but remove the obsolete shell.
            foreach(string name in new[]{"Hoisted pump motor","Hoisted pump coupling","Hoist suspension cable","Motor cooling ring","Pump end flange","Flange fastener","Pump safety stripe"}) Hide(name);
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Pump pressure vessel"))
            {
                r.sharedMaterial=enamel;float z=r.transform.position.z;
                Primitive(PrimitiveType.Sphere,"Vessel domed crown",new Vector3(0,3.28f,z),new Vector3(4.78f,.45f,2.78f),Quaternion.identity,enamel);
                foreach(float y in new[]{1.15f,3.18f})Primitive(PrimitiveType.Cylinder,"Vessel seam band",new Vector3(0,y,z),new Vector3(4.85f,.035f,2.85f),Quaternion.identity,steel);
                Pipe("Vessel inspection flange",new Vector3(-2.38f,2.13f,z),new Vector3(-2.50f,2.13f,z),.42f,steel);
                Pipe("Vessel inspection cover",new Vector3(-2.51f,2.13f,z),new Vector3(-2.55f,2.13f,z),.33f,blue);
                for(int i=0;i<8;i++)
                {float a=i*Mathf.PI/4;var p=new Vector3(-2.56f,2.13f+Mathf.Cos(a)*.365f,z+Mathf.Sin(a)*.365f);Pipe("Cover bolt",p,p+Vector3.left*.025f,.027f,pale);}
                Circuit(new[]{new Vector3(2.35f,2.65f,z),new Vector3(2.85f,2.65f,z),new Vector3(2.85f,1.18f,z)},blue);
            }
        }

        private static void BridgeRails(TidalRoutePlanner planner)
        {
            var safety=Object.FindFirstObjectByType<BridgeSafety>();
            var visuals=new List<GameObject>();
            foreach(var gate in new[]{safety.west,safety.east})
            {
                var r=gate.GetComponent<Renderer>();r.enabled=false;var b=r.bounds;
                var saved=root;root=gate.transform;
                // Use a named child so reapplying this pass doesn't accumulate gate detail.
                var prior=root.Find("Tidal gate dressing");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
                var group=new GameObject("Tidal gate dressing").transform;group.SetParent(root,false);root=group;
                visuals.Add(group.gameObject);
                foreach(float z in new[]{b.min.z,b.center.z,b.max.z}) Pipe("Safety upright",new Vector3(b.center.x,b.min.y,z),new Vector3(b.center.x,b.max.y,z),.045f,amber);
                foreach(float y in new[]{b.center.y,b.max.y}) Pipe("Safety rail",new Vector3(b.center.x,y,b.min.z),new Vector3(b.center.x,y,b.max.z),.045f,amber);
                root=saved;
            }
            planner.dryBridgeVisuals=visuals.ToArray();
        }

        private static Mesh MetricCube(Vector3 scale)
        {
            string key=$"Surface-{Mathf.RoundToInt(scale.x*100)}-{Mathf.RoundToInt(scale.y*100)}-{Mathf.RoundToInt(scale.z*100)}";
            string path=Folder+"/"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=Object.Instantiate(cube);mesh.name=key;var v=mesh.vertices;var n=mesh.normals;var uv=new Vector2[v.Length];
            for(int i=0;i<v.Length;i++)
            {var p=Vector3.Scale(v[i]+Vector3.one*.5f,scale);uv[i]=Mathf.Abs(n[i].y)>.5f?new Vector2(p.x,p.z)/3:Mathf.Abs(n[i].x)>.5f?new Vector2(p.z,p.y)/3:new Vector2(p.x,p.y)/3;}
            mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        private static void Circuit(Vector3[] points,Material mat)
        {
            for(int i=0;i<points.Length-1;i++)Pipe("Connected service pipe",points[i],points[i+1],.065f,mat);
            foreach(var p in points)Primitive(PrimitiveType.Sphere,"Pipe elbow joint",p,Vector3.one*.15f,Quaternion.identity,mat);
        }
        private static void Gauge(Vector3 p,float radius)
        {
            Pipe("Gauge bezel",p,p+Vector3.forward*.07f,radius,steel);
            Pipe("Gauge dial",p-Vector3.forward*.005f,p+Vector3.forward*.002f,radius*.85f,pale);
            Pipe("Gauge needle",p-Vector3.forward*.016f,p+new Vector3(-radius*.43f,radius*.36f,-.016f),.009f,rubber);
        }
        private static void Ring(string name,Vector3 p,float radius,float tube,Material mat)
        {
            const string path=Folder+"/Handwheel.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int i=0;i<=40;i++)for(int j=0;j<=8;j++)
                {float a=i*Mathf.PI*2/40,b=j*Mathf.PI*2/8;vertices.Add(new Vector3((radius+tube*Mathf.Cos(b))*Mathf.Cos(a),(radius+tube*Mathf.Cos(b))*Mathf.Sin(a),tube*Mathf.Sin(b)));uv.Add(new Vector2(i/40f,j/8f));}
                for(int i=0;i<40;i++)for(int j=0;j<8;j++){int k=i*9+j;triangles.AddRange(new[]{k,k+9,k+1,k+1,k+9,k+10});}
                mesh=new Mesh{name="Valve handwheel"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.transform.position=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private static Material Mat(string name,Color tint,float metal,float smooth)
        {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);return m;}
        private static GameObject Primitive(PrimitiveType type,string name,Vector3 p,Vector3 size,Quaternion rotation,Material mat)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetPositionAndRotation(p,rotation);go.transform.localScale=size;go.transform.SetParent(root,true);go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        private static void Box(string name,Vector3 p,Vector3 size,Material mat){Primitive(PrimitiveType.Cube,name,p,size,Quaternion.identity,mat);}
        private static void Pipe(string name,Vector3 a,Vector3 b,float r,Material mat){Primitive(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(r*2,(b-a).magnitude*.5f,r*2),Quaternion.FromToRotation(Vector3.up,b-a),mat);}
        private static AnnexInteractable Interaction(AnnexAction action){return Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==action);}
        private static void Label(string text,Vector3 p,float yaw,float size)
        {
            var go=new GameObject("Mounted sign / "+text);go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=72;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.text=text;
            var depth=go.AddComponent<WorldLabel>();depth.template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/World text.mat");depth.Refresh();
        }
        private static void Hide(string name){foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name==name))r.enabled=false;}
        private static string PhysicsSignature()
        {
            return string.Join("\n",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+"/"+EditorJsonUtility.ToJson(c)+"/"+c.gameObject.activeSelf+"/"+c.transform.localToWorldMatrix.ToString("F5")))+"\n"+
                string.Join("\n",Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(i=>i.action+"/"+i.transform.localToWorldMatrix.ToString("F5")))+"\n"+
                string.Join("\n",Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(i=>i.action+"/"+i.transform.localToWorldMatrix.ToString("F5")));
        }
        private static void Audit()
        {
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(r.GetComponent<TextMesh>()!=null || !r.enabled)continue;
                if(r.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name.Contains("InternalError")))throw new InvalidOperationException("Broken material: "+r.name);
            }
            if(concrete.GetTexture("_BaseMap")==null || floor.GetTexture("_BaseMap")==null)throw new InvalidOperationException("Missing surface texture");
        }
    }
}
