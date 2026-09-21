using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class SignVisualCheck
    {
        private const string Key="Waterline.SignVisual";
        private static HarborLayout layout;
        private static FirstPersonController player;
        private static TextMesh[] labels;
        private static Vector3[] positions;
        private static int step;
        private static double next,deadline;
        static SignVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){SignPresentationBuilder.CaptureSaved();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            step=0;next=EditorApplication.timeSinceStartup+2;deadline=next+45;EditorApplication.update+=Advance;
        }
        private static void Advance()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"Sign state check timeout");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;
                switch(step++)
                {
                    case 0:
                        layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
                        player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                        foreach(var e in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){e.enabled=false;e.body.gameObject.SetActive(false);}
                        var root=GameObject.Find(SignPresentationBuilder.RootName);
                        Require(root!=null && root.GetComponentsInChildren<Collider>(true).Length==0,"Sign presentation missing or added physics");
                        labels=Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name=="BRIDGE RETRACTS ON FLOOD" || t.name=="Sign WATERLINE / 07" || t.name=="FORWARD BERTH / EXIT" || t.name=="Annex sign FUSE / RETURN TO POWER" || t.name=="Annex sign CALIBRATION PLATE > TIDE").OrderBy(t=>t.GetInstanceID()).ToArray();
                        Require(labels.Length==6,"Expected six mounted signs");positions=labels.Select(t=>t.transform.position).ToArray();
                        Check("closed");
                        foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge})Require(layout.dock.State.TryApply(action,out _),"Cannot stage deployed bridge");
                        next=EditorApplication.timeSinceStartup+2;break;
                    case 1:
                        if(!layout.dock.bridge.GetComponent<Collider>().enabled){step--;return;}
                        Require(!layout.Planner.BridgeClosed,"Dry bridge should be open");Check("deployed");
                        Require(layout.dock.State.TryApply(DockAction.StartFlood,out _),"Cannot stage filling");
                        layout.dock.Paused=true;layout.Planner.Apply();next=EditorApplication.timeSinceStartup+.5;break;
                    case 2:
                        Require(layout.Planner.bridgeBarriers.All(b=>b.activeSelf),"Flood gates missing");Check("filling");
                        layout.dock.State.Tick(12,12);layout.dock.Paused=false;layout.Planner.Apply();next=EditorApplication.timeSinceStartup+2;break;
                    case 3:
                        if(layout.dock.bridge.localScale.x>.03f){step--;return;}
                        Require(layout.Planner.boardingGangway.activeSelf,"Full-water berth missing");Check("flooded");
                        Debug.Log("[Waterline] SIGN VISUAL PASS: six static signs remain mounted and visible across closed, deployed, filling and flooded states; original bridge/gate/berth transitions exercised. Inspect fixed-view captures for occlusion.");Finish(0);break;
                }
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void Check(string state)
        {
            for(int i=0;i<labels.Length;i++)Require(labels[i].GetComponent<Renderer>().enabled && labels[i].gameObject.activeInHierarchy && Vector3.Distance(labels[i].transform.position,positions[i])<.001f,"Sign moved or disappeared with bridge state");
            string folder="TestResults/SignFinish/runtime-"+state;Directory.CreateDirectory(folder);
            BoatPresentationBuilder.Shot(player.view,folder+"/east-bridge.png",new Vector3(10,1.7f,2),new Vector3(6,1.5f,2));
            BoatPresentationBuilder.Shot(player.view,folder+"/west-bridge.png",new Vector3(-10,1.7f,2),new Vector3(-6,1.5f,2));
            BoatPresentationBuilder.Shot(player.view,folder+"/dock-marker.png",new Vector3(4,-1.1f,-7),new Vector3(0,1.7f,4));
            BoatPresentationBuilder.Shot(player.view,folder+"/berth.png",new Vector3(11,1.7f,3.5f),new Vector3(8.4f,1.5f,0));
        }
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Advance;EditorApplication.Exit(code);}
    }
}
