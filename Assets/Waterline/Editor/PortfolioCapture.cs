using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class PortfolioCapture
    {
        private const string Key="Waterline.PortfolioCapture";
        private const string Folder="Docs/Portfolio/Submission/Images";
        private static double next;
        static PortfolioCapture(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange s)
        {if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Capture;}}
        private static void Capture()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                Directory.CreateDirectory(Folder);var h=Object.FindFirstObjectByType<HarborLayout>();h.enabled=false;h.dock.Paused=true;
                var p=Object.FindFirstObjectByType<FirstPersonController>();p.enabled=false;
                foreach(var g in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))g.enabled=false;
                void Shot(string name,Vector3 from,Vector3 to)=>BoatPresentationBuilder.Shot(p.view,Folder+"/"+name+".png",from,to);
                Shot("dock",new Vector3(9,1.7f,3),new Vector3(1,-.9f,-.2f));
                Shot("controls",new Vector3(10.5f,-1.8f,11.5f),new Vector3(10.5f,-2.1f,14));
                Shot("float-dry",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge,DockAction.StartFlood})
                    if(!h.dock.State.TryApply(action,out var reason))throw new InvalidOperationException(reason);
                h.dock.State.Tick(12,12);h.LowerDeck.Apply();h.Planner.Apply();h.Apply();
                Shot("float-unprepared",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                h.annex.State.FloatIntakeOpen=true;h.annex.State.FloatDrainClosed=true;h.annex.State.FloatPrepared=true;
                h.LowerDeck.Apply();h.Planner.Apply();h.Apply();
                Shot("float-prepared",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                Debug.Log("[Waterline] PORTFOLIO CAPTURE COMPLETE: five current scene images; staged states, not player recording; scene not saved.");Finish(0);
            }catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Capture;EditorApplication.Exit(code);}
    }
}
