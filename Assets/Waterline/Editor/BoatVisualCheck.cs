using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class BoatVisualCheck
    {
        private const string Key="Waterline.BoatVisual";
        private static HarborLayout layout;
        private static FirstPersonController player;
        private static Transform detail;
        private static Vector3 localOffset;
        private static double next,deadline;
        private static int step;
        static BoatVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            step=0;next=EditorApplication.timeSinceStartup+2;deadline=next+30;EditorApplication.update+=Advance;
        }
        private static void Advance()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Boat visual check timeout");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
                switch(step++)
                {
                    case 0:
                        Directory.CreateDirectory("TestResults/BoatFinish/runtime");layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
                        player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                        // Isolate boat art; forced water states bypass normal enemy evacuation, which the full smoke run covers.
                        foreach(var e in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){e.enabled=false;e.body.gameObject.SetActive(false);}
                        layout.dock.Paused=true;detail=layout.dock.boat.Find(BoatPresentationBuilder.BoatRoot);Require(detail!=null,"Boat details missing");localOffset=detail.localPosition;
                        Require(!layout.Planner.boardingGangway.activeSelf,"Dry boarding bridge visible");
                        StateShots("dry");
                        foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge,DockAction.StartFlood})Require(layout.dock.State.TryApply(action,out _),"Cannot stage water progress");
                        layout.dock.State.Tick(6,12);layout.Apply();layout.Planner.Apply();break;
                    case 1:
                        Require(Mathf.Abs(layout.dock.boat.position.y-Mathf.Lerp(layout.dock.dryBoatHeight,layout.dock.fullBoatHeight,.5f))<.01f,"Boat did not follow half water level");
                        Require(detail.localPosition==localOffset && !layout.Planner.boardingGangway.activeSelf,"Details/boarding bridge wrong during rise");
                        StateShots("rising");layout.dock.State.Tick(6,12);layout.Apply();layout.Planner.Apply();break;
                    case 2:
                        Require(layout.dock.State.Phase==DockPhase.Flooded && layout.Planner.boardingGangway.activeSelf,"Full-water boarding bridge missing");
                        Require(Mathf.Abs(layout.dock.boat.position.y-layout.dock.fullBoatHeight)<.01f && detail.localPosition==localOffset,"Boat decoration did not follow completed rise");
                        var gangway=layout.Planner.boardingGangway;var bridgeDetails=gangway.transform.Find(BoatPresentationBuilder.BridgeRoot);
                        Require(bridgeDetails!=null && bridgeDetails.gameObject.activeInHierarchy,"Boarding rail/treads hidden");
                        Require(Vector3.Distance(bridgeDetails.lossyScale,Vector3.one)<.001f,"Scaled bridge distorted decorative dimensions");
                        var deck=layout.dock.boat.Find("Deck").GetComponent<Renderer>().bounds;var bridge=gangway.GetComponent<Renderer>().bounds;
                        Require(Mathf.Abs(deck.max.y-bridge.max.y)<.08f && Mathf.Abs(deck.max.x-bridge.min.x)<.08f,"Deck/gangway alignment broken");
                        Physics.SyncTransforms();
                        for(int i=0;i<=8;i++)
                        {
                            float x=Mathf.Lerp(6.85f,1.60f,i/8f);
                            Require(Physics.Raycast(new Vector3(x,.4f,0),Vector3.down,out var hit,1) && hit.point.y>-.08f,"Gap in physical boarding floor");
                        }
                        StateShots("flooded");
                        BoatPresentationBuilder.Shot(player.view,"TestResults/BoatFinish/runtime/boarding-deck.png",new Vector3(6.6f,1.7f,0),new Vector3(.5f,.5f,-1.0f));
                        Require(layout.dock.Perform(DockAction.BoardBoat),"Boarding action failed");layout.Planner.Apply();break;
                    default:
                        Require(layout.dock.State.Phase==DockPhase.Escaped && layout.Planner.boardingGangway.activeSelf,"Escaped-state boarding details missing");
                        Debug.Log("[Waterline] BOAT VISUAL PASS: dry/rising/flooded geometry, exact parent follow, bridge scale and visibility, nine boarding floor contacts, existing BoardBoat action and escaped state.");Finish(0);break;
                }
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void StateShots(string state)
        {
            BoatPresentationBuilder.Shot(player.view,"TestResults/BoatFinish/runtime/"+state+"-berth.png",new Vector3(6.6f,1.7f,1.8f),new Vector3(0,.1f,-1.4f));
            float y=layout.dock.boat.position.y;
            BoatPresentationBuilder.Shot(player.view,"TestResults/BoatFinish/runtime/"+state+"-boat.png",new Vector3(5,y+2.5f,-8),new Vector3(0,y+.6f,-3.8f));
        }
        private static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Advance;EditorApplication.Exit(code);}
    }
}
