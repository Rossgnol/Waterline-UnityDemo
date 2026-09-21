using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
namespace Waterline.Editor
{
    [InitializeOnLoad] public static class HarborSmokeTest
    {
        private const string Key="Waterline.HarborCheck";
        private static IEnumerator routine;
        private static double deadline;
        private static HarborLayout layout;
        private static DockSession dock;
        private static AnnexSession annex;
        private static FirstPersonController player;
        private static CharacterController controller;
        private static ThreatEncounter[] enemies;
        private static Dictionary<AnnexAction,AnnexInteractable> actions;
        private static Dictionary<DockAction,DockInteractable> dockActions;
        static HarborSmokeTest(){EditorApplication.playModeStateChanged+=OnPlay;}
        public static void Run(){HarborBuilder.Open();HarborBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunRewards(){ExplorationRewardsBuilder.Open();ExplorationRewardsBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunCirculation(){CirculationBuilder.Open();CirculationBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunFloodedLower(){FloodedLowerBuilder.Open();FloodedLowerBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunTidalRoutes(){TidalRouteBuilder.Open();TidalRouteBuilder.Validate();SessionState.SetBool(Key+"Skip",false);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunTidalFallback(){TidalRouteBuilder.Open();TidalRouteBuilder.Validate();SessionState.SetBool(Key+"Skip",true);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static bool PreparedRun {get{return layout.Planner!=null && !SessionState.GetBool(Key+"Skip",false);}}
        private static bool LowerExplored {get{return layout.LowerDeck!=null && (layout.Planner==null || PreparedRun);}}
        private static int ExtraSupply {get{return PreparedRun?1:0;}}
        private static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            deadline=EditorApplication.timeSinceStartup+480;routine=Check();EditorApplication.update+=Step;
        }
        private static void Step()
        {
            try{Require(EditorApplication.timeSinceStartup<deadline,"Timeout");if(routine.MoveNext())return;
                Debug.Log("[Waterline] HARBOR PASS: enlarged dock, staged physical doors, every room interaction, all patrol edges, bell, lighting, calibration, rest checkpoint, four patrols and annex captures, flood and escape.");Finish(0);}
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(code);}
        private static IEnumerator Check()
        {
            yield return null;layout=UnityEngine.Object.FindFirstObjectByType<HarborLayout>();dock=layout.dock;annex=layout.annex;
            player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();controller=player.GetComponent<CharacterController>();player.enabled=false;
            enemies=UnityEngine.Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None);
            actions=UnityEngine.Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).ToDictionary(i=>i.action);
            dockActions=UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).ToDictionary(i=>i.action);
            layout.enabled=false;Freeze();Require(annex.State.CalibrationRequired,"Calibration requirement not initialised");
            var supplies=annex.Supplies;if(supplies!=null){supplies.enabled=false;Require(annex.Journal.CollectedCount==0,"Uncollected journal leaked notes");}
            Require(layout.guards.All(g=>!g.Available),"North guards awake before north entry");
            // Test real blockers from their south/west approach before completing prerequisites.
            foreach(var door in layout.doors.Where(d=>!layout.IsOpen(d.rule)))
            {
                var normal=door.vertical?Vector3.right:Vector3.forward;
                Pose(controller,new Vector3(door.position.x,.03f,door.position.z)-normal*.8f);
                var before=controller.transform.position;controller.Move(normal*1.6f);
                Require(Vector3.Dot(controller.transform.position-before,normal)<.65f,"Closed gate can be crossed: "+door.from+" -> "+door.to);
            }
            Place(D(-8,-4));
            var walk=Path(D(-12.65f,-4),D(-12.65f,4.8f),D(-8,4.8f),D(-8,1.35f));while(walk.MoveNext())yield return null;
            if(layout.LowerDeck!=null && layout.Planner==null)
            {
                Require(layout.GoalRoom=="rigging_store","Pin goal not moved to rigging room");
                walk=Path(V(-10,3.5f),V(-10,8),V(-10,14.6f));while(walk.MoveNext())yield return null;
            }
            Use(DockAction.TakePin);
            if(LowerExplored)
            {
                if(layout.Planner!=null)
                {
                    walk=Path(V(-10,3.5f),V(-10,8),V(-10,14.6f));while(walk.MoveNext())yield return null;
                    Use(AnnexAction.TakeLowerSupplies);Require(!annex.Perform(AnnexAction.TakeLowerSupplies),"Lower supply duplicated");
                    walk=Path(V(-8,14.6f),V(-8,17),V(4,17),V(4,12.6f),V(2,12.6f));while(walk.MoveNext())yield return null;
                    Visible(actions[AnnexAction.TestFloatCircuit].transform);Require(!annex.Perform(AnnexAction.TestFloatCircuit),"Unfed float passed test");
                    walk=Path(V(4,12.6f),V(4,17),V(7,17),V(7,12.6f),V(9,12.6f));while(walk.MoveNext())yield return null;Use(AnnexAction.ToggleFloatIntake);
                    Require(!annex.Perform(AnnexAction.TestFloatCircuit),"Open drain held pressure");
                    walk=Path(V(12,12.6f));while(walk.MoveNext())yield return null;Use(AnnexAction.ToggleFloatDrain);
                    walk=Path(V(13,12.6f),V(13,17),V(4,17),V(4,12.6f),V(2,12.6f));while(walk.MoveNext())yield return null;Use(AnnexAction.TestFloatCircuit);
                    Require(annex.State.FloatPrepared,"Float not armed");
                    walk=Path(V(4,12.6f),V(4,17),V(11.5f,17.4f));while(walk.MoveNext())yield return null;
                }
                else
                {
                walk=Path(V(-8,14.6f),V(-8,17),V(0,17),V(9.5f,17),V(9.5f,14),V(11.5f,14.6f));while(walk.MoveNext())yield return null;
                }
                Use(AnnexAction.ReadFloodPlan);Require(annex.State.FloodPlanRead,"Flood plan not collected");
                walk=layout.Planner!=null?Path(V(13.2f,17.4f),V(13.2f,12),V(10,12),V(10,8),V(10,3.5f),V(4.2f,2.32f),V(-4.2f,2.32f)):Path(V(10,14),V(10,8),V(10,3.5f),V(4.2f,2.32f),V(-4.2f,2.32f));while(walk.MoveNext())yield return null;
                Require(!layout.LowerDeck.Raised && Mathf.Abs(layout.LowerDeck.platform.position.y+3.65f)<.01f,"Dry platform height incorrect");
            }
            walk=Path(D(-3,1.35f),D(-3,3.7f),D(3,3.7f),D(8,3.7f),D(8,4.8f),D(12.65f,4.8f),D(12.65f,-4),D(8.5f,-4),D(8.5f,-.2f));while(walk.MoveNext())yield return null;
            dock.threat.GetComponent<CharacterController>().enabled=true;dock.threat.Advance(.02f);Freeze();Use(DockAction.UnlockGate);
            walk=Path(D(10,-.2f),D(10,2.4f),D(8.4f,2.4f));while(walk.MoveNext())yield return null;if(layout.Planner!=null){walk=Path(V(13,.4f));while(walk.MoveNext())yield return null;}Use(DockAction.DeployBridge);
            var wait=Wait(1.5);while(wait.MoveNext())yield return null;
            Require(Mathf.Abs(dock.bridge.localScale.x-14)<.03f,"Bridge did not retain enlarged span");
            if(layout.Planner!=null){walk=Path(V(11.2f,.4f));while(walk.MoveNext())yield return null;}
            walk=Path(D(8,3.5f),D(-8,3.5f),D(-8,.8f));while(walk.MoveNext())yield return null;Use(DockAction.InstallPin);
            Require(layout.guards[0].Available && !layout.guards[1].Available,"First stage guard activation incorrect");
            CheckCirculationStage(6,false);
            Require(!dock.Perform(DockAction.StartFlood),"North pressure interlock bypassed");
            walk=Path(D(-8,3.5f),V(-11.2f,10));while(walk.MoveNext())yield return null;Use(AnnexAction.ReadDutyLog);
            layout.enabled=true;yield return null;yield return null;layout.enabled=false;Freeze();
            Require(layout.Visited.Contains("hub") && layout.CurrentRoom=="hub","Room discovery did not record hall");
            dock.MapVisible=true;Require(dock.HideGameplayHud && dock.InputBlocked,"Map fails to hide HUD and pause");dock.MapVisible=false;
            walk=Path(V(-11.2f,14),V(-17,14),V(-17,12),V(-25,12),V(-25,10));while(walk.MoveNext())yield return null;Use(AnnexAction.ReadWorkshopManual);
            if(supplies!=null)
            {
                Use(AnnexAction.ReadSupplyManifest);
                Require(annex.Journal.CollectedCount==(LowerExplored?4:3),"Read notes not collected");
                annex.Journal.Toggle();Require(dock.HideGameplayHud && dock.InputBlocked && annex.Journal.Showing,"Journal does not pause and hide HUD");
                Require(!annex.Perform(AnnexAction.DeployDecoy),"Journal allowed gameplay input");
                annex.Journal.ShowMap();Require(dock.MapVisible && !annex.Journal.Showing,"Journal cannot return to map");annex.Journal.Close();
            }
            walk=Path(V(-25,12),V(-17,12),V(-17,26),V(-25,26),V(-25,29));while(walk.MoveNext())yield return null;Use(AnnexAction.TakeFuse);
            walk=Path(V(-25,35),V(-23,35),V(-23,39));while(walk.MoveNext())yield return null;Use(AnnexAction.OpenWorkshopLoop);
            if(supplies!=null)
            {
                walk=Path(V(-23,43),V(-21.7f,44.8f));while(walk.MoveNext())yield return null;
                Visible(actions[AnnexAction.OpenSupplyCabinet].transform);Require(!actions[AnnexAction.OpenSupplyCabinet].Use(),"Wrong supply combination accepted");
                foreach(var pair in new[]{(AnnexAction.DialSupplyRepair,-25f,3),(AnnexAction.DialSupplyInspection,-24f,1),(AnnexAction.DialSupplyTide,-23f,4)})
                {walk=Path(V(pair.Item2,44.8f));while(walk.MoveNext())yield return null;for(int i=0;i<pair.Item3;i++)Use(pair.Item1);}
                walk=Path(V(-21.7f,44.8f));while(walk.MoveNext())yield return null;Use(AnnexAction.OpenSupplyCabinet);
                Require(annex.State.DecoyCharges==2+ExtraSupply && !supplies.cabinetContents.activeSelf,"Supply reward not reflected in scene");
                Require(!actions[AnnexAction.OpenSupplyCabinet].Use(),"Supply reward duplicated");
                walk=Path(V(-23,40),V(-17,40));while(walk.MoveNext())yield return null;
                var rewardCheck=CheckDecoys(supplies);while(rewardCheck.MoveNext())yield return null;
                Place(V(-23,39));
            }
            walk=Path(V(-23,40),V(-17,40),V(-17,32),V(-10,32),V(-10,24));while(walk.MoveNext())yield return null;Use(AnnexAction.RestorePower);
            Require(layout.guards[1].Available && !layout.guards[2].Available,"Power guard stage incorrect");
            CheckCirculationStage(9,false);
            walk=Path(V(-10,36),V(-10,44),V(-10,46));while(walk.MoveNext())yield return null;Use(AnnexAction.RestAtBench);
            Require(ThreatEncounter.IsSafe(player.transform.position),"Rest room not safe");
            walk=Path(V(-10,30),V(0,30),V(0,36),V(0,44));while(walk.MoveNext())yield return null;
            Use(AnnexAction.ReadArchiveIndex);Use(AnnexAction.TakeSignalKey);Require(layout.guards.All(g=>g.Available),"Third stage guard still asleep");
            CheckCirculationStage(layout.Planner!=null?20:19,true);
            walk=Path(V(10,44),V(10,30),V(17,30),V(17,25),V(25,25),V(27,26.2f));while(walk.MoveNext())yield return null;
            foreach(var e in enemies){e.GetComponent<CharacterController>().enabled=true;e.Advance(4.1f);}Freeze();
            Use(AnnexAction.RingInspectionBell);Require(!actions[AnnexAction.RingInspectionBell].Use(),"Bell cooldown bypassed");
            layout.enabled=true;dock.MapVisible=true;wait=Wait(.3);while(wait.MoveNext())yield return null;
            Require(!layout.guards.Any(g=>g.Mode==ThreatMode.Investigate),"Bell advanced through pause");dock.MapVisible=false;
            wait=Wait(3.3);while(wait.MoveNext())yield return null;layout.enabled=false;Freeze();
            Require(layout.guards.Any(g=>g.Mode==ThreatMode.Investigate),"Bell did not attract guards");
            walk=Path(V(25,25),V(25,36),V(25,46));while(walk.MoveNext())yield return null;Use(AnnexAction.ToggleEastLights);
            Require(layout.SightMultiplier(new Vector3(17,0,30))<1 && layout.eastLights.All(l=>l.intensity<.3f),"Dim lights did not affect corridor");
            walk=Path(V(25,42),V(17,42),V(17,53),V(11.5f,53.5f));while(walk.MoveNext())yield return null;Use(AnnexAction.OpenObservationLoop);
            CheckReturnPresentation(true);
            if(layout.GetComponent<HarborReturnLink>()!=null && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)CirculationBuilder.Capture("open");
            walk=Path(V(10,53),V(10,44),V(10,30),V(0,30),V(10,30),V(10,44),V(10,53),V(-22,53),V(-22,59),V(-18.8f,59));while(walk.MoveNext())yield return null;Use(AnnexAction.OpenServiceLoop);
            walk=Path(V(-22,59),V(-22,53),V(0,53),V(0,48),V(0,53),V(-4,53),V(-4,62),V(-7,62),V(-9,64.2f));while(walk.MoveNext())yield return null;Use(AnnexAction.TakeCalibrationPlate);
            walk=Path(V(-7,62),V(17,62),V(23,64.2f));while(walk.MoveNext())yield return null;
            Visible(actions[AnnexAction.ReadTideRecord].transform);Require(!actions[AnnexAction.ReadTideRecord].Use(),"Can read uncalibrated tide gauge");
            walk=Path(V(19,63.3f));while(walk.MoveNext())yield return null;Use(AnnexAction.AlignTideGauge);
            Require(!layout.calibrationPlate.activeSelf && !annex.State.HasCalibrationPlate,"Installed plate not consumed");
            walk=Path(V(23,64.2f));while(walk.MoveNext())yield return null;Use(AnnexAction.ReadTideRecord);
            walk=Path(V(17,62),V(17,12),V(28,10.3f));while(walk.MoveNext())yield return null;
            Visible(actions[AnnexAction.ConfirmPressure].transform);Require(!actions[AnnexAction.ConfirmPressure].Use(),"Wrong pressure accepted");
            foreach(var pair in new[]{(AnnexAction.DialHarbor,23.7f,4),(AnnexAction.DialDock,24.95f,2),(AnnexAction.DialSea,26.2f,6)})
            {walk=Path(V(pair.Item2,13.4f));while(walk.MoveNext())yield return null;for(int i=0;i<pair.Item3;i++)Use(pair.Item1);}
            walk=Path(V(28,10.3f));while(walk.MoveNext())yield return null;Use(AnnexAction.ConfirmPressure);
            walk=Path(V(22,9.7f));while(walk.MoveNext())yield return null;Use(AnnexAction.OpenShortcut);
            walk=Path(V(17,12),V(17,14),V(11.2f,14),V(11.2f,4),D(8,3.5f),D(-8,3.5f),D(-8,2.3f));while(walk.MoveNext())yield return null;
            dock.CapturePlayer();dock.RetryCheckpoint();Freeze();
            Require(Vector2.Distance(V(player.transform.position.x,player.transform.position.z),V(-10,44))<.1f,"Rest checkpoint position lost");
            Require(annex.State.GaugeAligned && annex.State.PressureBalanced && annex.State.WorkshopLoopOpen && annex.State.ObservationLoopOpen,"Checkpoint lost new world state");
            CheckReturnPresentation(true);
            if(supplies!=null)Require(annex.State.SupplyCabinetOpen && annex.State.DecoyCharges==ExtraSupply && !supplies.Busy && annex.Journal.CollectedCount==(LowerExplored?6:5),"Retry lost supply state or retained live decoy");
            if(LowerExplored)Require(annex.State.FloodPlanRead,"Retry lost flood plan");
            if(PreparedRun)Require(annex.State.FloatPrepared && annex.State.LowerSuppliesTaken,"Retry lost optional preparation");
            Require(enemies.All(e=>e.Attempts==2),"Not all guards reset");
            // Verify every edge with the real enemy capsule, including enlarged stairs.
            foreach(var e in enemies)
            {
                var cc=e.GetComponent<CharacterController>();
                for(int a=0;a<e.graph.nodes.Length;a++)foreach(int b in e.graph.nodes[a].links)
                {
                    Pose(cc,e.graph.nodes[a].position);var target=e.graph.nodes[b].position;int n=0;
                    while(Flat(cc.transform.position,target)>.1f)
                    {
                        Require(++n<900,"Enemy edge blocked: "+e.displayName+" "+a+" -> "+b+" at "+cc.transform.position);
                        var delta=target-cc.transform.position;delta.y=0;delta=Vector3.ClampMagnitude(delta,.12f);delta.y=-.1f;cc.Move(delta);
                        Require(cc.transform.position.y>-4.1f,"Enemy fell through floor");
                    }
                    yield return null;
                }
                cc.enabled=false;
            }
            Place(V(-10,44));foreach(var e in enemies)e.ResetEncounter();
            var distance=new float[enemies.Length];
            for(int i=0;i<3500;i++)for(int j=0;j<enemies.Length;j++)
            {var before=enemies[j].transform.position;enemies[j].Advance(.02f);distance[j]+=Vector3.Distance(before,enemies[j].transform.position);Require(!dock.Caught,"Rest room was invaded");}
            Require(distance.All(d=>d>15),"A patrol did not cover ground");
            foreach(var e in layout.guards)
            {
                Freeze();float x=e.displayName=="西廊巡检员"?-17:17,z=e.displayName=="测潮巡查员"?62:30;
                Pose(e.GetComponent<CharacterController>(),new Vector3(x,.03f,z));e.transform.rotation=Quaternion.Euler(0,180,0);Place(V(x,z-4));
                Require(e.CanSeePlayer(),"Guard cannot see clear target: "+e.displayName);
                for(int i=0;i<300 && !dock.Caught;i++)e.Advance(.02f);
                Require(dock.Caught,"Guard failed capture: "+e.displayName);dock.RetryCheckpoint();Freeze();
                Place(V(-10,44));foreach(var g in enemies){g.GetComponent<CharacterController>().enabled=true;g.Advance(4.1f);}Freeze();
            }
            Place(layout.Planner!=null?V(-11.2f,2.4f):D(-8,2.3f));foreach(var e in enemies)e.GetComponent<CharacterController>().enabled=true;
            Require(dock.Perform(DockAction.StartFlood),"Flood alarm rejected");
            for(int i=0;!dock.threat.EvacuationComplete;i++)
            {Require(i<4500,"Enlarged stair evacuation stuck");foreach(var e in enemies)e.Advance(.02f);if(i%20==0)yield return null;}
            dock.threat.Advance(.02f);yield return null;Require(dock.State.Phase==DockPhase.Filling,"Water never started");
            if(layout.LowerDeck!=null)
            {
                layout.Apply();layout.LowerDeck.Apply();
                Require(layout.LowerDeck.Closed && !layout.LowerDeck.Raised,"Filling topology incorrect");
                dock.MapVisible=true;float waterBefore=dock.State.WaterProgress,platformBefore=layout.LowerDeck.platform.position.y;
                var paused=Wait(.2);while(paused.MoveNext())yield return null;
                Require(Mathf.Abs(dock.State.WaterProgress-waterBefore)<.0001f && Mathf.Abs(layout.LowerDeck.platform.position.y-platformBefore)<.001f,"Map did not pause rising float");dock.MapVisible=false;
            }
            while(dock.State.Phase!=DockPhase.Flooded)yield return null;Freeze();
            if(layout.LowerDeck!=null)
            {
                layout.Apply();layout.LowerDeck.Apply();
                Require(Mathf.Abs(layout.LowerDeck.platform.position.y+(layout.Planner!=null && !PreparedRun?3.65f:.15f))<.01f && Mathf.Abs(layout.LowerDeck.water.position.y-dock.fullWaterHeight)<.01f,"Water/platform final heights mismatch");
                foreach(var door in layout.doors.Where(d=>d.rule==HarborGateRule.DryDock))
                {
                    Require(door.blocker.activeSelf,"Flooded lower entry still open");
                    Pose(controller,new Vector3(door.position.x,-3.47f,door.position.z-1));
                    controller.Move(Vector3.forward*2);Require(player.transform.position.z<door.position.z-.2f,"Flood barrier could be crossed");
                }
                if(layout.Planner==null || PreparedRun)
                {
                Place(V(-11.2f,4));
                walk=Path(V(-11.2f,8.65f),V(-5,8.65f),V(0,8.65f),V(5,8.65f),V(11.2f,8.65f),V(11.2f,4));while(walk.MoveNext())yield return null;
                Require(player.transform.position.y>-.2f,"Raised crossing fell into flooded pit");
                }
                else Require(!layout.LowerDeck.Raised && layout.doors.Where(d=>d.rule==HarborGateRule.FloatLink).All(d=>d.blocker.activeSelf),"Unprepared crossing opened");
                Debug.Log("[Waterline] FLOOD TRANSFORMATION PASS: three dry chambers, collected note, checkpoint, paused rise, sealed lower entries and physical upper crossing.");
            }
            if(layout.Planner!=null)
            {
                layout.Planner.Apply();Require(layout.Planner.bridgeBarriers.All(g=>g.activeSelf) && !dock.bridge.GetComponent<Collider>().enabled,"Navigation bridge did not retract");
                Place(V(-8,2));controller.Move(Vector3.right*2);Require(player.transform.position.x<-7.3f,"Retracted bridge barrier can be crossed");
                Place(V(-5,14));controller.Move(Vector3.right*3);Require(player.transform.position.x<-4.2f,"Pump bank can be walked through");
                Require(!annex.Perform(AnnexAction.ToggleFloatIntake),"Flood allowed lower adjustment");
                // Both candidate routes use the same start/end. Measure only collision paths, not human time.
                float south=0,shortRoute=0;
                Place(V(-11.2f,2.4f));var previous=player.transform.position;
                walk=Path(D(-8,-5.9f),D(-5.5f,-6.6f),D(-.8f,-6.6f),D(0,-5.6f),D(5,-5.6f),D(8,-6),D(10,-3.2f),V(11.2f,0),V(9.8f,0));
                while(walk.MoveNext()){south+=Flat(previous,player.transform.position);previous=player.transform.position;yield return null;}
                if(PreparedRun)
                {
                    Place(V(-11.2f,2.4f));previous=player.transform.position;
                    walk=Path(V(-11.2f,8.65f),V(11.2f,8.65f),V(11.2f,4),V(11.2f,0),V(9.8f,0));
                    while(walk.MoveNext()){shortRoute+=Flat(previous,player.transform.position);previous=player.transform.position;yield return null;}
                    Require(shortRoute<south,"Prepared route did not reduce travel");
                }
                Debug.Log("[Waterline] TIDAL ROUTES PASS: prepared="+PreparedRun+", southMetres="+south+", floatMetres="+shortRoute+"; same endpoints, guards frozen for distance.");
            }
            else {walk=Path(D(-8,3.5f),D(10,3.5f),D(10,-3.2f),D(5.8f,-2.4f));while(walk.MoveNext())yield return null;}
            Use(DockAction.BoardBoat);
            Require(dock.State.Phase==DockPhase.Escaped,"Harbor escape failed");
        }
        private static void CheckCirculationStage(int expected,bool northOpen)
        {
            if(layout.GetComponent<HarborReturnLink>()==null)return;
            var seen=new HashSet<string>{"hub"};var queue=new Queue<string>();queue.Enqueue("hub");
            while(queue.Count>0)
            {
                string room=queue.Dequeue();
                foreach(var door in layout.doors.Where(d=>d.from==room || d.to==room))
                {
                    string other=door.from==room?door.to:door.from;
                    if(layout.Room(other).sector==0 || !layout.IsOpen(door.rule) || !seen.Add(other))continue;queue.Enqueue(other);
                }
            }
            Require(seen.Count==expected,"Circulation phase reachability expected "+expected+" got "+seen.Count);
            var gate=layout.doors.Single(d=>d.from=="west_service" && d.to=="north_gallery");
            Require(layout.IsOpen(gate.rule)==northOpen,"West north gate opened at wrong phase");
            var saved=player.transform.position;Pose(controller,new Vector3(-17,.03f,49.2f));
            controller.Move(Vector3.forward*1.6f);float z=controller.transform.position.z;
            Require(northOpen?z>50.5f:z<49.8f,"West north gate collision disagrees with progression");
            Pose(controller,saved);CheckReturnPresentation(false);
            Debug.Log("[Waterline] CIRCULATION STAGE PASS: "+expected+" regions, northOpen="+northOpen);
        }
        private static void CheckReturnPresentation(bool open)
        {
            var link=layout.GetComponent<HarborReturnLink>();if(link==null)return;link.Apply();
            Require(link.ShowingOpen==open && link.routeMarkers.All(r=>r.sharedMaterial==(open?link.openMaterial:link.closedMaterial)),"Return line state failed");
            Require(link.statusLabels.All(l=>l.text==(open?"RETURN LINK / OPEN":"RETURN LINK / NORTH LATCH")),"Return destination labels stale");
        }
        private static IEnumerator CheckDecoys(HarborSupplies supplies)
        {
            Place(V(-10,14));Require(!annex.Perform(AnnexAction.DeployDecoy) && annex.State.DecoyCharges==2+ExtraSupply,"Safe-room placement consumed supply");
            var guard=layout.guards[0];guard.GetComponent<CharacterController>().enabled=true;guard.Advance(4.1f);Freeze();
            // Re-target halfway along an existing corridor edge, not only at authored nodes.
            Pose(guard.GetComponent<CharacterController>(),new Vector3(-17,.03f,34));guard.transform.rotation=Quaternion.Euler(0,180,0);Place(V(-17,40));
            Require(annex.Perform(AnnexAction.DeployDecoy) && supplies.Pending && annex.State.DecoyCharges==1+ExtraSupply,"Ground deployment failed");
            Require(!annex.Perform(AnnexAction.DeployDecoy) && annex.State.DecoyCharges==1+ExtraSupply,"Pending decoy allowed duplicate deployment");
            dock.MapVisible=true;supplies.Advance(10);Require(supplies.Pending && supplies.NoisePulses==0,"Map did not pause decoy");dock.MapVisible=false;
            annex.Journal.Toggle();supplies.Advance(10);Require(supplies.Pending,"Journal did not pause decoy");annex.Journal.Close();
            supplies.Advance(2.9f);Require(supplies.Pending && supplies.NoisePulses==0,"Decoy rang too soon");
            Place(V(-10,14));supplies.Advance(.2f);
            Require(supplies.Ringing && supplies.NoisePulses==1 && guard.Mode==ThreatMode.Investigate,"Decoy did not attract guard to its location");
            var sourcePosition=supplies.DevicePosition;float beforeDistance=Flat(guard.transform.position,sourcePosition);
            for(int i=0;i<100;i++)guard.Advance(.02f);
            Require(Flat(guard.transform.position,sourcePosition)<beforeDistance-.5f,"Investigating guard did not approach decoy");
            int pulses=supplies.NoisePulses;dock.Paused=true;supplies.Advance(10);Require(supplies.Ringing && supplies.NoisePulses==pulses,"Pause did not suspend ringing");dock.Paused=false;
            for(int i=0;i<65;i++)supplies.Advance(.1f);Require(!supplies.Busy,"Decoy never expired");
            // A newly closed obstacle must prevent the mid-edge entry optimisation.
            Pose(guard.GetComponent<CharacterController>(),new Vector3(-17,.03f,34));
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Test-only closed corridor barrier";
            obstacle.transform.position=new Vector3(-17,1,38);obstacle.transform.localScale=new Vector3(2,2,1);Physics.SyncTransforms();
            try
            {
                guard.HearNoise(new Vector3(-17,.1f,26),26);guard.HearNoise(new Vector3(-17,.1f,42),26);guard.Advance(.02f);
                Require(guard.transform.position.z<34,"Mid-edge entry ignored body obstruction");
            }
            finally{UnityEngine.Object.DestroyImmediate(obstacle);Physics.SyncTransforms();}
            Pose(guard.GetComponent<CharacterController>(),new Vector3(-16,.03f,34));
            guard.HearNoise(new Vector3(-17,.1f,26),26);guard.HearNoise(new Vector3(-17,.1f,42),26);guard.Advance(.02f);
            Require(guard.transform.position.z<34,"Mid-edge entry cut across an off-edge position");
            Pose(guard.GetComponent<CharacterController>(),new Vector3(-17,.03f,34));guard.transform.rotation=Quaternion.identity;Place(V(-17,38));
            for(int i=0;i<100 && guard.Mode!=ThreatMode.Chase;i++)guard.Advance(.02f);
            Require(guard.Mode==ThreatMode.Chase && !guard.HearNoise(new Vector3(-17,.2f,42),26),"Decoy can cancel established chase");
            Place(V(-17,40));Require(annex.Perform(AnnexAction.DeployDecoy) && annex.State.DecoyCharges==ExtraSupply,"Second deployment failed");
            // Keep the second device pending; the full-route retry must clear it without refunding ammo.
            foreach(var e in enemies)e.ResetEncounter();Freeze();
            Debug.Log("[Waterline] REWARDS SIDE PASS: physical combination, journal, delayed noise, pause, expiry and limited inventory.");
            yield return null;
        }
        private static void Freeze(){foreach(var e in enemies){e.enabled=false;e.GetComponent<CharacterController>().enabled=false;}}
        private static void Use(AnnexAction action){Visible(actions[action].transform);Require(actions[action].Use(),"Action failed "+action);annex.NoteOpen=false;Freeze();}
        private static void Use(DockAction action){Visible(dockActions[action].transform);Require(dockActions[action].Use(),"Dock action failed "+action);layout.Apply();Freeze();}
        private static void Visible(Transform t)
        {
            Physics.SyncTransforms();var delta=t.position-player.view.transform.position;
            Require(delta.magnitude<=dock.tuning.interactionDistance,"Out of reach "+t.name+" from "+player.transform.position);
            Require(player.RaycastInteraction(delta.normalized,out var hit) && (hit.transform==t || hit.transform.IsChildOf(t)),"Occluded "+t.name+" by "+(hit.transform==null?"none":hit.transform.name));
        }
        private static Vector2 V(float x,float z){return new Vector2(x,z);}
        private static Vector2 D(float x,float z){var p=dock.DockToWorld(new Vector3(x,0,z));return new Vector2(p.x,p.z);}
        private static void Place(Vector2 p){Pose(controller,new Vector3(p.x,.03f,p.y));}
        private static void Pose(CharacterController cc,Vector3 p){cc.enabled=false;cc.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
        private static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        private static IEnumerator Path(params Vector2[] points)
        {
            foreach(var p in points)
            {
                int n=0;while(Vector2.Distance(V(player.transform.position.x,player.transform.position.z),p)>.1f)
                {
                    Require(++n<1000,"Player route blocked toward "+p+" at "+player.transform.position);
                    var delta=Vector2.ClampMagnitude(p-V(player.transform.position.x,player.transform.position.z),.18f);controller.Move(new Vector3(delta.x,-.09f,delta.y));
                    Require(player.transform.position.y>-4.1f,"Player fell through floor");yield return null;
                }
            }
        }
        private static IEnumerator Wait(double seconds){double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
        private static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("[Harbor check] "+message);}
    }
}
