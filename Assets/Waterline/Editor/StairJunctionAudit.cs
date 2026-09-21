using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad]
    public static class StairJunctionAudit
    {
        private const string Key = "Waterline.StairJunctionAudit";
        private const string Folder = "TestResults/StairJunction";
        private static double next;
        [Serializable] private sealed class Segment
        {
            public string route, status, obstruction;
            public Vector3 start, target, end;
        }
        [Serializable] private sealed class Report
        {
            public string method = "Saved Day11 dry stairs. Actual character controller walking both directions including upper/lower corners. Frozen guards and player input. Separate fixed-camera visual inspection; scene not saved.";
            public List<Segment> segments = new List<Segment>();
        }
        static StairJunctionAudit() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run()
        {
            TidalRouteBuilder.Open();
            Directory.CreateDirectory(Folder);
            Capture();
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void Capture()
        {
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            var dock = player.session;
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "west" : "east";
                void Shot(string suffix, Vector3 from, Vector3 to) => BoatPresentationBuilder.Shot(player.view, Folder + "/" + name + suffix + ".png", dock.DockToWorld(from), dock.DockToWorld(to));
                Shot("-upper", new Vector3(side * 10, 1.65f, -4), new Vector3(side * 12.65f, .3f, -2.8f));
                Shot("-down", new Vector3(side * 12.65f, 1.65f, -3.8f), new Vector3(side * 12.65f, -2.4f, 3.6f));
                Shot("-up", new Vector3(side * 12.65f, -1.85f, 4.8f), new Vector3(side * 12.65f, .5f, -3));
                Shot("-lower", new Vector3(side * 10, -1.85f, 4.8f), new Vector3(side * 12.65f, -2.5f, 3.5f));
                Shot("-ceiling", new Vector3(side * 8, 1.65f, -4), new Vector3(side * 8, 3.3f, 1));
            }
            BoatPresentationBuilder.Shot(player.view, Folder + "/lower-junction.png", new Vector3(-10,-1.85f,9), new Vector3(-10,-.4f,16));
            BoatPresentationBuilder.Shot(player.view, Folder + "/north-ceiling.png", new Vector3(0,1.65f,25), new Vector3(0,3.2f,32));
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Stair") || r.name.ToLowerInvariant().Contains("ceiling")))
                Debug.Log("[JUNCTION GEOMETRY] " + r.name + " p=" + r.transform.position + " size=" + r.bounds.size + " euler=" + r.transform.eulerAngles);
        }
        public static void InspectSigns()
        {
            TidalRouteBuilder.Open(); Directory.CreateDirectory(Folder + "/signs-before");
            foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>Mathf.Abs(t.transform.position.x)<5 && t.transform.position.z>25 && t.transform.position.z<40))
                Debug.Log("[JUNCTION LABEL] " + t.text + " p=" + t.transform.position.ToString("F3") + " size=" + t.GetComponent<Renderer>().bounds.size + " yaw=" + t.transform.eulerAngles.y);
            var h = Object.FindFirstObjectByType<HarborLayout>();
            foreach(var d in h.doors.Where(d=>d.from=="pump_hall" || d.to=="pump_hall")) Debug.Log("[JUNCTION DOOR] " + d.from + " -> " + d.to + " p=" + d.position + " label=" + d.sign.text);
            CaptureSigns("signs-before");
        }
        internal static void CaptureSigns(string phase)
        {
            Directory.CreateDirectory(Folder + "/" + phase);
            var camera = Object.FindFirstObjectByType<FirstPersonController>().view;
            BoatPresentationBuilder.Shot(camera,Folder+"/"+phase+"/approach.png",new Vector3(0,1.65f,33),new Vector3(0,2.9f,38));
            BoatPresentationBuilder.Shot(camera,Folder+"/"+phase+"/offset.png",new Vector3(-2,1.65f,34.5f),new Vector3(0,2.9f,38));
            BoatPresentationBuilder.Shot(camera,Folder+"/"+phase+"/reverse.png",new Vector3(0,1.65f,42),new Vector3(0,2.9f,38));
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            next = EditorApplication.timeSinceStartup + 2; EditorApplication.update += Check;
        }
        private static void Check()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                var h = Object.FindFirstObjectByType<HarborLayout>(); h.enabled = false; h.dock.Paused = true;
                var player = Object.FindFirstObjectByType<FirstPersonController>(); player.enabled = false;
                var cc = player.GetComponent<CharacterController>();
                foreach(var g in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))
                { g.enabled = false; g.GetComponent<CharacterController>().enabled = false; g.body.gameObject.SetActive(false); }
                var report = new Report();
                foreach(int side in new[] { -1, 1 }) foreach(bool reverse in new[] { false, true })
                {
                    var points = new[] { new Vector3(side*10,.04f,-4), new Vector3(side*12.65f,.04f,-4), new Vector3(side*12.65f,-3.46f,4.8f), new Vector3(side*10,-3.46f,4.8f) }.Select(h.dock.DockToWorld).ToArray();
                    if(reverse) Array.Reverse(points);
                    cc.enabled = false; player.transform.position = points[0]; cc.enabled = true; Physics.SyncTransforms();
                    for(int segment = 1; segment < points.Length; segment++)
                    {
                        var start = player.transform.position; var target = points[segment];
                        for(int step = 0; step < 350; step++)
                        {
                            var delta = target - player.transform.position; delta.y = 0;
                            if(delta.magnitude < .05f) break;
                            cc.Move(Vector3.ClampMagnitude(delta,.065f) + Vector3.down*.06f);
                        }
                        for(int settle = 0; settle < 12; settle++) cc.Move(Vector3.down*.06f);
                        var end = player.transform.position;
                        bool pass = Vector3.Distance(end,target) < .2f;
                        report.segments.Add(new Segment { route = side + (reverse ? " up" : " down"), start = start, target = target, end = end, status = pass ? "pass" : "inspect", obstruction = pass ? "" : string.Join("; ", Physics.OverlapSphere(end+Vector3.up*.9f,.6f).Where(c=>c!=cc).Select(c=>c.name)) });
                    }
                }
                File.WriteAllText(Folder + "/audit.json",JsonUtility.ToJson(report,true));
                Debug.Log("[Waterline] STAIR AUDIT COMPLETE: segments=" + report.segments.Count + " inspect=" + report.segments.Count(s=>s.status!="pass") + "; 12 fixed views require visual review."); Finish(0);
            }
            catch(Exception ex) { Debug.LogException(ex); Finish(1); }
        }
        private static void Finish(int code)
        { SessionState.SetBool(Key,false); EditorApplication.update -= Check; EditorApplication.Exit(code); }
    }
}
