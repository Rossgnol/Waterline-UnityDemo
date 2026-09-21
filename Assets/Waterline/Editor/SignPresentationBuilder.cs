using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static class SignPresentationBuilder
    {
        private static readonly string[] Targets = { "BRIDGE RETRACTS ON FLOOD", "WATERLINE / 07", "FORWARD BERTH / EXIT", "FUSE / RETURN TO POWER", "CALIBRATION PLATE > TIDE" };
        internal const string RootName = "Mounted wayfinding presentation";
        private static Transform root;
        private static Material steel, dark, amber, blue;

        [MenuItem("Waterline/32 Mount dock and workbench signs")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();
            string before = Signature();
            var old = GameObject.Find(RootName); if(old != null) Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;
            steel=Mat("Machined steel");dark=Mat("Recess and rubber");amber=Mat("Drain ochre");blue=Mat("Inlet blue");
            var labels=Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var text in labels.Where(t=>t.name=="BRIDGE RETRACTS ON FLOOD"))
            {
                float side=Mathf.Sign(text.transform.position.x);
                var p=new Vector3(side*7.35f,1.72f,3.30f);float yaw=side<0?90:-90;
                Physics.SyncTransforms();
                var overlaps=Physics.OverlapBox(p,new Vector3(.86f,.385f,.045f),Quaternion.Euler(0,yaw,0),~0,QueryTriggerInteraction.Ignore);
                if(overlaps.Length>0)throw new InvalidOperationException("Bridge warning sign overlaps: "+string.Join("; ",overlaps.Select(c=>c.name+" "+c.bounds)));
                Mount(text,"BRIDGE RETRACTS\nON FLOOD",p,yaw,new Vector2(1.65f,.70f),.021f,amber);
                var q=Quaternion.Euler(0,yaw,0);
                foreach(float x in new[]{-.53f,.53f})
                {
                    var foot=p+q*new Vector3(x,-p.y,.04f);
                    Box("Warning sign foot",foot+Vector3.up*.03f,new Vector3(.23f,.06f,.23f),Quaternion.identity,steel);
                    Box("Warning sign upright",foot+Vector3.up*.77f,new Vector3(.055f,1.48f,.055f),Quaternion.identity,steel);
                }
            }
            // Replace the old free-standing dock identity backing and its redundant wall-facing copy.
            var identity=labels.Single(t=>t.name=="Sign WATERLINE / 07");
            labels.Single(t=>t.name=="Mounted sign / WATERLINE / 07").GetComponent<Renderer>().enabled=false;
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(r=>(r.name=="Console sign backing" || r.name=="Console sign stem") && Mathf.Abs(r.transform.position.x)<.1f && Mathf.Abs(r.transform.position.z-4.84f)<.1f))r.enabled=false;
            Mount(identity,"WATERLINE / 07",new Vector3(0,2.85f,5.16f),0,new Vector2(2.9f,.57f),.046f,blue);
            WallBrackets(new Vector3(0,2.85f,5.16f),2.9f,.57f,5.27f);

            var berth=labels.Single(t=>t.name=="FORWARD BERTH / EXIT");
            var berthCenter=new Vector3(8.4f,2.05f,-.22f);
            Mount(berth,"FORWARD BERTH\nEXIT",berthCenter,180,new Vector2(1.95f,.70f),.024f,amber);
            foreach(float x in new[]{7.90f,8.90f})Box("Berth sign console support",new Vector3(x,1.49f,-.26f),new Vector3(.045f,.52f,.045f),Quaternion.identity,steel);

            var fuse=labels.Single(t=>t.name=="Annex sign FUSE / RETURN TO POWER");
            var fuseCenter=new Vector3(-27,1.85f,29.48f);
            Mount(fuse,"SPARE FUSE\nRETURN TO AUX POWER",fuseCenter,0,new Vector2(2.6f,.70f),.025f,blue);
            foreach(float x in new[]{-28f,-26f})
            {
                Box("Fuse sign bench bracket",new Vector3(x,1.25f,29.48f),new Vector3(.045f,.50f,.045f),Quaternion.identity,steel);
                Box("Fuse sign mounting foot",new Vector3(x,1.012f,29.36f),new Vector3(.19f,.025f,.30f),Quaternion.identity,steel);
            }
            var plate=labels.Single(t=>t.name=="Annex sign CALIBRATION PLATE > TIDE");
            var plateCenter=new Vector3(-9,1.60f,69.75f);
            Mount(plate,"CALIBRATION PLATE\nFIT TO TIDE FRAME",plateCenter,0,new Vector2(3.0f,.76f),.027f,amber);
            WallBrackets(plateCenter,3,.76f,69.84f);
            if(Signature()!=before)throw new InvalidOperationException("Sign art changed gameplay, physics or light settings");
            if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Sign decor contains collision");
            Combine();TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] SIGN ART PASS: six mounted sign stations, original collision, interactions, lights and layout unchanged.");
        }

        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/"+name+".mat") ?? throw new InvalidOperationException("Missing sign material "+name);
        private static void Mount(TextMesh text,string value,Vector3 p,float yaw,Vector2 size,float character,Material accent)
        {
            var q=Quaternion.Euler(0,yaw,0);var front=q*Vector3.back;
            text.text=value;text.characterSize=character;text.alignment=TextAlignment.Center;
            text.transform.SetPositionAndRotation(p+front*.042f,q);text.GetComponent<WorldLabel>().Refresh();
            Box("Sign steel surround",p,new Vector3(size.x+.07f,size.y+.07f,.06f),q,steel);
            Box("Sign enamel face",p+front*.034f,new Vector3(size.x,size.y,.012f),q,dark);
            Box("Sign purpose stripe",p+front*.041f+Vector3.up*(size.y*.5f-.047f),new Vector3(size.x-.1f,.055f,.009f),q,accent);
            foreach(float x in new[]{-.5f,.5f})foreach(float y in new[]{-.5f,.5f})
                Box("Sign fastener",p+q*new Vector3(x*(size.x-.11f),y*(size.y-.12f),-.044f),new Vector3(.027f,.027f,.014f),q,steel);
            var b=text.GetComponent<Renderer>().localBounds.size;
            if(b.x>size.x-.14f || b.y>size.y-.16f)throw new InvalidOperationException("Sign text exceeds face: "+text.name+" "+b);
        }
        private static void WallBrackets(Vector3 p,float width,float height,float wallZ)
        {
            float depth=wallZ-p.z;
            foreach(float x in new[]{-.38f,.38f})foreach(float y in new[]{-.28f,.28f})
                Box("Wall sign spacer",p+new Vector3(width*x,height*y,depth*.5f),new Vector3(.065f,.065f,depth),Quaternion.identity,steel);
        }
        private static void Box(string name,Vector3 p,Vector3 size,Quaternion rotation,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name=name;go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(p,rotation);go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        private static void Combine()
        {
            const string folder="Assets/Waterline/Generated/Signs";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var parts=root.GetComponentsInChildren<MeshRenderer>();
            foreach(var group in parts.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh{name="Mounted signs "+group.Key.name};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
                string path=folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){saved=mesh;AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                var go=new GameObject(saved.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var part in parts)Object.DestroyImmediate(part.gameObject);
        }
        private static string Signature()
        {
            var physics=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5")+c.gameObject.activeSelf);
            var interactions=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c is DockInteractable || c is AnnexInteractable || c is DockSession || c is HarborLayout || c is TidalRoutePlanner).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5"));
            var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c));
            return string.Join("\n",physics.Concat(interactions).Concat(lights));
        }

        public static void Audit()
        {
            TidalRouteBuilder.Open();
            foreach (var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => Targets.Contains(t.text)))
            {
                var renderer = text.GetComponent<Renderer>();
                Debug.Log($"[SIGN AUDIT] {text.name}; position={text.transform.position:F3}; rotation={text.transform.eulerAngles:F2}; scale={text.transform.lossyScale:F3}; character={text.characterSize}; enabled={renderer.enabled}; bounds={renderer.bounds}; parent={text.transform.parent?.name}");
                foreach(var other in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.GetComponent<TextMesh>() == null && Vector3.Distance(r.bounds.ClosestPoint(text.transform.position),text.transform.position)<.5f))
                    Debug.Log($"[SIGN NEIGHBOR] {other.name}; position={other.transform.position:F3}; bounds={other.bounds}; parent={other.transform.parent?.name}");
            }
            Capture("before");
        }

        public static void CaptureSaved(){TidalRouteBuilder.Open();Capture("after");}
        internal static void Capture(string phase)
        {
            var camera = Object.FindFirstObjectByType<FirstPersonController>().view;
            string folder = "TestResults/SignFinish/" + phase; Directory.CreateDirectory(folder);
            BoatPresentationBuilder.Shot(camera,folder+"/east-bridge.png",new Vector3(10,1.7f,2),new Vector3(6,1.5f,2));
            BoatPresentationBuilder.Shot(camera,folder+"/west-bridge.png",new Vector3(-10,1.7f,2),new Vector3(-6,1.5f,2));
            BoatPresentationBuilder.Shot(camera,folder+"/berth.png",new Vector3(11,1.7f,3.5f),new Vector3(8.4f,1.5f,0));
            BoatPresentationBuilder.Shot(camera,folder+"/dock-marker.png",new Vector3(4,-1.1f,-7),new Vector3(0,1.7f,4));
            BoatPresentationBuilder.Shot(camera,folder+"/fuse.png",new Vector3(-25,1.7f,27),new Vector3(-27,1.4f,29));
            BoatPresentationBuilder.Shot(camera,folder+"/calibration.png",new Vector3(-7,1.7f,64),new Vector3(-9,1.4f,66));
            Debug.Log("[Waterline] SIGN CAPTURE " + phase + " complete.");
        }
    }
}
