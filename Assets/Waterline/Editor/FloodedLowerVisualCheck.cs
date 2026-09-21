using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class FloodedLowerVisualCheck
    {
        private const string Key="Waterline.LowerVisual";
        private const string Folder="TestResults/FloodedLower";
        private static HarborLayout layout;
        private static FirstPersonController player;
        private static EditorWindow view;
        private static int step;
        private static double next,deadline;
        static FloodedLowerVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            FloodedLowerBuilder.Open();SessionState.SetBool(Key,true);
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
                        layout.Visited.UnionWith(layout.rooms.Where(r=>r.sector==0 || r.id=="hub" || r.id=="float_link").Select(r=>r.id));
                        layout.dock.Paused=true;layout.Apply();layout.LowerDeck.Apply();
                        Shot("rigging-dry",new Vector3(-10,-1.8f,8),new Vector3(-10,-2,16));
                        Shot("drain-dry",new Vector3(10,-1.8f,8),new Vector3(11.5f,-2,16));
                        Shot("float-dry",new Vector3(0,-1.8f,17),new Vector3(0,-2.6f,8.7f));
                        Shot("hall-dry",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        player.ResetPose(new Vector3(-10,-3.47f,10),0);layout.dock.Paused=false;layout.dock.MapVisible=true;break;
                    case 1:ScreenCapture.CaptureScreenshot(Folder+"/map-dry.png");break;
                    case 2:
                        layout.dock.MapVisible=false;layout.dock.Paused=true;
                        foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge,DockAction.StartFlood})layout.dock.State.TryApply(action,out _);
                        layout.dock.State.Tick(6,12);layout.Apply();layout.LowerDeck.Apply();
                        Shot("hall-rising",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        layout.dock.State.Tick(6,12);layout.Apply();layout.LowerDeck.Apply();
                        Shot("hall-flooded",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
                        Shot("rigging-flooded",new Vector3(-10,-.5f,8),new Vector3(-10,-1,16));
                        layout.dock.Paused=false;layout.dock.MapVisible=true;break;
                    case 3:ScreenCapture.CaptureScreenshot(Folder+"/map-flooded.png");break;
                    case 4:
                        layout.dock.MapVisible=false;player.ResetPose(new Vector3(-5,.03f,8.65f),90);break;
                    case 5:layout.dock.MapVisible=true;break;
                    case 6:ScreenCapture.CaptureScreenshot(Folder+"/map-upper-link.png");break;
                    default:
                        foreach(var name in new[]{"rigging-dry","drain-dry","float-dry","hall-dry","hall-rising","hall-flooded","rigging-flooded","map-dry","map-flooded","map-upper-link"})
                            if(!File.Exists(Folder+"/"+name+".png"))throw new Exception("Missing capture "+name);
                        Debug.Log("[Waterline] LOWER VISUAL CAPTURE PASS: actual dry, filling and flooded states; inspect images before accepting.");Finish(0);break;
                }
            }
            catch(Exception error){Debug.LogException(error);Finish(1);}
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
