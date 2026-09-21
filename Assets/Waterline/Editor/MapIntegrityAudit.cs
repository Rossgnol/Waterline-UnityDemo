using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class MapIntegrityAudit
    {
        private const string Key="Waterline.MapIntegrityAudit";
        private static double next;
        [Serializable] private sealed class Crossing
        {
            public string from,to,rule,status;public int direction;public Vector3 start,end,target;public string nearby;
        }
        [Serializable] private sealed class SurfaceOverlap
        {
            public string a,b;public Vector3 centerA,centerB;public float area,height;
        }
        [Serializable] private sealed class Report
        {
            public string method="Day11 dry state, all normal lock prerequisites staged open; actual CharacterController moves in both directions through same-level portal centres. Guards and input disabled. Cross-level and float links explicitly excluded; not a full gameplay test.";
            public List<Crossing> crossings=new List<Crossing>();
            public List<string> excluded=new List<string>();
            public List<SurfaceOverlap> coplanarFloors=new List<SurfaceOverlap>();
            public List<string> missingAssets=new List<string>();
        }
        static MapIntegrityAudit(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;}}
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var report=new Report();var h=Object.FindFirstObjectByType<HarborLayout>();h.enabled=false;h.dock.Paused=true;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;var cc=player.GetComponent<CharacterController>();
                h.dock.State.TryApply(DockAction.TakePin,out _);h.dock.State.TryApply(DockAction.InstallPin,out _);
                var s=h.annex.State;s.PowerRestored=true;s.HasSignalKey=true;s.ShortcutOpen=true;s.WorkshopLoopOpen=true;s.ObservationLoopOpen=true;s.ServiceLoopOpen=true;
                h.Apply();h.annex.enabled=false;
                foreach(var g in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){g.enabled=false;g.GetComponent<CharacterController>().enabled=false;g.body.gameObject.SetActive(false);}
                foreach(var d in h.doors)
                {
                    var a=h.Room(d.from);var b=h.Room(d.to);
                    if(a==null || b==null || a.level!=b.level || d.rule==HarborGateRule.FloatLink)
                    {report.excluded.Add(d.from+" -> "+d.to+" : cross-level or float link");continue;}
                    var normal=d.vertical?Vector3.right:Vector3.forward;float y=a.level<0?-3.46f:.04f;
                    foreach(int direction in new[]{-1,1})
                    {
                        var mid=new Vector3(d.position.x,y,d.position.z);var start=mid-normal*direction*1.05f;var target=mid+normal*direction*1.05f;
                        cc.enabled=false;cc.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
                        for(int i=0;i<90;i++)
                        {
                            var delta=target-cc.transform.position;delta.y=0;if(delta.magnitude<.05f)break;
                            cc.Move(Vector3.ClampMagnitude(delta,.075f)+Vector3.down*.025f);
                        }
                        var end=cc.transform.position;var flat=end-target;flat.y=0;
                        bool pass=flat.magnitude<.10f && Mathf.Abs(end.y-y)<.35f;
                        var nearby=pass?"":string.Join("; ",Physics.OverlapSphere(end+Vector3.up*.9f,.65f).Where(c=>c!=cc).Select(c=>c.name+" @ "+c.bounds.center));
                        report.crossings.Add(new Crossing{from=d.from,to=d.to,rule=d.rule.ToString(),direction=direction,start=start,end=end,target=target,status=pass?"pass":"inspect",nearby=nearby});
                        if(!pass)Debug.Log("[MAP CROSSING INSPECT] "+d.from+" -> "+d.to+" dir="+direction+" end="+end+" target="+target+" nearby="+nearby);
                    }
                }
                var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
                foreach(var r in renderers)
                {
                    var mf=r.GetComponent<MeshFilter>();if(mf!=null && mf.sharedMesh==null)report.missingAssets.Add("mesh: "+r.name);
                    if(r.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name=="Hidden/InternalErrorShader"))report.missingAssets.Add("material/shader: "+r.name);
                }
                var floors=renderers.Where(r=>r.GetComponent<BoxCollider>()!=null && r.bounds.size.y<1 && Mathf.Abs(Vector3.Dot(r.transform.up,Vector3.up))>.9999f && (r.name.ToLowerInvariant().Contains("floor") || r.name.ToLowerInvariant().Contains("walkway") || r.name.ToLowerInvariant().Contains("slab"))).ToArray();
                for(int i=0;i<floors.Length;i++)for(int j=i+1;j<floors.Length;j++)
                {
                    var a=floors[i].bounds;var b=floors[j].bounds;if(Mathf.Abs(a.max.y-b.max.y)>.0005f)continue;
                    float x=Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x),z=Mathf.Min(a.max.z,b.max.z)-Mathf.Max(a.min.z,b.min.z);
                    if(x<.03f || z<.03f)continue;
                    report.coplanarFloors.Add(new SurfaceOverlap{a=floors[i].name,b=floors[j].name,centerA=a.center,centerB=b.center,area=x*z,height=a.max.y});
                    Debug.Log("[MAP FLOOR OVERLAP] "+floors[i].name+" / "+floors[j].name+" area="+x*z+" top="+a.max.y+" centers="+a.center+" / "+b.center);
                }
                Directory.CreateDirectory("TestResults/MapIntegrity");File.WriteAllText("TestResults/MapIntegrity/audit.json",JsonUtility.ToJson(report,true));
                Debug.Log("[Waterline] MAP AUDIT COMPLETE: crossings="+report.crossings.Count+" inspect="+report.crossings.Count(c=>c.status!="pass")+" excluded="+report.excluded.Count+" floor overlaps="+report.coplanarFloors.Count+" missing assets="+report.missingAssets.Count+". Inspect candidates before calling them bugs.");Finish(0);
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
    }
}
