using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class RewardsVisualCheck
    {
        private const string Key="Waterline.RewardsVisual";
        private static AnnexSession annex;
        private static FirstPersonController player;
        private static double next,deadline;
        private static int stage;
        private static EditorWindow gameView;
        static RewardsVisualCheck(){EditorApplication.playModeStateChanged+=OnPlay;}
        public static void Run()
        {
            ExplorationRewardsBuilder.Open();SessionState.SetBool(Key,true);
            gameView=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Show();
            EditorApplication.isPlaying=true;
        }
        private static void OnPlay(PlayModeStateChange change)
        {
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            stage=0;next=EditorApplication.timeSinceStartup+2;deadline=next+60;EditorApplication.update+=Step;
        }
        private static void Step()
        {
            try
            {
                if(gameView==null)gameView=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Repaint();
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Visual capture timed out");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
                switch(stage++)
                {
                    case 0:
                        Directory.CreateDirectory("TestResults/Rewards");
                        annex=UnityEngine.Object.FindFirstObjectByType<AnnexSession>();player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                        foreach(var guard in UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))guard.enabled=false;
                        player.ResetPose(new Vector3(-25,.03f,10),180);
                        annex.Perform(AnnexAction.ReadDutyLog);annex.NoteOpen=false;
                        annex.Perform(AnnexAction.ReadWorkshopManual);annex.NoteOpen=false;
                        annex.Perform(AnnexAction.ReadSupplyManifest);break;
                    case 1:ScreenCapture.CaptureScreenshot("TestResults/Rewards/reading.png");break;
                    case 2:annex.NoteOpen=false;annex.Journal.Toggle();break;
                    case 3:ScreenCapture.CaptureScreenshot("TestResults/Rewards/journal.png");break;
                    case 4:annex.Harbor.Visited.UnionWith(new[]{"hub","workwear","west_service","spares","workshop"});annex.Journal.ShowMap();break;
                    case 5:ScreenCapture.CaptureScreenshot("TestResults/Rewards/map.png");break;
                    case 6:annex.Journal.Close();player.ResetPose(new Vector3(-24,.03f,44.8f),0);break;
                    case 7:ScreenCapture.CaptureScreenshot("TestResults/Rewards/cabinet-hud.png");break;
                    case 8:
                        for(int i=0;i<3;i++)annex.Perform(AnnexAction.DialSupplyRepair);
                        annex.Perform(AnnexAction.DialSupplyInspection);
                        for(int i=0;i<4;i++)annex.Perform(AnnexAction.DialSupplyTide);
                        annex.Perform(AnnexAction.OpenSupplyCabinet);break;
                    case 9:ScreenCapture.CaptureScreenshot("TestResults/Rewards/cabinet-open.png");break;
                    default:
                        foreach(var name in new[]{"reading","journal","map","cabinet-hud","cabinet-open"})
                            if(!File.Exists("TestResults/Rewards/"+name+".png"))throw new Exception("Screenshot missing: "+name);
                        Debug.Log("[Waterline] REWARDS GUI CAPTURED");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){EditorApplication.update-=Step;SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
    }
}
