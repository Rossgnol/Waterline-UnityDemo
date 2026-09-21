using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static class SignPlacementAudit
    {
        private const string Folder = "TestResults/SignPlacement";
        [Serializable] private sealed class LabelInfo
        {
            public string text; public Vector3 position, size; public float yaw;
        }
        [Serializable] private sealed class Candidate
        {
            public string kind, a, b, image;
            public Vector3 position;
        }
        [Serializable] private sealed class Report
        {
            public string method = "Saved Day11 enabled active TextMesh bounds. Same-facing near-plane overlap plus short front rays against physical geometry. Candidates require screenshot review; not a complete visibility or readability proof.";
            public List<LabelInfo> labels = new List<LabelInfo>();
            public List<Candidate> candidates = new List<Candidate>();
        }
        public static void Run()
        {
            TidalRouteBuilder.Open(); Directory.CreateDirectory(Folder);
            var labels = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.GetComponent<Renderer>().enabled && !string.IsNullOrWhiteSpace(t.text)).OrderBy(t=>t.transform.position.z).ToArray();
            var report = new Report();
            foreach(var t in labels)
            {
                var bounds = t.GetComponent<Renderer>().bounds;
                report.labels.Add(new LabelInfo { text=t.text, position=t.transform.position, size=bounds.size, yaw=t.transform.eulerAngles.y });
            }
            for(int i=0;i<labels.Length;i++) for(int j=i+1;j<labels.Length;j++)
            {
                var a=labels[i];var b=labels[j];
                if(Vector3.Dot(a.transform.forward,b.transform.forward)<.99f) continue;
                if(Mathf.Abs(Vector3.Dot(a.transform.forward,a.transform.position-b.transform.position))>.3f) continue;
                var ab=a.GetComponent<Renderer>().bounds;var bb=b.GetComponent<Renderer>().bounds;
                float y=Mathf.Min(ab.max.y,bb.max.y)-Mathf.Max(ab.min.y,bb.min.y);
                bool acrossX=Mathf.Abs(a.transform.forward.z)>.9f;
                float across=acrossX ? Mathf.Min(ab.max.x,bb.max.x)-Mathf.Max(ab.min.x,bb.min.x) : Mathf.Min(ab.max.z,bb.max.z)-Mathf.Max(ab.min.z,bb.min.z);
                if(y<.015f || across<.03f) continue;
                Add(report,a,b.text,"overlap");
            }
            Physics.SyncTransforms();
            foreach(var t in labels)
            {
                var b=t.GetComponent<Renderer>().bounds;
                // TextMesh fronts face local -Z. Ignore support behind the letters.
                var front=-t.transform.forward;
                var origin=b.center+front*.5f;
                var hits=Physics.RaycastAll(origin,-front,.495f,~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(t.transform)).OrderBy(hit=>hit.distance).ToArray();
                if(hits.Length>0) Add(report,t,hits[0].collider.name,"front-collider");
            }
            File.WriteAllText(Folder+"/audit.json",JsonUtility.ToJson(report,true));
            Debug.Log("[Waterline] SIGN PLACEMENT AUDIT: labels="+labels.Length+" candidates="+report.candidates.Count+"; requires visual review.");
        }
        public static void Inspect()
        {
            TidalRouteBuilder.Open();
            foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.text=="UNLOCK IN EAST ROOM" || t.text=="OPTIONAL / SUPPLIES" || t.text=="DRAIN SERVICE"))
                Debug.Log("[SIGN DETAIL] "+t.text+" p="+t.transform.position.ToString("F4")+" b="+t.GetComponent<Renderer>().bounds+" rotation="+t.transform.eulerAngles+" parent="+t.transform.parent.name);
            foreach(var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Lower portal lintel" || c.name.StartsWith("Service shutter")))
                Debug.Log("[SIGN SUPPORT] "+c.name+" b="+c.bounds+" p="+c.transform.position+" size="+c.transform.lossyScale);
        }
        private static void Add(Report report,TextMesh label,string other,string kind)
        {
            string file="candidate-"+report.candidates.Count.ToString("D2")+".png";
            var p=label.GetComponent<Renderer>().bounds.center;
            var from=p-label.transform.forward*3;from.y=p.y<-.6f?-1.85f:1.65f;
            BoatPresentationBuilder.Shot(Object.FindFirstObjectByType<FirstPersonController>().view,Folder+"/"+file,from,p);
            report.candidates.Add(new Candidate{kind=kind,a=label.text,b=other,position=p,image=file});
            Debug.Log("[SIGN CANDIDATE] "+file+" "+kind+" "+label.text+" / "+other+" p="+p);
        }
    }
}
