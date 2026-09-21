using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class TidalRouteBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day11_TidalRoutes.unity";
        private static Transform root;
        private static HarborLayout h;
        private static Material steel,amber,teal;
        [MenuItem("Waterline/25 Open prepared tidal routes")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        public static void Generate()
        {
            FloodedLowerBuilder.Open();h=Object.FindFirstObjectByType<HarborLayout>();
            root=new GameObject("DAY 11 / exploration prepares escape").transform;
            steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Deck steel.mat");
            amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Safety amber.mat");
            teal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Signal teal.mat");
            var p=h.gameObject.AddComponent<TidalRoutePlanner>();p.layout=h;p.readyMaterial=teal;p.idleMaterial=amber;
            foreach(var item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Chamber wall equipment" && t.position.x>0).ToArray())Object.DestroyImmediate(item.gameObject);
            // Make the central machinery an actual obstacle, with two distinct concourses.
            Box("Central pump exclusion plinth",new Vector3(0,.55f,16.5f),new Vector3(8,1.1f,11),steel);
            for(int i=0;i<3;i++)
            {
                var vessel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);vessel.name="Pump pressure vessel";vessel.transform.SetParent(root);vessel.transform.position=new Vector3(0,2.1f,12.8f+i*3.5f);vessel.transform.localScale=new Vector3(4.8f,1.2f,2.8f);vessel.GetComponent<Renderer>().sharedMaterial=steel;
                Box("Pump safety stripe",new Vector3(0,1.5f,11.46f+i*3.5f),new Vector3(6.5f,.16f,.08f),amber,false);
            }
            foreach(float x in new[]{-3.95f,3.95f})
            {
                var fence=Box("Pump exclusion fence",new Vector3(x,1.7f,16.5f),new Vector3(.15f,3.4f,11),steel);fence.GetComponent<Renderer>().enabled=false;
                Box("Pump handrail",new Vector3(x,1.6f,16.5f),new Vector3(.12f,.12f,11),amber,false);
                for(int j=0;j<7;j++)Box("Pump rail post",new Vector3(x,1.1f,11.3f+j*1.7f),new Vector3(.1f,1,.1f),steel,false);
            }
            h.Room("hub").bounds=Rect.MinMaxRect(-14,6,-4,22);h.Room("hub").title="大厅西侧";
            h.Room("hub").purpose="安全回程地标；泵组隔开东侧，浮台准备后可在注水时跨越。";
            var east=new HarborRoom{id="hub_east",title="大厅东侧",purpose="接东廊与船坞返程门；浮台跨越的终点。",bounds=Rect.MinMaxRect(4,6,14,22),sector=1,safe=true,ground=GroundKind.Rubber};
            h.rooms=h.rooms.Concat(new[]{east}).ToArray();
            foreach(var door in h.doors.Where(d=>d.position.x>0)){if(door.from=="hub")door.from="hub_east";if(door.to=="hub")door.to="hub_east";}
            h.Room("rigging_store").purpose="可选应急物资：一枚诱敌器，进入北楼后可使用。";
            h.Room("float_pit").purpose="观察通路终点并试压；准备成功后，注水会升起大厅近路。";
            h.Room("drain_service").purpose="沿管线打开进水、关闭排水，再回浮台井试压。";
            h.Room("float_link").purpose="需下层试压准备；注水后接通大厅两侧，免走南栈道。";
            h.Room("lower_tools").purpose="主线联轴销；北门可选探索，为后期撤离准备浮台。";
            h.Room("bridge").purpose="干坞时跨坞修泵；注水时自动收回，为船只让出航道。";
            var pin=Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==DockAction.TakePin);
            pin.transform.position=h.dock.DockToWorld(new Vector3(-8,.9f-3.5f,0));
            pin.description="主线联轴销。北门维护舱可选探索：物资和浮台准备。";
            // Explicit tabletop at the original tools approach, independent of older decoration.
            Box("Tools pin workbench",pin.transform.position+Vector3.down*.5f,new Vector3(1.6f,.8f,1.1f),steel);
            p.supplyPickup=Interact("应急诱敌器",new Vector3(-10,-2.38f,16),AnnexAction.TakeLowerSupplies,"取走一枚；北楼巡查区按 Q 部署。",teal);
            var inlet=Interact("浮箱进水阀",new Vector3(9,-2.25f,14),AnnexAction.ToggleFloatIntake,"蓝色管线供水。观察阀位和试压反馈。",teal);
            var drain=Interact("排水旁通阀",new Vector3(12,-2.25f,14),AnnexAction.ToggleFloatDrain,"橙色管线通往排水口；开启时浮箱无法保压。",amber);
            p.intakeHandle=inlet.transform;p.drainHandle=drain.transform;
            p.intakeLabel=Label("INLET / CLOSED",new Vector3(9,-1.5f,14.3f),0,.022f);
            p.drainLabel=Label("DRAIN / OPEN",new Vector3(12,-1.5f,14.3f),0,.022f);
            Box("Valve service bench",new Vector3(10.5f,-3.05f,14),new Vector3(4.5f,.8f,1),steel);
            Interact("浮台试压杆",new Vector3(2,-2.25f,14),AnnexAction.TestFloatCircuit,"观察压力：供水进入且不流走，才能准备浮台。",amber);
            Box("Pressure test pedestal",new Vector3(2,-3.05f,14),new Vector3(1.2f,.8f,1),steel);
            p.testLabel=Label("PRESSURE / NO FEED",new Vector3(2,-1.4f,14.20f),0,.025f);
            p.testBackLabel=Label("PRESSURE / NO FEED",new Vector3(2,-1.4f,14.30f),180,.025f);
            Box("Pressure readout backing",new Vector3(2,-1.4f,14.25f),new Vector3(3.2f,.45f,.06f),steel,false);
            p.pressureLamp=Box("Pressure indicator",new Vector3(2,-1.85f,14.1f),new Vector3(.3f,.18f,.15f),amber,false).GetComponent<Renderer>();
            // Visible plumbing links the valves to the test station, above the walkable doorway.
            Box("Blue inlet header",new Vector3(5.5f,-.75f,17),new Vector3(7,.12f,.12f),teal,false);
            Box("Blue inlet drop",new Vector3(9,-1.35f,15.5f),new Vector3(.12f,.12f,3),teal,false);
            Box("Orange drain header",new Vector3(7,-.95f,17.35f),new Vector3(10,.12f,.12f),amber,false);
            Box("Orange drain drop",new Vector3(12,-1.35f,15.5f),new Vector3(.12f,.12f,3.7f),amber,false);
            foreach(float x in new[]{2f,9f,12f})Box("Pipe riser",new Vector3(x,-1.45f,17),new Vector3(.12f,1.4f,.12f),x==12?amber:teal,false);
            Label("OPTIONAL / SUPPLIES + ESCAPE LINK",new Vector3(-10,-1.25f,19.78f),0,.026f);
            Label("INLET -> FLOAT TANK -> DRAIN",new Vector3(10.5f,-1.15f,19.75f),0,.025f);
            Label("FLOOD RETRACTS BRIDGE / PREPARE FLOAT OR WALK SOUTH",new Vector3(-10,-1.3f,5.05f),180,.016f);
            p.bridgeBarriers=new GameObject[2];
            for(int i=0;i<2;i++)
            {
                float x=i==0?-7.15f:7.15f;
                p.bridgeBarriers[i]=Box("Navigation clearance gate",new Vector3(x,.75f,2),new Vector3(.2f,1.5f,3.6f),amber);
                p.bridgeBarriers[i].GetComponent<Renderer>().enabled=false;
                for(int j=0;j<4;j++){var post=Box("Bridge closed upright",new Vector3(x,.75f,.3f+j*1.13f),new Vector3(.12f,1.5f,.12f),amber,false);post.transform.SetParent(p.bridgeBarriers[i].transform,true);}
                var rail=Box("Bridge closed handrail",new Vector3(x,1.5f,2),new Vector3(.12f,.12f,3.6f),amber,false);rail.transform.SetParent(p.bridgeBarriers[i].transform,true);
                p.bridgeBarriers[i].SetActive(false);
                Label("BRIDGE RETRACTS ON FLOOD",new Vector3(x+(i==0?-.25f:.25f),1.95f,2),i==0?90:-90,.017f);
            }
            // The dock guard never needs the removable bridge in its dry lower patrol.
            // Remove those nodes from this scene, so upper chase/investigation cannot target the gap.
            var graph=h.dock.threat.graph;var old=graph.nodes;
            var keep=Enumerable.Range(0,old.Length).Where(i=>!old[i].label.StartsWith("Bridge")).ToArray();
            var indices=keep.Select((id,index)=>new{id,index}).ToDictionary(v=>v.id,v=>v.index);
            graph.nodes=keep.Select(i=>new ThreatNode{label=old[i].label,position=old[i].position,level=old[i].level,links=old[i].links.Where(indices.ContainsKey).Select(j=>indices[j]).ToArray()}).ToArray();
            graph.lowerPatrol=graph.lowerPatrol.Select(i=>indices[i]).ToArray();graph.upperArrival=indices[graph.upperArrival];
            graph.upperPatrol=new[]{"East overlook","South east","South center","South cover west","South east"}.Select(s=>Array.FindIndex(graph.nodes,n=>n.label==s)).ToArray();
            foreach(var item in Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None))
                if(item.action==DockAction.StartFlood)item.description="注水会收回检修桥并封闭下层。已试压浮台可走大厅近路；否则走南栈道。长按确认。";
            MoveConsole(DockAction.StartFlood,new Vector3(-13.1f,1.1f,3.8f));
            MoveConsole(DockAction.DeployBridge,new Vector3(12.3f,1.1f,2));
            MoveConsole(DockAction.BoardBoat,new Vector3(8.4f,1.1f,0));
            // Board at the boat's forward quarter: the hall return reaches this berth directly.
            p.boardingGangway=Box("Forward boarding gangway",new Vector3(4.45f,-.12f,0),new Vector3(5.1f,.24f,1),steel);
            p.boardingGangway.SetActive(false);
            Label("FORWARD BERTH / EXIT",new Vector3(8.4f,2,0),180,.023f);
            for(int i=0;i<4;i++)Box("Hall to berth guide",new Vector3(11.2f,.015f,4-i),new Vector3(.25f,.02f,.5f),teal,false);
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if(label.text=="PIN MOVED > NORTH / RIGGING")label.text="NORTH / OPTIONAL ESCAPE PREPARATION";
                if(label.text=="TOOLS / NORTH > RIGGING")label.text="TOOLS / PIN + OPTIONAL NORTH";
                if(label.text=="01 RIGGING / PIN")label.text="";
                if(label.text=="DRAIN SERVICE / FLOOD PLAN" || label.text=="FLOAT WELL / RISES WITH WATER")label.text="";
                if(label.text=="INLET -> FLOAT TANK -> DRAIN")label.transform.position+=Vector3.up*.4f;
                if(label.gameObject.name=="Sign EXIT")label.transform.rotation=Quaternion.Euler(0,180,0);
            }
            foreach(var d in h.doors)
            {
                if(d.from=="lower_tools" && d.to=="rigging_store"){d.sign.text="OPTIONAL / SUPPLIES";d.hint="可选物资与浮台准备；注水后封闭。";}
                if(d.rule==HarborGateRule.FloatLink)d.hint="下层试压准备后，注水升起浮台。未准备可绕南栈道。";
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] TIDAL ROUTES GENERATED: optional supplies, readable water circuit, divided hall, retracting bridge and fallback route.");
        }
        private static void MoveConsole(DockAction action,Vector3 target)
        {
            var item=Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).Single(i=>i.action==action);var delta=target-item.transform.position;
            var stand=GameObject.Find(item.gameObject.name+" stand");if(stand!=null)stand.transform.position+=delta;
            var sign=GameObject.Find("Sign "+PrototypeBuilder.ControlLabel(action)) ?? GameObject.Find("Sign "+action.ToString().ToUpperInvariant());if(sign!=null)sign.transform.position+=delta;
            item.transform.position=target;
        }
        private static GameObject Box(string name,Vector3 pos,Vector3 size,Material material,bool collision=true)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        private static GameObject Interact(string title,Vector3 pos,AnnexAction action,string hint,Material material)
        {var go=Box(title,pos,new Vector3(.65f,.3f,.55f),material);var i=go.AddComponent<AnnexInteractable>();i.annex=h.annex;i.action=action;i.displayName=title;i.description=hint;return go;}
        private static TextMesh Label(string text,Vector3 pos,float yaw,float size)
        {var go=new GameObject(text);go.transform.SetParent(root);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);var label=go.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=72;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;var depth=go.AddComponent<WorldLabel>();depth.template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/World text.mat");depth.Refresh();return label;}
        public static void Validate()
        {PrototypeValidation.Validate();var layout=Object.FindFirstObjectByType<HarborLayout>();if(layout.Planner==null || layout.rooms.Length!=32 || layout.doors.Length!=36)throw new Exception("Tidal route topology mismatch");foreach(var d in layout.doors)if(layout.Room(d.from)==null || layout.Room(d.to)==null)throw new Exception("Bad endpoint");if(layout.dock.threat.graph.upperPatrol.Any(i=>i<0))throw new Exception("Missing patrol destination");}
        [MenuItem("Waterline/26 Build prepared tidal routes")]
        public static void BuildWindows()
        {Open();Validate();Directory.CreateDirectory("Builds/TidalRoutes");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/TidalRoutes/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Tidal route build failed");}
    }
}
