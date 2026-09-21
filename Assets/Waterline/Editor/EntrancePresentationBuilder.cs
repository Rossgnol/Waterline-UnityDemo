using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class EntrancePresentationBuilder
    {
        internal const string RootName="Entrance plaques and latch mounting";
        private const string Folder="Assets/Waterline/Generated/EntranceFinish";
        private static Transform root;
        private static Material steel,dark,enamel,blue,amber;
        internal static bool IsRoomLabel(TextMesh t)=>t.text=="03 CONTROL / PUMP" || t.text=="02  WINCH";
        internal static TextMesh[] Labels()=>Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(IsRoomLabel).ToArray();
        internal static AnnexInteractable Latch()=>Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==AnnexAction.OpenWorkshopLoop);
        [MenuItem("Waterline/35 Mount room plaques and workshop latch")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();var signature=Signature();
            var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            root=new GameObject(RootName).transform;Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            steel=Mat("Machined steel");dark=Mat("Recess and rubber");enamel=Mat("Worn enamel");blue=Mat("Inlet blue");amber=Mat("Drain ochre");
            var labels=Labels();Require(labels.Length==4,"Expected four room label faces");
            var obsolete=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>
                (r.name=="Console sign backing" || r.name=="Console sign stem") && Mathf.Abs(Mathf.Abs(r.transform.position.x)-11.2f)<.02f && Mathf.Abs(r.transform.position.z-3.992f)<.025f).ToArray();
            Require(obsolete.Length==4,"Unexpected old entrance backing/stem count");foreach(var r in obsolete)r.enabled=false;
            foreach(var text in labels)
            {
                float x=Mathf.Sign(text.transform.position.x)*11.2f;bool back=text.name.StartsWith("Mounted sign /");
                float face=back?1:-1;float z=back?4.565f:4.235f;var p=new Vector3(x,2.88f,z);
                Box("Room plaque surround",p,new Vector3(2.02f,.40f,.055f),steel);
                Box("Room plaque face",p+Vector3.forward*face*.035f,new Vector3(1.95f,.335f,.015f),dark);
                Box("Room colour strip",p+new Vector3(-.925f,0,face*.047f),new Vector3(.04f,.28f,.012f),x<0?blue:amber);
                foreach(float dx in new[]{-.82f,.82f})
                {
                    // Brackets meet the existing chamber lintel at z=4.4; no hanging rod below the sign.
                    Box("Lintel plaque standoff",p+new Vector3(dx,0,-face*.035f),new Vector3(.13f,.22f,.06f),steel);
                    foreach(float dy in new[]{-.145f,.145f})Box("Plaque fastener",p+new Vector3(dx,dy,face*.048f),new Vector3(.025f,.025f,.018f),steel);
                }
                text.transform.SetPositionAndRotation(p+Vector3.forward*face*.055f,Quaternion.Euler(0,back?180:0,0));
                text.characterSize=.024f;text.GetComponent<WorldLabel>().Refresh();
                var r=text.GetComponent<Renderer>();if(r.bounds.size.x>1.78f){text.characterSize*=1.78f/r.bounds.size.x;text.GetComponent<WorldLabel>().Refresh();}
                Require(r.bounds.size.x<=1.79f && r.bounds.min.y>2.65f,"Room plaque text exceeds its face");
            }
            MountLatch();Combine();
            Require(root.GetComponentsInChildren<Collider>(true).Length==0,"Added decoration collision");
            Require(signature==Signature(),"Entrance art changed physics, interactions, layout or lighting");
            TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] ENTRANCE ART PASS: four lintel-mounted plaque faces; grounded latch stand; unchanged physics, interactions, lights and topology.");
            Capture("after");
        }
        private static void MountLatch()
        {
            var item=Latch();var b=item.GetComponent<Renderer>().bounds;var p=b.center;
            // The original click target stays put; the new base explains its position in the room.
            Box("Latch floor foot",new Vector3(p.x,.035f,p.z),new Vector3(.40f,.07f,.38f),dark);
            Box("Latch pedestal post",new Vector3(p.x,b.min.y*.5f,p.z),new Vector3(.095f,b.min.y,.095f),steel);
            Box("Latch body saddle",new Vector3(p.x,b.min.y+.005f,p.z),new Vector3(.29f,.04f,.29f),steel);
            foreach(float dx in new[]{-.135f,.135f})foreach(float dz in new[]{-.12f,.12f})Box("Latch foot anchor",new Vector3(p.x+dx,.078f,p.z+dz),new Vector3(.032f,.024f,.032f),steel);
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Waterline/Generated/Consoles/Chamfered console housing.asset");Require(mesh!=null,"Missing existing chamfered housing mesh");
            item.GetComponent<MeshFilter>().sharedMesh=mesh;item.GetComponent<Renderer>().sharedMaterial=enamel;
            // Lever faces the workshop approach. Its outline is decorative, with no new interactive target.
            Box("Latch recessed control plate",p+new Vector3(-.154f,0,0),new Vector3(.016f,.44f,.235f),dark);
            Box("Latch lever pivot",p+new Vector3(-.173f,-.04f,0),new Vector3(.055f,.10f,.10f),steel);
            Box("Latch lever shaft",p+new Vector3(-.21f,.025f,0),new Vector3(.03f,.18f,.035f),steel);
            Box("Latch insulated lever grip",p+new Vector3(-.216f,.12f,0),new Vector3(.055f,.075f,.07f),amber);
            Box("Latch lower rating plate",p+new Vector3(-.169f,-.17f,0),new Vector3(.014f,.035f,.125f),steel);
            foreach(float y in new[]{-.18f,.18f})foreach(float z in new[]{-.09f,.09f})Box("Latch face screw",p+new Vector3(-.17f,y,z),new Vector3(.018f,.024f,.024f),steel);
            Box("Latch side access cover",p+new Vector3(0,0,-.155f),new Vector3(.21f,.42f,.018f),steel);
            for(int i=0;i<3;i++)Box("Latch side ventilation",p+new Vector3(0,-.12f+i*.055f,-.17f),new Vector3(.13f,.016f,.012f),dark);
        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/"+name+".mat") ?? throw new InvalidOperationException(name);
        private static void Box(string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        private static void Combine()
        {
            var parts=root.GetComponentsInChildren<MeshRenderer>();
            foreach(var group in parts.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh{name="Entrance fittings "+group.Key.name};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
                var path=Folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var r in parts)Object.DestroyImmediate(r.gameObject);
        }
        private static string Signature()
        {
            var c=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.gameObject.activeSelf+x.transform.localToWorldMatrix.ToString("F5"));
            var a=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x is DockSession || x is DockInteractable || x is AnnexInteractable || x is HarborLayout || x is TidalRoutePlanner || x is HarborDoorMarker).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.transform.localToWorldMatrix.ToString("F5"));
            var l=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x));return string.Join("\n",c.Concat(a).Concat(l));
        }
        internal static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static void Audit()
        {
            TidalRouteBuilder.Open();
            foreach(var t in Labels())
            {
                Debug.Log("[ENTRANCE LABEL] "+t.name+" text="+t.text+" at="+t.transform.position.ToString("F3")+" rotation="+t.transform.eulerAngles+" bounds="+t.GetComponent<Renderer>().bounds);
                foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.GetComponent<TextMesh>()==null && Vector3.Distance(r.bounds.ClosestPoint(t.transform.position),t.transform.position)<.35f))
                    Debug.Log("[ENTRANCE NEIGHBOR] "+r.name+" p="+r.transform.position.ToString("F3")+" b="+r.bounds+" enabled="+r.enabled);
            }
            var latch=Latch();Debug.Log("[LATCH AUDIT] "+latch.name+" at="+latch.transform.position+" b="+latch.GetComponent<Renderer>().bounds+" euler="+latch.transform.eulerAngles);
            Capture("before");
        }
        internal static void Capture(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;string folder="TestResults/EntranceFinish/"+phase;Directory.CreateDirectory(folder);
            foreach(float x in new[]{-11.2f,11.2f})
            {
                string side=x<0?"west":"east";
                BoatPresentationBuilder.Shot(camera,folder+"/"+side+"-approach.png",new Vector3(x+.5f,1.65f,2.7f),new Vector3(x,1.55f,6));
                BoatPresentationBuilder.Shot(camera,folder+"/"+side+"-room.png",new Vector3(x+.4f,1.65f,1.1f),new Vector3(x,2.35f,6));
                BoatPresentationBuilder.Shot(camera,folder+"/"+side+"-reverse.png",new Vector3(x+.1f,1.8f,5.7f),new Vector3(x,2.8f,4.4f));
            }
            var p=Latch().transform.position;
            BoatPresentationBuilder.Shot(camera,folder+"/workshop-latch.png",p+new Vector3(-2,0,-1.5f),p+Vector3.down*.35f);
            Debug.Log("[Waterline] ENTRANCE CAPTURE "+phase+" complete.");
        }
    }

    [InitializeOnLoad] public static class EntranceVisualCheck
    {
        private const string Key="Waterline.EntranceVisual";
        private static double next;
        static EntranceVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();EntrancePresentationBuilder.Capture("after");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;}}
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var h=Object.FindFirstObjectByType<HarborLayout>();h.enabled=false;h.dock.Paused=true;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                foreach(var guard in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){guard.enabled=false;guard.body.gameObject.SetActive(false);}
                var root=GameObject.Find(EntrancePresentationBuilder.RootName);EntrancePresentationBuilder.Require(root!=null && root.GetComponentsInChildren<Collider>(true).Length==0,"Bad entrance decoration root");
                var labels=EntrancePresentationBuilder.Labels();EntrancePresentationBuilder.Require(labels.Length==4,"Room label count");var positions=labels.Select(t=>t.transform.position).ToArray();
                foreach(var label in labels)EntrancePresentationBuilder.Require(label.GetComponent<Renderer>().bounds.min.y>2.65f && label.GetComponent<Renderer>().bounds.size.x<1.8f,"Low or overflowing plaque");
                var item=EntrancePresentationBuilder.Latch();
                foreach(var direction in new[]{Vector3.left,Vector3.back})
                {
                    player.view.transform.position=item.transform.position+direction*1.65f;Physics.SyncTransforms();
                    EntrancePresentationBuilder.Require(player.RaycastInteraction(-direction,out var hit) && hit.collider.GetComponentInParent<AnnexInteractable>()==item,"Original latch target occluded");
                }
                EntrancePresentationBuilder.Capture("runtime-closed");
                h.dock.Paused=false;EntrancePresentationBuilder.Require(item.Use(),"Workshop latch rejected original interaction");h.dock.Paused=true;h.Apply();
                foreach(var d in h.doors.Where(d=>d.rule==HarborGateRule.WorkshopReturn))EntrancePresentationBuilder.Require(!d.blocker.activeSelf,"Workshop door failed to open");
                EntrancePresentationBuilder.Require(item.gameObject.activeInHierarchy && root.activeSelf,"Latch disappeared with the doors");
                EntrancePresentationBuilder.Require(h.dock.State.TryApply(DockAction.TakePin,out _) && h.dock.State.TryApply(DockAction.InstallPin,out _) && h.annex.State.TryApply(AnnexAction.OpenShortcut,out _),"Cannot stage open dock portals");h.Apply();
                Physics.SyncTransforms();foreach(float x in new[]{-11.2f,11.2f})for(int i=0;i<=10;i++)
                {
                    var p=new Vector3(x,1.0f,4f+i*.25f);EntrancePresentationBuilder.Require(!Physics.CheckCapsule(p+Vector3.down*.55f,p+Vector3.up*.55f,.28f,~0,QueryTriggerInteraction.Ignore),"Entrance passage blocked");
                }
                for(int i=0;i<labels.Length;i++)EntrancePresentationBuilder.Require(labels[i].gameObject.activeInHierarchy && labels[i].transform.position==positions[i],"Room plaque moved with gate state");
                EntrancePresentationBuilder.Capture("runtime-open");
                Debug.Log("[Waterline] ENTRANCE VISUAL PASS: four mounted labels; two original latch rays; actual Use opens both workshop gates; 22 entrance capsule samples; stable room plaques with open dock gates.");Finish(0);
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
    }
}
