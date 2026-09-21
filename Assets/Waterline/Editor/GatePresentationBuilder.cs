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
    // Surface detail only. The existing blocker remains the sole authority for collision and visibility.
    public static class GatePresentationBuilder
    {
        internal const string RootName="Return gate finish presentation";
        internal const string LeafName="Shutter leaf fittings";
        private const string Folder="Assets/Waterline/Generated/Gates";
        private static Material steel,enamel,rubber,ochre;
        internal static HarborDoor[] Doors()=>Object.FindFirstObjectByType<HarborLayout>().doors.Where(d=>
            d.rule==HarborGateRule.DockRepaired || d.rule==HarborGateRule.DockReturn ||
            d.rule==HarborGateRule.WorkshopReturn || d.rule==HarborGateRule.ObservationReturn).ToArray();

        [MenuItem("Waterline/34 Refine return gate hardware")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();var baseline=Signature();var doors=Doors();
            Require(doors.Length==6,"Expected six main/return gates");
            var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject(RootName).transform;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            steel=Mat("Machined steel");enamel=Mat("Worn enamel");rubber=Mat("Recess and rubber");ochre=Mat("Drain ochre");
            foreach(var d in doors)
            {
                var gate=d.blocker;Require(gate!=null,"Gate has no blocker");
                var previous=gate.transform.Find(LeafName);if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var bounds=gate.GetComponent<Collider>().bounds;
                var p=new Vector3(d.position.x,bounds.min.y,d.position.z);
                var rotation=Quaternion.Euler(0,d.vertical?90:0,0);
                var mount=new GameObject(d.from+" to "+d.to).transform;mount.SetParent(root,false);mount.SetPositionAndRotation(p,rotation);
                var leaf=new GameObject(LeafName).transform;leaf.SetPositionAndRotation(p,rotation);leaf.SetParent(gate.transform,true);
                float height=bounds.size.y;float width=d.vertical?bounds.size.z:bounds.size.x;
                // Keep all static hardware beyond the original clear aperture and above head height.
                foreach(float side in new[]{-1f,1f})
                {
                    float x=side*(width*.5f+.09f);
                    Box(mount,"Guide mounting channel",new Vector3(x,height*.5f,0),new Vector3(.13f,height,.26f),steel);
                    foreach(float face in new[]{-1f,1f})
                    {
                        Box(mount,"Guide exposed lip",new Vector3(x,height*.5f,face*.148f),new Vector3(.085f,height,.034f),enamel);
                        Box(mount,"Guide wear strip",new Vector3(x-side*.032f,height*.5f,face*.17f),new Vector3(.016f,height-.06f,.016f),rubber);
                        foreach(float y in new[]{.18f,.75f,1.35f,2.05f})
                            Box(mount,"Guide anchor",new Vector3(x,y,face*.174f),new Vector3(.045f,.045f,.025f),steel);
                        for(int j=0;j<4;j++)
                            Box(mount,"Guide hazard marking",new Vector3(x,.26f+j*.16f,face*.17f),new Vector3(.079f,.065f,.012f),ochre);
                    }
                    Box(mount,"Guide foot",new Vector3(x,.055f,0),new Vector3(.16f,.11f,.28f),rubber);
                }
                // Head box explains where the curtain goes; the signs remain readable on the original lintel.
                Box(mount,"Roller cassette",new Vector3(0,height+.15f,0),new Vector3(width+.28f,.30f,.36f),steel);
                foreach(float face in new[]{-1f,1f})
                {
                    Box(mount,"Cassette removable cover",new Vector3(0,height+.16f,face*.186f),new Vector3(width+.16f,.20f,.016f),enamel);
                    Box(mount,"Cassette lower shadow seam",new Vector3(0,height+.017f,face*.17f),new Vector3(width,.024f,.025f),rubber);
                    foreach(float x in new[]{-width*.43f,width*.43f})
                        Box(mount,"Cassette fastener",new Vector3(x,height+.16f,face*.20f),new Vector3(.04f,.04f,.018f),steel);
                    // Full-width ribbed curtain on both faces. Everything is parented to the original moving/hiding leaf.
                    for(int j=0;j<12;j++)
                    {
                        float y=(j+.5f)*height/12;
                        Box(leaf,"Rolled steel slat",new Vector3(0,y,face*.112f),new Vector3(width-.035f,height/12-.012f,.022f),enamel);
                        Box(leaf,"Slat rolled edge",new Vector3(0,y-height/24+.018f,face*.133f),new Vector3(width-.045f,.025f,.025f),steel);
                    }
                    Box(leaf,"Curtain bottom rail",new Vector3(0,.075f,face*.137f),new Vector3(width-.025f,.13f,.046f),steel);
                    Box(leaf,"Curtain pull recess",new Vector3(0,.86f,face*.133f),new Vector3(.46f,.16f,.025f),rubber);
                    foreach(float x in new[]{-.16f,.16f})
                        Box(leaf,"Pull handle standoff",new Vector3(x,.86f,face*.175f),new Vector3(.04f,.085f,.09f),steel);
                    Box(leaf,"Pull handle grip",new Vector3(0,.86f,face*.215f),new Vector3(.36f,.04f,.045f),steel);
                    Box(leaf,"Curtain safety strip",new Vector3(0,.24f,face*.139f),new Vector3(width-.06f,.055f,.012f),ochre);
                }
                Combine(leaf,"Leaf "+d.from+" "+d.to);
            }
            Combine(root,"Static gate hardware");
            Require(Signature()==baseline,"Gate art changed collision, interactions, lights or topology");
            Require(root.GetComponentsInChildren<Collider>(true).Length==0,"Static fittings contain collision");
            TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] GATE ART PASS: six shutters; identical physics/interaction/light/layout; leaf hardware follows original blockers.");
            Capture("after");
        }
        private static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/"+n+".mat") ?? throw new InvalidOperationException(n);
        private static void Box(Transform parent,string name,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        private static void Combine(Transform parent,string name)
        {
            var parts=parent.GetComponentsInChildren<MeshRenderer>(true);
            foreach(var group in parts.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh{name=name+" "+group.Key.name};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
                string path=Folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var r in parts)Object.DestroyImmediate(r.gameObject);
        }
        private static string Signature()
        {
            var physics=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.gameObject.activeSelf+x.transform.localToWorldMatrix.ToString("F5"));
            var logic=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x is DockInteractable || x is AnnexInteractable || x is HarborDoorMarker || x is HarborLayout || x is DockSession || x is TidalRoutePlanner).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.transform.localToWorldMatrix.ToString("F5"));
            var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x));
            return string.Join("\n",physics.Concat(logic).Concat(lights));
        }
        public static void Audit()
        {
            TidalRouteBuilder.Open();foreach(var d in Doors())Debug.Log("[GATE AUDIT] "+d.from+" -> "+d.to+" "+d.rule+" bounds="+d.blocker.GetComponent<Renderer>().bounds);
            Capture("before");
        }
        internal static void Capture(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;string folder="TestResults/GateFinish/"+phase;Directory.CreateDirectory(folder);
            foreach(var d in Doors())
            {
                var basis=Quaternion.Euler(0,d.vertical?90:0,0);var floor=new Vector3(d.position.x,0,d.position.z);
                BoatPresentationBuilder.Shot(camera,folder+"/"+d.from+"-"+d.to+".png",floor+basis*new Vector3(.5f,1.65f,-3.3f),floor+new Vector3(0,1.55f,0));
            }
            Debug.Log("[Waterline] GATE CAPTURE "+phase+" complete.");
        }
        internal static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    }

    [InitializeOnLoad] public static class GateVisualCheck
    {
        private const string Key="Waterline.GateVisual";
        private static double next;
        static GateVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;}}
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var h=Object.FindFirstObjectByType<HarborLayout>();h.enabled=false;h.dock.Paused=true;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                foreach(var g in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){g.enabled=false;g.body.gameObject.SetActive(false);}
                var doors=GatePresentationBuilder.Doors();GatePresentationBuilder.Require(doors.Length==6,"Missing target gates");
                h.Apply();Physics.SyncTransforms();
                foreach(var d in doors)
                {
                    var leaf=d.blocker.transform.Find(GatePresentationBuilder.LeafName);
                    GatePresentationBuilder.Require(leaf!=null && leaf.gameObject.activeInHierarchy && leaf.GetComponentsInChildren<Collider>(true).Length==0,"Closed curtain missing or extra collision");
                    var n=d.vertical?Vector3.left:Vector3.back;var point=d.blocker.GetComponent<Collider>().bounds.center;
                    player.view.transform.position=point+n*1.65f;
                    GatePresentationBuilder.Require(player.RaycastInteraction(-n,out var hit) && hit.collider.GetComponent<HarborDoorMarker>()==d.blocker.GetComponent<HarborDoorMarker>(),"Gate description ray occluded");
                }
                GatePresentationBuilder.Capture("runtime-closed");
                GatePresentationBuilder.Require(h.dock.State.TryApply(DockAction.TakePin,out _) && h.dock.State.TryApply(DockAction.InstallPin,out _),"Cannot stage pump repair");
                foreach(var action in new[]{AnnexAction.OpenShortcut,AnnexAction.OpenWorkshopLoop,AnnexAction.OpenObservationLoop})GatePresentationBuilder.Require(h.annex.State.TryApply(action,out _),"Cannot stage return latch");
                h.Apply();Physics.SyncTransforms();
                foreach(var d in doors)
                {
                    var leaf=d.blocker.transform.Find(GatePresentationBuilder.LeafName);
                    GatePresentationBuilder.Require(!d.blocker.activeSelf && !leaf.gameObject.activeInHierarchy,"Open door left floating leaf details");
                    var normal=d.vertical?Vector3.right:Vector3.forward;var midpoint=new Vector3(d.position.x,1.05f,d.position.z);
                    // Check the player's width/headroom through the complete opening, not just a central ray.
                    for(int i=0;i<=8;i++)
                    {
                        var p=midpoint+normal*(-.8f+i*.2f);
                        GatePresentationBuilder.Require(!Physics.CheckCapsule(p+Vector3.down*.55f,p+Vector3.up*.55f,.28f,~0,QueryTriggerInteraction.Ignore),"Open gate passage blocked: "+d.from+"/"+d.to);
                    }
                }
                GatePresentationBuilder.Capture("runtime-open");
                // A restored closed state must bring every child fitting back with its original leaf.
                h.annex.State.ShortcutOpen=false;h.annex.State.WorkshopLoopOpen=false;h.annex.State.ObservationLoopOpen=false;h.Apply();
                foreach(var d in doors.Where(d=>d.rule!=HarborGateRule.DockRepaired))GatePresentationBuilder.Require(d.blocker.transform.Find(GatePresentationBuilder.LeafName).gameObject.activeInHierarchy,"Restored gate fittings missing");
                Debug.Log("[Waterline] GATE VISUAL PASS: six prompt rays; six clear open passages (54 capsule samples); five restored returns; closed/open captures. No gameplay or performance claim.");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
    }
}
