using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
namespace Waterline.Editor
{
    [InitializeOnLoad]
    public static class AnnexSmokeTest
    {
        private const string Key="Waterline.AnnexTest.Active";
        private static IEnumerator routine;
        private static double deadline;
        private static CharacterController controller;
        private static FirstPersonController player;
        private static AnnexSession annex;
        private static ThreatEncounter[] enemies;
        static AnnexSmokeTest(){EditorApplication.playModeStateChanged+=OnPlay;}
        public static void Run()
        {AnnexBuilder.Open();AnnexBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunConnected()
        {ConnectedBuilder.Open();ConnectedBuilder.ValidateConnected();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void OnPlay(PlayModeStateChange change)
        {
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            deadline=EditorApplication.timeSinceStartup+360;routine=Check();EditorApplication.update+=Step;
        }
        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"Timed out");if(routine.MoveNext())return;
                Debug.Log("[Waterline] "+(annex.Layout!=null?"CONNECTED":"ANNEX")+" PASS: original loop, six rooms, physical locks, reachable puzzle, shortcut, checkpoint, three patrols, captures, flood and escape.");Finish(0);
            }catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(code);}
        private static IEnumerator Check()
        {
            yield return null;annex=UnityEngine.Object.FindFirstObjectByType<AnnexSession>();var dock=annex.dock;
            player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;controller=player.GetComponent<CharacterController>();
            enemies=UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None);
            foreach(var e in enemies){e.enabled=false;e.GetComponent<CharacterController>().enabled=false;}
            var items=UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).ToDictionary(i=>i.action);
            var actions=UnityEngine.Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).ToDictionary(i=>i.action);
            Require(enemies.Length==3,"Missing enemies");
            foreach(var p in new[]{V(-8,-4),V(-12.65f,-4),V(-12.65f,4.8f),V(-8,4.8f),V(-8,1.35f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(items[DockAction.TakePin]);
            foreach(var p in new[]{V(-3,1.35f),V(-3,3.7f),V(3,3.7f),V(8,3.7f),V(8,4.8f),V(12.65f,4.8f),V(12.65f,-4),V(8.5f,-4),V(8.5f,-.2f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            dock.threat.GetComponent<CharacterController>().enabled=true;dock.threat.Advance(.02f);dock.threat.GetComponent<CharacterController>().enabled=false;Use(items[DockAction.UnlockGate]);
            foreach(var p in new[]{V(10,-.2f),V(10,2.4f),V(8.4f,2.4f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(items[DockAction.DeployBridge]);var wait=Wait(1.5);while(wait.MoveNext())yield return null;
            foreach(var p in new[]{V(8.4f,3.5f),V(-8,3.5f),V(-8,.8f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(items[DockAction.InstallPin]);Require(dock.HasCheckpoint,"Dock checkpoint missing");
            Require(!dock.Perform(DockAction.StartFlood),"Can bypass north annex pressure interlock");
            Place(V(3,9));controller.Move(Vector3.right*3);Require(player.transform.position.x<4.7f,"Signal lock can be crossed without key");
            Place(V(-2,10));controller.Move(Vector3.forward*3);Require(player.transform.position.z<Z(12)-.25f,"Archive lock can be crossed without power");
            Place(V(-8,.8f));
            foreach(var p in new[]{V(-8,3.5f),V(-8,7.5f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(actions[AnnexAction.ReadDutyLog]);
            Require(actions[AnnexAction.ReadDutyLog].Use() && annex.NoteOpen && dock.InputBlocked,"Record does not pause gameplay");
            var pausedPositions=enemies.Select(e=>e.transform.position).ToArray();foreach(var e in enemies)e.Advance(.5f);
            Require(enemies.Select((e,i)=>e.transform.position==pausedPositions[i]).All(v=>v),"Enemies move while reading");annex.NoteOpen=false;
            foreach(var p in new[]{V(-8,12.6f),V(-8,16.7f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(actions[AnnexAction.TakeFuse]);Require(!annex.fuse.activeSelf,"Fuse remained visible");
            foreach(var p in new[]{V(-8,9),V(-2,9),V(-.45f,8.15f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(actions[AnnexAction.RestorePower]);Require(annex.archiveLocks.All(g=>!g.activeSelf),"Archive locks did not release");
            foreach(var p in new[]{V(-2,9),V(-2,13),V(-2,15)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            if(annex.Layout==null)Use(actions[AnnexAction.ReadTideRecord]);else Use(actions[AnnexAction.ReadArchiveIndex]);Use(actions[AnnexAction.TakeSignalKey]);Require(annex.signalLocks.All(g=>!g.activeSelf),"Signal locks did not release");
            foreach(var p in new[]{V(3,15),V(8,15),V(8,19.5f),V(3,19.5f),V(-8,19.5f),V(-8,13),V(-8,9),V(-2,9),V(3,9),V(6.85f,8.9f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            if(annex.Layout!=null){var extra=CheckConnectedRooms(actions);while(extra.MoveNext())yield return null;}
            // The order as printed in the archive is deliberately not the input order.
            for(int i=0;i<6;i++)Use(actions[AnnexAction.DialHarbor]);
            var move=Walk(V(7.9f,8.9f));while(move.MoveNext())yield return null;for(int i=0;i<2;i++)Use(actions[AnnexAction.DialDock]);
            move=Walk(V(8.95f,8.9f));while(move.MoveNext())yield return null;for(int i=0;i<4;i++)Use(actions[AnnexAction.DialSea]);
            move=Walk(V(9.95f,8.1f));while(move.MoveNext())yield return null;
            Visible(actions[AnnexAction.ConfirmPressure].transform);Require(!actions[AnnexAction.ConfirmPressure].Use(),"Wrong-order code accepted");
            move=Walk(V(6.85f,8.9f));while(move.MoveNext())yield return null;for(int i=0;i<5;i++)Use(actions[AnnexAction.DialHarbor]);
            move=Walk(V(8.95f,8.9f));while(move.MoveNext())yield return null;for(int i=0;i<2;i++)Use(actions[AnnexAction.DialSea]);
            move=Walk(V(9.95f,8.1f));while(move.MoveNext())yield return null;Use(actions[AnnexAction.ConfirmPressure]);Require(annex.State.PressureBalanced,"Correct code rejected");
            move=Walk(V(8,7.6f));while(move.MoveNext())yield return null;Use(actions[AnnexAction.OpenShortcut]);
            foreach(var p in new[]{V(8,4),V(8,3.5f),V(-8,3.5f),V(-8,2.3f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            dock.CapturePlayer();dock.RetryCheckpoint();yield return null;
            Require(annex.State.PressureBalanced && annex.State.PowerRestored && annex.State.RecordRead && annex.State.ShortcutOpen && !annex.signalKey.activeSelf && !annex.fuse.activeSelf,"Checkpoint failed to restore annex world");
            Require(enemies.All(e=>e.Attempts==2),"Not all enemies reset on retry");
            Place(V(-8,8));
            foreach(var e in enemies)e.GetComponent<CharacterController>().enabled=false;
            foreach(var e in enemies.Where(e=>!e.managesDockFlood))
            {
                var cc=e.GetComponent<CharacterController>();
                for(int start=0;start<e.graph.nodes.Length;start++)foreach(int end in e.graph.nodes[start].links)
                {
                    Pose(cc,e.graph.nodes[start].position);Vector3 target=e.graph.nodes[end].position;int steps=0;
                    while(Flat(cc.transform.position,target)>.09f)
                    {
                        Require(++steps<450,"New enemy edge blocked "+e.displayName+" "+start+" -> "+end+" at "+cc.transform.position);
                        var delta=target-cc.transform.position;delta.y=0;delta=Vector3.ClampMagnitude(delta,.1f);delta.y=-.08f;cc.Move(delta);
                        Require(cc.transform.position.y>-.3f,"Enemy fell off annex floor");
                    }
                    yield return null;
                }
                cc.enabled=false;
            }
            foreach(var e in enemies)e.ResetEncounter();
            float[] distance=new float[enemies.Length];
            for(int i=0;i<3000;i++)foreach(var pair in enemies.Select((e,index)=>(e,index)))
            {
                var before=pair.e.transform.position;pair.e.Advance(.02f);distance[pair.index]+=Vector3.Distance(before,pair.e.transform.position);
                Require(!dock.Caught,"Duty safe room failed");
            }
            Require(distance.All(d=>d>8),"One of the three patrols did not move");
            var northGuard=enemies.First(e=>e.displayName=="北楼巡检员");
            Pose(northGuard.GetComponent<CharacterController>(),new Vector3(3,.03f,Z(19.5f)));
            Place(V(3,15));annex.BroadcastFootstep(annex.Layout!=null?10:7,player.transform.position);
            Require(northGuard.Mode==ThreatMode.Investigate,"Player footstep is not forwarded to annex guard");
            foreach(var e in enemies.Where(e=>!e.managesDockFlood))
            {
                foreach(var other in enemies)if(other!=e)other.GetComponent<CharacterController>().enabled=false;
                float x=e.displayName=="北楼巡检员"?-8:8;
                Pose(e.GetComponent<CharacterController>(),new Vector3(x,.03f,Z(15)));e.transform.rotation=Quaternion.Euler(0,180,0);Place(V(x,13));
                Require(e.CanSeePlayer(),"New enemy cannot see player in its area");
                for(int i=0;i<220 && !dock.Caught;i++)e.Advance(.02f);
                Require(dock.Caught,"New enemy never captures visible player: "+e.displayName);
                dock.RetryCheckpoint();Place(V(-8,8));for(int i=0;i<220;i++)foreach(var guard in enemies)guard.Advance(.02f);
            }
            Place(V(-8,2.3f));Require(dock.Perform(DockAction.StartFlood),"Expanded progression cannot start alarm");
            for(int i=0;!dock.threat.EvacuationComplete;i++)
            {
                Require(i<3000,"Dock worker evacuation stuck");foreach(var e in enemies)e.Advance(.02f);if(i%10==0)yield return null;
            }
            dock.threat.Advance(.02f);yield return null;Require(dock.State.Phase==DockPhase.Filling,"Expanded level never fills");
            while(dock.State.Phase!=DockPhase.Flooded)yield return null;
            foreach(var e in enemies)e.GetComponent<CharacterController>().enabled=false;
            foreach(var p in new[]{V(-8,3.5f),V(10,3.5f),V(10,-3.2f),V(5.8f,-2.4f)}){var walk=Walk(p);while(walk.MoveNext())yield return null;}
            Use(items[DockAction.BoardBoat]);Require(dock.State.Phase==DockPhase.Escaped,"Expanded escape failed");
        }
        private static IEnumerator CheckConnectedRooms(System.Collections.Generic.Dictionary<AnnexAction,AnnexInteractable> actions)
        {
            var dock=annex.dock;
            Require(!annex.Layout.serviceGate.activeSelf==annex.State.ServiceLoopOpen,"Service gate state mismatch");
            dock.MapVisible=true;Require(dock.HideGameplayHud && dock.InputBlocked,"Map must hide gameplay HUD and pause");dock.MapVisible=false;
            Require(!dock.HideGameplayHud,"Gameplay HUD remains hidden after map closes");
            foreach(var e in enemies){e.GetComponent<CharacterController>().enabled=true;e.Advance(4.1f);e.GetComponent<CharacterController>().enabled=false;}
            // Enter the west wing from duty, use the manual, and return through packing and spares.
            foreach(var p in new[]{new Vector2(3,10.8f),new Vector2(-8,10.8f),new Vector2(-8,10),new Vector2(-16,10),new Vector2(-16.7f,12)})
                {var w=Walk(p);while(w.MoveNext())yield return null;}
            Use(actions[AnnexAction.ReadWorkshopManual]);
            Require(ThreatEncounter.GroundAt(player.transform.position)==GroundKind.Rubber,"West bypass lacks rubber footsteps");
            foreach(var p in new[]{new Vector2(-16,12),new Vector2(-16,22),new Vector2(-8,22),new Vector2(-8,27.5f),new Vector2(-16,27.5f),new Vector2(-16,22),new Vector2(-8,22),new Vector2(-8,27.5f),new Vector2(-8,35),new Vector2(-2,35),new Vector2(-2,32)})
                {var w=Walk(p);while(w.MoveNext())yield return null;}
            Use(actions[AnnexAction.OpenServiceLoop]);Require(!annex.Layout.serviceGate.activeSelf,"Dispatch does not open archive shortcut");
            // Walk the new shortcut in both directions; no teleport across the lock.
            foreach(var p in new[]{new Vector2(-2,27.5f),new Vector2(-2,24),new Vector2(-2,27.5f),new Vector2(-2,35),new Vector2(3,35),new Vector2(8,35)})
                {var w=Walk(p);while(w.MoveNext())yield return null;}
            Use(actions[AnnexAction.ReadTideRecord]);
            foreach(var p in new[]{new Vector2(8,27.5f),new Vector2(8,19.6f)}){var w=Walk(p);while(w.MoveNext())yield return null;}
            Use(actions[AnnexAction.RingInspectionBell]);
            Require(!actions[AnnexAction.RingInspectionBell].Use(),"Bell cooldown can be bypassed");
            var wait=Wait(3.3);while(wait.MoveNext())yield return null;
            Require(enemies.Any(e=>!e.managesDockFlood && e.Mode==ThreatMode.Investigate),"Delayed bell did not draw a patrol");
            foreach(var p in new[]{new Vector2(8,17.5f),new Vector2(8,14.3f),new Vector2(5.65f,14.3f),new Vector2(5.65f,10.64f),new Vector2(6.85f,10.64f)}){var w=Walk(p);while(w.MoveNext())yield return null;}
            Debug.Log("[Waterline] CONNECTED ROOMS PASS: two-way west loop, dispatch shortcut, measurement record, delayed bell and modal HUD state.");
        }
        private static void Use(DockInteractable item){Visible(item.transform);Require(item.Use(),"Dock action failed: "+item.name);}
        private static void Use(AnnexInteractable item){Visible(item.transform);Require(item.Use(),"Annex action failed: "+item.name);annex.NoteOpen=false;}
        private static void Visible(Transform item)
        {
            Physics.SyncTransforms();Vector3 delta=item.position-player.view.transform.position;
            Require(delta.magnitude<=annex.dock.tuning.interactionDistance,"Out of reach: "+item.name);
            Require(player.RaycastInteraction(delta.normalized,out var hit) && (hit.transform==item || hit.transform.IsChildOf(item)),"Occluded: "+item.name+" hit "+(hit.transform==null?"nothing":hit.transform.name));
        }
        private static Vector2 V(float x,float z){return new Vector2(x,Z(z));}
        private static float Z(float z){return annex!=null && annex.Layout!=null?ConnectedLayout.ExpandZ(z):z;}
        private static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        private static void Place(Vector2 p){Pose(controller,new Vector3(p.x,.03f,p.y));}
        private static void Pose(CharacterController cc,Vector3 p){cc.enabled=false;cc.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
        private static IEnumerator Walk(Vector2 p)
        {
            int n=0;while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),p)>.09f)
            {
                Require(++n<550,"Player route blocked toward "+p+" at "+player.transform.position);
                var d=Vector2.ClampMagnitude(p-new Vector2(player.transform.position.x,player.transform.position.z),.12f);controller.Move(new Vector3(d.x,-.09f,d.y));
                Require(player.transform.position.y> -4,"Player fell through floor");yield return null;
            }
        }
        private static IEnumerator Wait(double seconds){double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
        private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("[Annex check] "+message);}
    }
}
