using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class ConsolePresentationBuilder
    {
        internal const string RootName="Dock console finish presentation";
        private const string Folder="Assets/Waterline/Generated/Consoles";
        private static Transform root;
        private static Material steel,enamel,rubber,pale;

        [MenuItem("Waterline/33 Refine dock console cabinets")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();string baseline=Signature();
            var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();root=new GameObject(RootName).transform;
            steel=Mat("Machined steel");enamel=Mat("Worn enamel");rubber=Mat("Recess and rubber");pale=Mat("Gauge face");
            var mesh=BeveledHousing();var items=Items();if(items.Length!=5)throw new InvalidOperationException("Expected five dock consoles");
            foreach(var item in items)
            {
                var head=item.GetComponent<Renderer>().bounds;
                var stand=GameObject.Find(item.name+" stand");var body=stand.GetComponent<Renderer>().bounds;
                item.GetComponent<MeshFilter>().sharedMesh=mesh;stand.GetComponent<MeshFilter>().sharedMesh=mesh;
                stand.GetComponent<Renderer>().sharedMaterial=enamel;
                Cabinet(body,head);MountLabels(item,head);
            }
            if(baseline!=Signature())throw new InvalidOperationException("Console art changed physics, interactions, lighting or layout");
            if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Console decor contains collision");
            Combine();TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] CONSOLE ART PASS: five cabinets, corrected paired labels, unchanged collision/interaction/light/layout signatures.");
        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/"+name+".mat") ?? throw new InvalidOperationException(name);
        private static void Cabinet(Bounds b,Bounds h)
        {
            Box("Console anchored plinth",new Vector3(b.center.x,b.min.y+.045f,b.center.z),new Vector3(b.size.x,.09f,b.size.z),rubber);
            Box("Console base seam",new Vector3(b.center.x,b.min.y+.11f,b.center.z),new Vector3(b.size.x+.012f,.035f,b.size.z+.012f),steel);
            Box("Console upper gasket",new Vector3(b.center.x,b.max.y-.03f,b.center.z),new Vector3(b.size.x+.014f,.045f,b.size.z+.014f),rubber);
            foreach(float side in new[]{-1f,1f})
            {
                float z=b.center.z+side*(b.extents.z+.008f);float outward=side*.012f;
                var p=new Vector3(b.center.x,b.center.y+.015f,z);
                Box("Cabinet door seal",p,new Vector3(b.size.x*.78f,b.size.y*.72f,.018f),rubber);
                Box("Cabinet removable cover",p+Vector3.forward*outward,new Vector3(b.size.x*.73f,b.size.y*.67f,.012f),enamel);
                foreach(float y in new[]{b.center.y-.20f,b.center.y+.23f})Box("Cover hinge",new Vector3(b.min.x+.11f,y,z+outward*1.7f),new Vector3(.045f,.10f,.035f),steel);
                Box("Recessed cover latch",new Vector3(b.max.x-.16f,b.center.y+.10f,z+outward*1.7f),new Vector3(.033f,.12f,.025f),steel);
                for(int i=0;i<5;i++)Box("Cabinet ventilation slot",new Vector3(b.center.x,b.min.y+.23f+i*.037f,z+outward*1.7f),new Vector3(b.size.x*.43f,.012f,.014f),rubber);
                foreach(float x in new[]{b.min.x+.15f,b.max.x-.15f})Box("Base anchoring bolt",new Vector3(x,b.min.y+.13f,b.center.z+side*(b.extents.z-.075f)),new Vector3(.055f,.025f,.055f),steel);
                // Equipment label on both sides; these shallow plates are decorative, not extra interaction targets.
                var serial=new Vector3(h.center.x,h.center.y,h.center.z+side*(h.extents.z+.007f));
                if(side>0)
                {
                    Box("Rear service plate",serial,new Vector3(h.size.x*.66f,h.size.y*.61f,.014f),steel);
                    for(int i=0;i<4;i++)Box("Rear service plate engraving",serial+new Vector3(-.12f+i*.08f,0,.010f),new Vector3(.039f,.07f,.008f),pale);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                float x=b.center.x+side*(b.extents.x+.015f);
                Box("Cabinet side service seam",new Vector3(x,b.center.y,b.center.z),new Vector3(.018f,.58f,b.size.z*.64f),steel);
                Box("Cabinet side panel",new Vector3(x+side*.010f,b.center.y,b.center.z),new Vector3(.012f,.53f,b.size.z*.58f),enamel);
            }
        }
        private static void MountLabels(DockInteractable item,Bounds head)
        {
            string value=PrototypeBuilder.ControlLabel(item.action);
            var pair=Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.text==value && (t.name.StartsWith("Sign ") || t.name=="Mounted sign / "+value)).ToArray();
            if(pair.Length!=2)throw new InvalidOperationException("Missing label pair for "+item.action);
            var oldFront=pair.Single(t=>t.name.StartsWith("Sign "));var oldPoint=oldFront.transform.position;
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>(r.name=="Console sign backing" || r.name=="Console sign stem") && Mathf.Abs(r.transform.position.x-oldPoint.x)<.08f && Mathf.Abs(r.transform.position.z-oldPoint.z)<.12f))r.enabled=false;
            var p=new Vector3(head.center.x,head.max.y+.25f,head.center.z+.12f);
            Box("Console nameplate surround",p,new Vector3(.96f,.35f,.065f),steel);
            foreach(var text in pair)
            {
                bool front=text==oldFront;float side=front?-1:1;
                text.transform.SetPositionAndRotation(p+Vector3.forward*(side*.043f),Quaternion.Euler(0,front?0:180,0));text.characterSize=.023f;text.GetComponent<WorldLabel>().Refresh();
                Box("Console nameplate face",p+Vector3.forward*(side*.037f),new Vector3(.90f,.29f,.01f),rubber);
                if(text.GetComponent<Renderer>().localBounds.size.x>.84f)throw new InvalidOperationException("Nameplate text overflow");
            }
            foreach(float dx in new[]{-.33f,.33f})
            {
                Box("Nameplate bracket",new Vector3(p.x+dx,head.max.y+.07f,p.z),new Vector3(.035f,.14f,.04f),steel);
                Box("Nameplate mounting foot",new Vector3(p.x+dx,head.max.y+.009f,p.z),new Vector3(.13f,.018f,.17f),steel);
            }
        }
        private static void Box(string name,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name=name;go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        private static Mesh BeveledHousing()
        {
            var ring=new[]{new Vector2(-.44f,-.5f),new Vector2(.44f,-.5f),new Vector2(.5f,-.44f),new Vector2(.5f,.44f),new Vector2(.44f,.5f),new Vector2(-.44f,.5f),new Vector2(-.5f,.44f),new Vector2(-.5f,-.44f)};
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<8;i++)
            {
                var a=ring[i];var b=ring[(i+1)%8];var at=new Vector3(a.x,.5f,a.y);var bt=new Vector3(b.x,.5f,b.y);var ab=new Vector3(a.x,-.5f,a.y);var bb=new Vector3(b.x,-.5f,b.y);
                Face(at,bt,bb);Face(at,bb,ab);Face(Vector3.up*.5f,bt,at);Face(Vector3.down*.5f,ab,bb);
            }
            var mesh=new Mesh{name="Chamfered console housing"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Save(mesh);
            void Face(Vector3 a,Vector3 b,Vector3 c)
            {
                int n=v.Count;var normal=Vector3.Cross(b-a,c-a).normalized;
                Vector2 Uv(Vector3 p)=>Mathf.Abs(normal.y)>.5f?new Vector2(p.x+.5f,p.z+.5f):Mathf.Abs(normal.x)>Mathf.Abs(normal.z)?new Vector2(p.z+.5f,p.y+.5f):new Vector2(p.x+.5f,p.y+.5f);
                v.AddRange(new[]{a,b,c});uv.AddRange(new[]{Uv(a),Uv(b),Uv(c)});triangles.AddRange(new[]{n,n+1,n+2});
            }
        }
        private static Mesh Save(Mesh mesh)
        {
            string path=Folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }
        private static void Combine()
        {
            var parts=root.GetComponentsInChildren<MeshRenderer>();
            foreach(var group in parts.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh{name="Console fittings "+group.Key.name};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=Save(mesh);go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var r in parts)Object.DestroyImmediate(r.gameObject);
        }
        private static string Signature()
        {
            var physics=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5")+c.gameObject.activeSelf);
            var logic=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c is DockInteractable || c is AnnexInteractable || c is DockSession || c is HarborLayout || c is TidalRoutePlanner).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5"));
            var light=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c));
            return string.Join("\n",physics.Concat(logic).Concat(light));
        }
        public static void Audit()
        {
            TidalRouteBuilder.Open();
            foreach(var item in Items())
            {
                var stand=GameObject.Find(item.name+" stand");
                Debug.Log($"[CONSOLE AUDIT] {item.action}: {item.GetComponent<Renderer>().bounds}; stand={stand?.GetComponent<Renderer>().bounds}");
                string label=PrototypeBuilder.ControlLabel(item.action);
                foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.text==label))
                    Debug.Log($"[CONSOLE LABEL] {text.name}: {text.transform.position:F3}, yaw={text.transform.eulerAngles.y}, bounds={text.GetComponent<Renderer>().bounds}");
            }
            Capture("before");
        }
        internal static DockInteractable[] Items()=>Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(i=>i.action!=DockAction.TakePin).OrderBy(i=>i.action).ToArray();
        public static void CaptureSaved(){TidalRouteBuilder.Open();Capture("after");}
        internal static void Capture(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;string folder="TestResults/ConsoleFinish/"+phase;Directory.CreateDirectory(folder);
            foreach(var item in Items())
            {
                var p=item.transform.position;
                BoatPresentationBuilder.Shot(camera,folder+"/"+item.action+"-front.png",p+new Vector3(1.65f,.55f,-2.1f),p+Vector3.down*.22f);
                // The flood console backs onto a wall; inspect its exposed side instead of placing the camera through that wall.
                var offset=item.action==DockAction.StartFlood?new Vector3(1.8f,.65f,-.9f):item.action==DockAction.BoardBoat?new Vector3(1.55f,.55f,2.1f):new Vector3(-1.55f,.55f,2.1f);
                BoatPresentationBuilder.Shot(camera,folder+"/"+item.action+"-"+(item.action==DockAction.StartFlood?"side":"back")+".png",p+offset,p+Vector3.down*.22f);
            }
            Debug.Log("[Waterline] CONSOLE CAPTURE "+phase+" complete.");
        }
    }
}
