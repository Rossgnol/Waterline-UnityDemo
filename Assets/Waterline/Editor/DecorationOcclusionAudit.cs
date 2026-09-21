using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // Visual triangles, not physics: catches non-colliding pipes and combined fittings.
    public static class DecorationOcclusionAudit
    {
        private const string Folder="TestResults/DecorationOcclusion";
        private sealed class Geometry { public string name;public Bounds bounds;public Vector3[] vertices;public int[] triangles; }
        [Serializable] private sealed class Candidate {public string text,obstruction,image;public int blockedSamples;public Vector3 position;}
        [Serializable] private sealed class Report
        {
            public string method="Saved Day11: five short rays toward each enabled TextMesh through actual triangles of enabled non-colliding MeshRenderers. Candidate screen captures require review. Not full glyph coverage, all viewing angles or dynamic-state proof.";
            public int labels,geometry,samples;public List<Candidate> candidates=new List<Candidate>();public List<string> records=new List<string>();
        }
        [Serializable] private sealed class Approach {public string action;public Vector3 standingPosition;}
        [Serializable] private sealed class Access {public Approach[] approaches;}
        public static void Run()
        {
            TidalRouteBuilder.Open();Directory.CreateDirectory(Folder);
            var geometry=new List<Geometry>();
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled && r.GetComponent<TextMesh>()==null && r.GetComponent<Collider>()==null))
            {
                var mf=r.GetComponent<MeshFilter>();if(mf==null || mf.sharedMesh==null)continue;
                var mesh=mf.sharedMesh;var matrix=r.transform.localToWorldMatrix;
                geometry.Add(new Geometry{name=r.name,bounds=r.bounds,vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(),triangles=mesh.triangles});
            }
            var report=new Report{geometry=geometry.Count};
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.GetComponent<Renderer>().enabled && !string.IsNullOrWhiteSpace(t.text)))
            {
                report.labels++;var r=t.GetComponent<Renderer>();var local=r.localBounds;
                int blocked=0;var blockers=new HashSet<string>();
                foreach(float fraction in new[]{-.4f,-.2f,0,.2f,.4f})
                {
                    report.samples++;var point=t.transform.TransformPoint(local.center+Vector3.right*local.size.x*fraction);
                    var ray=new Ray(point-t.transform.forward*.75f,t.transform.forward);
                    foreach(var g in geometry)
                    {
                        if(!g.bounds.IntersectRay(ray,out float d) || d>.748f)continue;
                        if(!Hits(ray,g))continue;blocked++;blockers.Add(g.name);break;
                    }
                }
                if(blocked==0)continue;
                string file="candidate-"+report.candidates.Count.ToString("D2")+".png";
                var center=r.bounds.center;var from=center-t.transform.forward*2.5f;from.y=center.y<-.6f?-1.85f:1.65f;
                BoatPresentationBuilder.Shot(camera,Folder+"/"+file,from,center);
                report.candidates.Add(new Candidate{text=t.text,obstruction=string.Join("; ",blockers),blockedSamples=blocked,position=center,image=file});
                Debug.Log("[DECORATION CANDIDATE] "+file+" "+blocked+"/5 "+t.text+" by "+string.Join("; ",blockers));
            }
            var access=JsonUtility.FromJson<Access>(File.ReadAllText("Docs/MapEvidence/Day11MapIntegrity/access.json"));
            foreach(var item in Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).Where(a=>a.action.ToString().StartsWith("Read")))
            {
                string action=item.action.ToString();var approach=access.approaches.FirstOrDefault(a=>a.action==action);
                if(approach==null)throw new InvalidOperationException("Missing known approach: "+action);
                var target=item.GetComponent<Collider>().bounds.center;
                BoatPresentationBuilder.Shot(camera,Folder+"/record-"+action+".png",approach.standingPosition+Vector3.up*1.62f,target);
                report.records.Add(action);
            }
            File.WriteAllText(Folder+"/audit.json",JsonUtility.ToJson(report,true));
            Debug.Log("[Waterline] DECORATION AUDIT COMPLETE: labels="+report.labels+" geometry="+report.geometry+" rays="+report.samples+" candidates="+report.candidates.Count+" recordViews="+report.records.Count);
        }
        private static bool Hits(Ray ray,Geometry g)
        {
            for(int i=0;i<g.triangles.Length;i+=3)
            {
                var a=g.vertices[g.triangles[i]];var e1=g.vertices[g.triangles[i+1]]-a;var e2=g.vertices[g.triangles[i+2]]-a;
                var p=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<.0000001f)continue;
                float inv=1/det;var delta=ray.origin-a;float u=Vector3.Dot(delta,p)*inv;if(u<0 || u>1)continue;
                var q=Vector3.Cross(delta,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0 || u+v>1)continue;
                float distance=Vector3.Dot(e2,q)*inv;if(distance>.0001f && distance<.748f)return true;
            }
            return false;
        }
    }
}
