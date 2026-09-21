using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    [InitializeOnLoad]
    public static class ThreatSmokeTest
    {
        private const string Key="Waterline.ThreatCheck.Active";
        private static IEnumerator routine;
        private static double deadline;
        private static ThreatEncounter threat;
        private static DockSession session;
        private static FirstPersonController player;
        static ThreatSmokeTest(){EditorApplication.playModeStateChanged+=OnPlay;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use a separate batch editor.");
            ThreatBuilder.Open();ThreatBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        private static void OnPlay(PlayModeStateChange change)
        {
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            deadline=EditorApplication.timeSinceStartup+240;routine=Check();EditorApplication.update+=Step;
        }
        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"Timed out.");
                if(routine.MoveNext())return;
                Debug.Log("[Waterline] THREAT PASS: graph collision routes, noise, vision, capture, retry, checkpoint, pause, physical evacuation, flood safety, upper patrol and escape.");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(code);}
        private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("[Threat check] "+message);}
        private static void Pose(Transform actor,Vector3 position,float yaw=0)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));cc.enabled=true;Physics.SyncTransforms();
        }
        private static void Advance(float seconds)
        {for(int i=0;i<Mathf.CeilToInt(seconds/.02f);i++)threat.Advance(.02f);}
        private static IEnumerator Wait(double seconds)
        {double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
        private static IEnumerator Check()
        {
            yield return null;
            threat=UnityEngine.Object.FindFirstObjectByType<ThreatEncounter>();session=threat.session;player=threat.player;
            player.enabled=false;threat.enabled=false;
            Pose(player.transform,new Vector3(-8,.08f,1));
            Advance(4.2f);
            Pose(threat.transform,new Vector3(3,-3.47f,3.7f),-90);
            Pose(player.transform,new Vector3(-3,-3.47f,3.7f));
            Require(threat.CanSeePlayer(),"Open lower deck sight line missing.");
            Require(!threat.HearNoise(player.transform.position,NoiseRules.Radius(GroundKind.Metal,MotionKind.Quiet)),"Quiet steps audible across six metres.");
            Require(threat.HearNoise(player.transform.position,NoiseRules.Radius(GroundKind.Metal,MotionKind.Run)),"Metal running was not heard.");
            Require(threat.Mode==ThreatMode.Investigate,"Sound did not initiate investigation.");
            Advance(.3f);Require(!session.Caught && threat.Awareness<1,"Instant recognition/capture.");
            Advance(.9f);Require(threat.Mode==ThreatMode.Chase,"Visible player not pursued after recognition.");
            Pose(player.transform,new Vector3(-8,-3.47f,0));
            Require(!threat.CanSeePlayer(),"Safe workshop is not safe.");Advance(.1f);
            Require(threat.Mode==ThreatMode.Search,"Safe-room entry does not end pursuit.");
            Advance(20);Require(!session.Caught && threat.Mode==ThreatMode.Patrol,"Enemy camps forever after losing the player.");
            Pose(threat.transform,new Vector3(0,-3.47f,3.7f),180);
            Pose(player.transform,new Vector3(0,-3.47f,-3.6f));
            Require(!threat.CanSeePlayer(),"Boat cabin fails to block vision.");
            Require(!threat.HearNoise(player.transform.position,10),"Solid occlusion fails to attenuate noise.");
            session.MapVisible=true;
            Vector3 frozen=threat.transform.position;float awareness=threat.Awareness;
            Advance(3);Require(threat.transform.position==frozen && threat.Awareness==awareness,"Map fails to pause enemy.");
            Require(!threat.HearNoise(threat.transform.position,20),"Paused enemy can hear new stimuli.");session.MapVisible=false;

            Pose(threat.transform,new Vector3(3,-3.47f,3.7f),-90);Pose(player.transform,new Vector3(2.15f,-3.47f,3.7f));
            Advance(.8f);Require(!session.Caught,"Capture ignores warning window.");Advance(2);
            Require(session.Caught,"Close visible player was never caught.");
            Require(!session.Perform(DockAction.TakePin),"Caught player can interact.");
            session.RetryCheckpoint();
            Require(!session.Caught && !session.State.HasPin && !session.HasCheckpoint && player.transform.position.x< -7,"Initial retry corrupts start.");
            var pin=UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(i=>i.action==DockAction.TakePin);
            Require(pin.gameObject.activeSelf,"Initial retry loses the pickup.");
            Require(pin.Use(),"Pin pickup failed.");
            Pose(player.transform,new Vector3(10,.08f,-3.8f));threat.Advance(.02f);
            Require(session.HasCheckpoint,"East upper arrival did not establish checkpoint.");
            Require(session.Perform(DockAction.UnlockGate) && session.Perform(DockAction.DeployBridge),"Upper mechanisms failed.");
            Pose(player.transform,new Vector3(-8,.08f,1.6f));Require(session.Perform(DockAction.InstallPin),"Repair failed.");
            session.CapturePlayer();session.RetryCheckpoint();yield return null;
            Require(session.State.PinInstalled && session.State.GateUnlocked && session.State.BridgeDeployed && !pin.gameObject.activeSelf && session.State.Phase==DockPhase.Dry,"Retry loses mechanism progress or duplicates pin.");
            Require(player.transform.position.x< -7 && player.transform.position.y>-.2f,"Repair checkpoint spawns in danger.");
            Require(threat.Attempts==3 && !threat.EvacuationComplete,"Retry does not reset encounter.");
            Require(threat.graph.Path(2,threat.graph.upperArrival,-1).Count==0,"Lower patrol can path onto upper deck early.");

            // Move the actual enemy capsule through every authored edge in both directions.
            var enemyCC=threat.GetComponent<CharacterController>();
            foreach(var start in Enumerable.Range(0,threat.graph.nodes.Length))
            foreach(int end in threat.graph.nodes[start].links)
            {
                Pose(threat.transform,threat.graph.nodes[start].position);
                Vector3 target=threat.graph.nodes[end].position;int count=0;
                while(Vector2.Distance(new Vector2(threat.transform.position.x,threat.transform.position.z),new Vector2(target.x,target.z))>.09f)
                {
                    Require(++count<400,"Graph edge blocked "+start+" -> "+end+" at "+threat.transform.position+" target "+target);
                    Vector3 d=target-threat.transform.position;d.y=0;d=Vector3.ClampMagnitude(d,.1f);d.y=-.08f;enemyCC.Move(d);
                    Require(threat.transform.position.y> -3.8f,"Graph route fell through floor.");
                }
                Require(Mathf.Abs(threat.transform.position.y-target.y)<.3f,"Graph edge has wrong height "+start+" -> "+end+" at "+threat.transform.position+" target "+target);
                yield return null;
            }
            Debug.Log("[Waterline] Every graph edge passed bidirectional capsule traversal.");
            session.RetryCheckpoint();
            Require(session.Perform(DockAction.StartFlood),"Alarm did not start.");
            Require(threat.Mode==ThreatMode.Evacuate && session.State.Phase==DockPhase.Dry,"Flood begins before evacuation.");
            var routes=UnityEngine.Object.FindFirstObjectByType<DockRoutes>();
            Pose(player.transform,new Vector3(-8,-3.47f,0));
            for(int frame=0;!threat.EvacuationComplete;frame++)
            {
                Require(frame<3000,"Evacuation blocked at "+threat.transform.position+" target "+threat.TargetNode);
                Vector3 before=threat.transform.position;threat.Advance(.02f);
                Vector3 move=threat.transform.position-before;move.y=0;
                Require(move.magnitude<.13f,"Enemy teleports during evacuation.");
                Require(session.State.Phase==DockPhase.Dry && routes.LowerOpen,"Flood closed stairs before evacuation completed.");
                if(frame%10==0)yield return null;
            }
            Require(threat.transform.position.y>-.2f,"Evacuated enemy is not upstairs.");Advance(.2f);
            Require(session.State.Phase==DockPhase.Dry,"Flood strands player still below.");
            Pose(player.transform,new Vector3(-8,.08f,1.6f));threat.Advance(.02f);yield return null;
            Require(session.State.Phase==DockPhase.Filling && !routes.LowerOpen,"Evacuation never releases flood interlock.");
            session.MapVisible=true;float water=session.State.WaterProgress;
            var wait=Wait(.3);while(wait.MoveNext())yield return null;
            Require(session.State.WaterProgress==water,"Map fails to pause flooding.");session.MapVisible=false;
            while(session.State.Phase!=DockPhase.Flooded){threat.Advance(.02f);yield return null;}
            for(int i=0;i<3000;i++)
            {
                threat.Advance(.02f);
                Require(threat.transform.position.y>-.3f && !ThreatEncounter.IsSafe(threat.transform.position),"Upper patrol leaves safe navigation bounds.");
            }
            Require(!session.Caught,"Safe room does not protect waiting player.");
            Require(session.Perform(DockAction.BoardBoat) && session.State.Phase==DockPhase.Escaped,"Threat encounter prevents valid escape.");
            frozen=threat.transform.position;Advance(2);Require(threat.transform.position==frozen,"Enemy keeps attacking after escape.");
            session.RetryCheckpoint();session.Perform(DockAction.StartFlood);
            Pose(player.transform,new Vector3(3.75f,-3.47f,3.7f));Advance(1.3f);
            Require(session.Caught,"Player can permanently block alarm evacuation without consequence.");
        }
    }
}
