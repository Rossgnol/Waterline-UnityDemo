using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class TidalRouteVisualCheck
    {
        private const string Key="Waterline.TidalVisual";
        private static string Folder => SessionState.GetString(Key+"Folder","TestResults/TidalRoutes");
        private static HarborLayout layout;
        private static FirstPersonController player;
        private static EditorWindow view;
        private static int step;
        private static double next,deadline;
        static TidalRouteVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        { SessionState.SetString(Key+"Folder","TestResults/TidalRoutes");StartCapture(); }
        public static void RunArt()
        { SessionState.SetString(Key+"Folder","TestResults/TidalArt");StartCapture(); }
        public static void RunUpperArt()
        { SessionState.SetString(Key+"Folder","TestResults/UpperArt");StartCapture(); }
        private static void StartCapture()
        {
            TidalRouteBuilder.Open();SessionState.SetBool(Key,true);
            view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.Show();EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange change)
        {
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            step=0;next=EditorApplication.timeSinceStartup+2;deadline=next+60;EditorApplication.update+=Advance;
        }
        private static void Advance()
        {
            try
            {
                if(view==null)view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.Repaint();
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Lower visual check timeout");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
                switch(step++)
                {
                    case 0:
                        Directory.CreateDirectory(Folder);layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
                        player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                        foreach(var enemy in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))enemy.enabled=false;
                        layout.Visited.UnionWith(layout.rooms.Where(r=>r.sector==0 || r.id=="hub" || r.id=="hub_east" || r.id=="float_link").Select(r=>r.id));
                        layout.dock.Paused=true;layout.Apply();layout.LowerDeck.Apply();layout.Planner.Apply();
                        Shot("rigging-dry",new Vector3(-10,-1.8f,8),new Vector3(-10,-2,16));
                        Shot("drain-dry",new Vector3(10,-1.8f,8),new Vector3(11.5f,-2,16));
                        Shot("float-dry",new Vector3(0,-1.8f,17),new Vector3(0,-2.6f,8.7f));
                        Shot("pump-bank",new Vector3(-10,1.7f,17),new Vector3(0,1.5f,17));
                        Shot("hall-dry",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        Shot("valves-close",new Vector3(10.5f,-1.8f,11.5f),new Vector3(10.5f,-2.1f,14));
                        Shot("pressure-close",new Vector3(2,-1.8f,11.3f),new Vector3(2,-2.05f,14));
                        Shot("supply-close",new Vector3(-10,-1.8f,13.5f),new Vector3(-10,-2.35f,16));
                        Shot("lower-tools",new Vector3(-9,-1.8f,-6),new Vector3(-11.2f,-2.5f,-3.6f));
                        layout.annex.State.LowerSuppliesTaken=true;layout.Planner.Apply();
                        Shot("supply-taken",new Vector3(-10,-1.8f,13.5f),new Vector3(-10,-2.35f,16));
                        layout.annex.State.LowerSuppliesTaken=false;layout.Planner.Apply();
                        layout.annex.State.FloatIntakeOpen=true;layout.annex.State.FloatDrainClosed=true;layout.Planner.Apply();
                        Shot("valves-open-closed",new Vector3(10.5f,-1.8f,11.5f),new Vector3(10.5f,-2.1f,14));
                        layout.annex.State.FloatIntakeOpen=false;layout.annex.State.FloatDrainClosed=false;layout.Planner.Apply();
                        if(Folder=="TestResults/UpperArt")UpperShot(false);
                        player.ResetPose(new Vector3(-10,-3.47f,10),0);layout.dock.Paused=false;layout.dock.MapVisible=true;break;
                    case 1:ScreenCapture.CaptureScreenshot(Folder+"/map-dry.png");break;
                    case 2:
                        layout.dock.MapVisible=false;layout.dock.Paused=true;
                        foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge,DockAction.StartFlood})layout.dock.State.TryApply(action,out _);
                        layout.annex.State.FloatIntakeOpen=true;layout.annex.State.FloatDrainClosed=true;layout.annex.State.TryApply(AnnexAction.TestFloatCircuit,out _);layout.dock.State.Tick(6,12);layout.Apply();layout.LowerDeck.Apply();layout.Planner.Apply();
                        Shot("hall-rising",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        layout.annex.State.FloatIntakeOpen=true;layout.annex.State.FloatDrainClosed=true;layout.annex.State.TryApply(AnnexAction.TestFloatCircuit,out _);layout.dock.State.Tick(6,12);layout.Apply();layout.LowerDeck.Apply();layout.Planner.Apply();
                        Shot("hall-flooded",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        Shot("rigging-flooded",new Vector3(-10,-.5f,8),new Vector3(-10,-1,16));
                        layout.dock.Paused=false;layout.dock.MapVisible=true;break;
                    case 3:ScreenCapture.CaptureScreenshot(Folder+"/map-flooded.png");break;
                    case 4:
                        layout.annex.State.FloatPrepared=false;layout.LowerDeck.Apply();layout.Apply();
                        Shot("hall-unprepared",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        layout.annex.State.FloatPrepared=true;layout.Apply();layout.LowerDeck.Apply();
                        layout.dock.MapVisible=false;player.ResetPose(new Vector3(-5,.03f,8.65f),90);break;
                    case 5:Shot("boarding",new Vector3(12,1.7f,4),new Vector3(5,0,0));Shot("retracted-bridge",new Vector3(-10,1.7f,1),new Vector3(0,0,2));if(Folder=="TestResults/UpperArt")UpperShot(true);layout.dock.MapVisible=true;break;
                    case 6:ScreenCapture.CaptureScreenshot(Folder+"/map-upper-link.png");break;
                    default:
                        foreach(var name in new[]{"rigging-dry","drain-dry","float-dry","hall-dry","hall-rising","hall-flooded","rigging-flooded","map-dry","map-flooded","map-upper-link"})
                            if(!File.Exists(Folder+"/"+name+".png"))throw new Exception("Missing capture "+name);
                        Debug.Log("[Waterline] TIDAL VISUAL CAPTURE PASS: actual dry, filling and flooded states; inspect images before accepting.");Finish(0);break;
                }
            }
            catch(Exception error){Debug.LogException(error);Finish(1);}
        }
        private static void UpperShot(bool prepared)
        {
            var visual=Object.FindFirstObjectByType<UpperInstrumentVisuals>();if(visual==null)throw new Exception("Upper instrument binding missing");
            var s=layout.annex.State;
            if(prepared){s.HasFuse=true;s.HasCalibrationPlate=true;s.GaugeAligned=true;s.Harbor=4;s.Dock=2;s.Sea=6;}
            layout.annex.Apply();layout.Apply();visual.Apply();
            string suffix=prepared?"-used":"-ready";
            Shot("fuse"+suffix,new Vector3(-27,1.7f,27.5f),new Vector3(-27,1.15f,29));
            Shot("plate"+suffix,new Vector3(-9,1.7f,64.4f),new Vector3(-9,1.1f,66));
            Shot("calibration"+suffix,new Vector3(19,1.7f,63.5f),new Vector3(19,1.2f,65));
            Shot("signal"+suffix,new Vector3(25,1.7f,13),new Vector3(25,1.5f,15));
            Shot("flood-control"+suffix,new Vector3(-12,1.7f,2.5f),new Vector3(-13.1f,1.1f,3.8f));
            if(prepared && (layout.annex.fuse.activeSelf || layout.calibrationPlate.activeSelf || !visual.installedPlate.activeSelf))throw new Exception("Installed/pickup presentation inconsistent");
        }
        private static void Shot(string name,Vector3 position,Vector3 target)
        {
            var camera=player.view;var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;
            camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=72;
            var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            File.WriteAllBytes(Folder+"/"+name+".png",pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=active;
            Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);camera.transform.position=p;camera.transform.rotation=q;camera.fieldOfView=f;
        }
        private static void Finish(int code){EditorApplication.update-=Advance;SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
    }
}
