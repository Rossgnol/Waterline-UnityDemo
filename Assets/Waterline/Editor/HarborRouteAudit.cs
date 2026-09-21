using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Waterline.Editor
{
    // Controlled collision traversal of candidate routes, not a shortest-path proof or a playtest.
    [InitializeOnLoad] public static class HarborRouteAudit
    {
        private const string Key="Waterline.RouteAudit";
        [Serializable] private sealed class Result
        {
            public string route;public bool observationOpen;
            public float horizontalMetres,unsafeMetres;public List<Vector3> waypoints;
        }
        [Serializable] private sealed class Report
        {
            public string method="Actual CharacterController collision traversal; manual candidate routes; guards and input disabled; fixed 0.09m movement requests. Distance is horizontal displacement; unsafe means HarborLayout.SafeAt=false, not observed enemy exposure. No scene saves.";
            public List<Result> routes=new List<Result>();
        }
        private static IEnumerator routine;private static double deadline;
        private static CharacterController controller;private static HarborLayout layout;
        private static Report report;
        static HarborRouteAudit(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){ExplorationRewardsBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            routine=Check();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Step;
        }
        private static void Step()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Route audit timed out");
                if(routine.MoveNext())return;
                Directory.CreateDirectory("TestResults/MapAudit");File.WriteAllText("TestResults/MapAudit/route-distances.json",JsonUtility.ToJson(report,true));
                Debug.Log("[Waterline] ROUTE AUDIT PASS: three candidate routes physically traversed");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){EditorApplication.update-=Step;SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
        private static Vector3 P(float x,float z){return new Vector3(x,.03f,z);}
        private static IEnumerator Check()
        {
            yield return null;yield return null;
            layout=UnityEngine.Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;controller=player.GetComponent<CharacterController>();
            foreach(var guard in UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))
            {guard.enabled=false;guard.GetComponent<CharacterController>().enabled=false;}
            var state=layout.annex.State;state.PowerRestored=true;state.HasSignalKey=true;
            state.WorkshopLoopOpen=false;state.ServiceLoopOpen=false;state.ObservationLoopOpen=false;
            layout.Apply();
            // Apply activates guard controllers; disable them again so only static route collision is tested.
            foreach(var guard in UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))guard.GetComponent<CharacterController>().enabled=false;
            report=new Report();
            var east=Walk("before-east-archive",false,new[]{P(10,53),P(17,53),P(17,30),P(10,30),P(10,44),P(0,44),P(0,30)});
            while(east.MoveNext())yield return null;
            var west=Walk("before-west-hall",false,new[]{P(10,53),P(-17,53),P(-17,14),P(-10,14),P(-10,30),P(0,30)});
            while(west.MoveNext())yield return null;
            state.ObservationLoopOpen=true;layout.Apply();
            foreach(var guard in UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))guard.GetComponent<CharacterController>().enabled=false;
            var shortcut=Walk("after-observation",true,new[]{P(10,53),P(10,30),P(0,30)});
            while(shortcut.MoveNext())yield return null;
        }
        private static IEnumerator Walk(string name,bool open,Vector3[] points)
        {
            controller.enabled=false;controller.transform.position=points[0];controller.enabled=true;Physics.SyncTransforms();
            var result=new Result{route=name,observationOpen=open,waypoints=new List<Vector3>(points)};
            for(int i=1;i<points.Length;i++)
            {
                int steps=0,stalled=0;
                while(true)
                {
                    var before=controller.transform.position;var delta=points[i]-before;delta.y=0;if(delta.magnitude<.04f)break;
                    if(++steps>2000)throw new Exception(name+" segment exceeded travel budget: "+i);
                    controller.Move(Vector3.ClampMagnitude(delta,.09f)+Vector3.down*.025f);
                    var distance=controller.transform.position-before;distance.y=0;float metres=distance.magnitude;
                    if(metres<.005f)stalled++;else stalled=0;
                    if(stalled>20)throw new Exception(name+" blocked near "+controller.transform.position+" toward "+points[i]);
                    if(controller.transform.position.y<-.2f || controller.transform.position.y>.2f)throw new Exception(name+" left expected floor");
                    result.horizontalMetres+=metres;if(!layout.SafeAt(controller.transform.position))result.unsafeMetres+=metres;
                    if(steps%8==0)yield return null;
                }
            }
            report.routes.Add(result);Debug.Log("[Route audit] "+name+" metres="+result.horizontalMetres+" unsafe="+result.unsafeMetres);
        }
    }
}
