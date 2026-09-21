using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class WallSurfaceRepair
    {
        internal static MeshRenderer[] Walls()=>Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name=="Harbor wall" && r.gameObject.activeInHierarchy).ToArray();
        private static bool Vertical(Bounds b)=>b.size.x<b.size.z;
        private static float AlongMin(Bounds b)=>Vertical(b)?b.min.z:b.min.x;
        private static float AlongMax(Bounds b)=>Vertical(b)?b.max.z:b.max.x;
        private static bool SamePlane(Bounds a,Bounds b)=>Vertical(a)==Vertical(b) && Mathf.Abs((Vertical(a)?a.center.x:a.center.z)-(Vertical(b)?b.center.x:b.center.z))<.0001f && Mathf.Abs(a.min.y-b.min.y)<.0001f && Mathf.Abs(a.max.y-b.max.y)<.0001f && Mathf.Abs((Vertical(a)?a.size.x:a.size.z)-(Vertical(b)?b.size.x:b.size.z))<.0001f;
        private static List<MeshRenderer> Contained(MeshRenderer[] walls)
        {
            var keep=new List<MeshRenderer>();var redundant=new List<MeshRenderer>();
            foreach(var wall in walls.Where(w=>w.enabled).OrderByDescending(w=>AlongMax(w.bounds)-AlongMin(w.bounds)))
            {
                if(keep.Any(k=>SamePlane(k.bounds,wall.bounds) && AlongMin(k.bounds)<=AlongMin(wall.bounds)+.0001f && AlongMax(k.bounds)>=AlongMax(wall.bounds)-.0001f))redundant.Add(wall);else keep.Add(wall);
            }
            return redundant;
        }
        private static bool Covers(MeshRenderer outer,MeshRenderer inner)=>SamePlane(outer.bounds,inner.bounds) && AlongMin(outer.bounds)<=AlongMin(inner.bounds)+.0001f && AlongMax(outer.bounds)>=AlongMax(inner.bounds)-.0001f;
        [MenuItem("Waterline/36 Remove redundant wall rendering")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();var before=Signature();var walls=Walls();
            Require(walls.Length==102,"Unexpected wall set; inspect before repairing");
            var duplicates=Contained(walls);Require(duplicates.Count==34 || duplicates.Count==0,"Unexpected number of fully covered walls");
            foreach(var wall in duplicates)
            {
                var cover=walls.First(w=>w!=wall && w.enabled && !duplicates.Contains(w) && Covers(w,wall));
                // Shared wall finishes must agree except for the world-size-dependent UV repetition.
                var a=wall.sharedMaterial;var b=cover.sharedMaterial;
                Require(a.shader==b.shader && a.GetTexture("_BaseMap")==b.GetTexture("_BaseMap") && a.GetColor("_BaseColor")==b.GetColor("_BaseColor") && Mathf.Approximately(a.GetFloat("_Smoothness"),b.GetFloat("_Smoothness")) && Mathf.Approximately(a.GetFloat("_Metallic"),b.GetFloat("_Metallic")),"Nonmatching wall finish would be lost");
                wall.enabled=false;EditorUtility.SetDirty(wall);
            }
            ValidateCoverage();Require(before==Signature(),"Repair changed collision, gameplay, lighting or materials");
            TidalRouteBuilder.Validate();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] WALL ART PASS: 34 fully covered renderers disabled; 68 visible walls; original colliders, gameplay, lighting and materials unchanged.");
            CaptureOpen("after");
        }
        internal static void ValidateCoverage()
        {
            var walls=Walls();var visible=walls.Where(w=>w.enabled).ToArray();var hidden=walls.Where(w=>!w.enabled).ToArray();
            Require(walls.Length==102 && visible.Length==68 && hidden.Length==34,"Unexpected saved wall visibility counts");
            foreach(var wall in hidden)Require(visible.Any(v=>Covers(v,wall)),"Disabled wall is not fully covered: "+wall.bounds);
            for(int i=0;i<visible.Length;i++)for(int j=i+1;j<visible.Length;j++)
            {
                var a=visible[i].bounds;var b=visible[j].bounds;if(!SamePlane(a,b))continue;
                Require(Mathf.Min(AlongMax(a),AlongMax(b))-Mathf.Max(AlongMin(a),AlongMin(b))<.001f,"Coplanar wall rendering still overlaps");
            }
            Require(walls.All(w=>w.GetComponent<Collider>()!=null && w.GetComponent<Collider>().enabled),"Original wall collider missing");
        }
        private static string Signature()
        {
            var colliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.gameObject.activeSelf+x.transform.localToWorldMatrix.ToString("F5"));
            var logic=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x is DockSession || x is DockInteractable || x is AnnexInteractable || x is HarborDoorMarker || x is HarborLayout || x is TidalRoutePlanner).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.transform.localToWorldMatrix.ToString("F5"));
            var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x));
            var materials=Walls().Select(w=>w.sharedMaterial).Distinct().OrderBy(m=>m.GetInstanceID()).Select(m=>EditorJsonUtility.ToJson(m));
            return string.Join("\n",colliders.Concat(logic).Concat(lights).Concat(materials));
        }
        internal static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        internal static void CaptureOpen(string phase)
        {
            var gates=Object.FindFirstObjectByType<HarborLayout>().doors.Where(d=>d.blocker!=null).Select(d=>d.blocker).Distinct().ToArray();
            var states=gates.Select(g=>g.activeSelf).ToArray();
            try{foreach(var gate in gates)gate.SetActive(false);Capture(phase);}
            finally{for(int i=0;i<gates.Length;i++)gates[i].SetActive(states[i]);}
        }
        public static void Audit()
        {
            TidalRouteBuilder.Open();var walls=Walls().Where(w=>w.enabled).ToArray();int pairs=0;
            for(int i=0;i<walls.Length;i++)for(int j=i+1;j<walls.Length;j++)
            {
                var a=walls[i].bounds;var b=walls[j].bounds;
                if(!SamePlane(a,b))continue;float overlap=Mathf.Min(AlongMax(a),AlongMax(b))-Mathf.Max(AlongMin(a),AlongMin(b));if(overlap<.001f)continue;pairs++;
                Debug.Log("[WALL OVERLAP] a="+a+" b="+b+" span="+overlap+" materials="+walls[i].sharedMaterial.name+" / "+walls[j].sharedMaterial.name);
            }
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var shadows=lights.Select(l=>l.shadows).ToArray();var duplicates=Contained(walls);
            Debug.Log("[WALL AUDIT] walls="+walls.Length+" coplanarPairs="+pairs+" fullyContained="+duplicates.Count);
            // Temporary diagnostic states only; do not save either geometry or lighting.
            foreach(var d in Object.FindFirstObjectByType<HarborLayout>().doors)if(d.blocker!=null)d.blocker.SetActive(false);
            try
            {
                Capture("before");foreach(var light in lights)light.shadows=LightShadows.None;Capture("no-shadows");
                for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];
                foreach(var r in duplicates)r.enabled=false;Capture("contained-preview");
            }
            finally{for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];foreach(var r in duplicates)r.enabled=true;}
            Debug.Log("[Waterline] WALL DIAGNOSTIC COMPLETE: no scene or pipeline settings saved.");
        }
        internal static void Capture(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;string folder="TestResults/WallFinish/"+phase;Directory.CreateDirectory(folder);
            BoatPresentationBuilder.Shot(camera,folder+"/return-corridor.png",new Vector3(2.7f,1.65f,29.5f),new Vector3(6,1.55f,30));
            BoatPresentationBuilder.Shot(camera,folder+"/east-corridor.png",new Vector3(10,1.65f,29),new Vector3(14,1.7f,33));
            BoatPresentationBuilder.Shot(camera,folder+"/power.png",new Vector3(-8,1.7f,30),new Vector3(-12,1.3f,27));
            BoatPresentationBuilder.Shot(camera,folder+"/fuse-bench.png",new Vector3(-25,1.7f,27),new Vector3(-27,1.05f,29));
            Debug.Log("[Waterline] WALL CAPTURE "+phase+" complete.");
        }
    }
    [InitializeOnLoad] public static class WallSurfaceVisualCheck
    {
        private const string Key="Waterline.WallSurfaceVisual";
        private static double next;
        static WallSurfaceVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();WallSurfaceRepair.ValidateCoverage();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;}}
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var h=Object.FindFirstObjectByType<HarborLayout>();h.enabled=false;h.dock.Paused=true;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                foreach(var g in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){g.enabled=false;g.body.gameObject.SetActive(false);}
                WallSurfaceRepair.ValidateCoverage();
                // The hidden renderers must keep their solid barriers; sample both faces of all 34 redundant walls.
                Physics.SyncTransforms();int rays=0;
                foreach(var wall in WallSurfaceRepair.Walls().Where(w=>!w.enabled))
                {
                    var b=wall.bounds;var normal=b.size.x<b.size.z?Vector3.right:Vector3.forward;
                    foreach(float side in new[]{-1f,1f})
                    {
                        var origin=b.center+normal*side*.7f;var collider=wall.GetComponent<Collider>();
                        WallSurfaceRepair.Require(collider.Raycast(new Ray(origin,-normal*side),out var hit,1.4f),"Hidden renderer lost its original solid wall");rays++;
                    }
                }
                WallSurfaceRepair.CaptureOpen("runtime");
                string folder="TestResults/WallFinish/runtime";
                var gates=h.doors.Where(d=>d.blocker!=null).Select(d=>d.blocker).Distinct().ToArray();var states=gates.Select(g=>g.activeSelf).ToArray();
                try
                {
                    foreach(var gate in gates)gate.SetActive(false);
                    BoatPresentationBuilder.Shot(player.view,folder+"/return-offset.png",new Vector3(3.1f,1.65f,29.7f),new Vector3(6,1.55f,30));
                    BoatPresentationBuilder.Shot(player.view,folder+"/east-offset.png",new Vector3(11,1.65f,29),new Vector3(14,1.7f,33));
                }
                finally{for(int i=0;i<gates.Length;i++)gates[i].SetActive(states[i]);}
                Debug.Log("[Waterline] WALL VISUAL PASS: 102 original walls/colliders; 68 visible, 34 fully covered; zero coplanar overlaps; "+rays+" retained collider rays; fixed-view and offset captures. Not gameplay or performance evidence.");Finish(0);
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
    }
}
