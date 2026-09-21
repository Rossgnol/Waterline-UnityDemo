using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using static Waterline.Editor.AnnexBuilder;
using Object=UnityEngine.Object;
namespace Waterline.Editor
{
    public static class HarborBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day07_Harbor.unity";
        private static HarborLayout layout;
        private static readonly List<HarborRoom> rooms=new List<HarborRoom>();
        private static readonly List<HarborDoor> doors=new List<HarborDoor>();
        private static readonly HashSet<string> walls=new HashSet<string>();
        [MenuItem("Waterline/15 Open staged harbor")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        public static void Generate()
        {
            ThreatBuilder.Open();rooms.Clear();doors.Clear();walls.Clear();
            var dock=Object.FindFirstObjectByType<DockSession>();ScaleDock(dock);
            root=new GameObject("HARBOR - Staged wings and landmark hall").transform;
            wall=Mat("Painted plaster",new Color(.28f,.34f,.35f));floor=Mat("Worn tiles",new Color(.17f,.22f,.23f));
            dark=Mat("Machine steel",Color.gray);brass=Mat("Warning brass",Color.yellow);red=Mat("Oxide panels",Color.red);blue=Mat("Cabinet blue",Color.blue);paper=Mat("Old records",Color.white);glow=Mat("Worklight",Color.white);
            annex=root.gameObject.AddComponent<AnnexSession>();annex.dock=dock;dock.annex=annex;
            annex.archiveLocks=Array.Empty<GameObject>();annex.signalLocks=Array.Empty<GameObject>();annex.powerLights=Array.Empty<Light>();
            layout=root.gameObject.AddComponent<HarborLayout>();layout.dock=dock;layout.annex=annex;
            root.gameObject.AddComponent<HarborMap>().layout=layout;root.gameObject.AddComponent<AnnexMap>().annex=annex;
            Object.FindFirstObjectByType<DockMap>().enabled=false;
            DefineRooms();DefineDoors();layout.rooms=rooms.ToArray();layout.doors=doors.ToArray();
            foreach(var room in rooms.Where(r=>r.sector>0))BuildRoom(room);
            ConnectDock();BuildDoors();Furnish();BuildPatrols();HarborArt.Apply(layout);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] HARBOR GENERATED: dock footprint x2.24; 14 north rooms, five corridors, staged gates and four patrols.");
        }
        private static void ScaleDock(DockSession dock)
        {
            dock.dockScale=new Vector2(1.4f,1.6f);
            var player=Object.FindFirstObjectByType<FirstPersonController>();var enemy=dock.threat;
            var geometry=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(r=>!r.transform.IsChildOf(enemy.transform) && !r.transform.IsChildOf(player.transform))
                .Select(r=>(t:r.transform,p:r.transform.position,s:r.transform.localScale,q:r.transform.rotation,text:r.GetComponent<TextMesh>()!=null)).ToArray();
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>(l,p:l.transform.position)).ToArray();
            dock.boat.position=dock.DockToWorld(dock.boat.position);
            foreach(var g in geometry)
            {
                g.t.position=dock.DockToWorld(g.p);if(g.text)continue;
                Vector3 axisZ=g.q*Vector3.forward,axisY=g.q*Vector3.up,axisX=g.q*Vector3.right;
                Vector3 S(Vector3 a){return new Vector3(a.x*1.4f,a.y,a.z*1.6f);}
                g.t.rotation=Quaternion.LookRotation(S(axisZ),S(axisY));
                g.t.localScale=new Vector3(g.s.x*S(axisX).magnitude,g.s.y*S(axisY).magnitude,g.s.z*S(axisZ).magnitude);
            }
            foreach(var pair in lights){pair.l.transform.position=dock.DockToWorld(pair.p);if(pair.l.type==LightType.Point)pair.l.range*=1.35f;}
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if(label.text.StartsWith("01  CONTROL"))label.text="03 CONTROL / PUMP";
                if(label.text.StartsWith("03  TOOL ROOM"))label.text="01 TOOL ROOM / PIN";
            }
            player.transform.position=dock.DockToWorld(player.transform.position);enemy.transform.position=dock.DockToWorld(enemy.transform.position);
            foreach(var graph in Object.FindObjectsByType<ThreatGraph>(FindObjectsSortMode.None))foreach(var n in graph.nodes)n.position=dock.DockToWorld(n.position);
            enemy.areaLimited=true;enemy.areaMin=new Vector2(-22.4f,-17);enemy.areaMax=new Vector2(22.4f,5.5f);
        }
        private static void R(string id,string title,string purpose,float a,float b,float s,float n,int sector=1,bool safe=false,bool corridor=false,GroundKind ground=GroundKind.Concrete,int level=0)
        {rooms.Add(new HarborRoom{id=id,title=title,purpose=purpose,bounds=Rect.MinMaxRect(a,s,b,n),sector=sector,safe=safe,corridor=corridor,ground=ground,level=level});}
        private static void DefineRooms()
        {
            R("west_dock","水控室","修泵、注水；连接西梯、检修桥与中央大厅。",-15.4f,-7,-11.6f,4.5f,0,true);
            R("east_dock","绞盘间","解锁坞门、展桥、登艇。",7,15.4f,-11.6f,4.5f,0);
            R("bridge","检修桥","展桥后的跨坞近路；金属脚步较响。",-7,7,.4f,3.6f,0,false,true,GroundKind.Metal);
            R("south_walk","南侧栈道","开锁后连通东西岸；橡胶地面，设备提供遮挡。",-15.4f,15.4f,-15.6f,-10.8f,0,false,true,GroundKind.Rubber);
            R("west_stairs","西梯","蓝色楼梯通往下层工具间。",-20.5f,-15.4f,-11.6f,4.3f,0,false,true);
            R("east_stairs","东梯","橙色楼梯通往绞盘间，注水前供拖链工疏散。",15.4f,20.5f,-11.6f,4.3f,0,false,true);
            R("lower_tools","工具间","联轴销；下层可暂避的维修空间。",-20.5f,-7,-10.8f,5.2f,0,true,false,GroundKind.Rubber,-1);
            R("lower_dock","坞底通道","绕过船体抵达东梯；注水后封闭。",-7,20.5f,-10.8f,5.2f,0,false,true,GroundKind.Concrete,-1);
            R("hub","中央值守大厅","港区地标与安全区。蓝色西门找零件，北墙左门去机房，钥匙开放东区。",-14,14,6,22,1,true);
            R("west_service","西侧服务廊","蓝色主路通往零件库；橡胶降低脚步噪声。",-20,-14,6,50,1,false,true,GroundKind.Rubber);
            R("workwear","工装间","静音作业手册；进入危险区前了解声音规则。",-30,-20,6,18);
            R("spares","零件库","备用熔断器；北门连接机修车间。",-30,-20,18,32);
            R("workshop","机修车间","遮挡设备与内侧门栓，开启维修回程路线。",-30,-20,32,50);
            R("power","备用机房","安装熔断器，开放泵压厅和北区电锁。",-14,-6,22,38);
            R("rest","维修休息室","可暂避，并在休息凳记录新的重试位置。",-14,-6,38,50,1,true);
            R("pump_hall","泵压厅","从机房前往档案室；观测捷径开启后成为返程通道。",-6,6,22,38);
            R("archive","航务档案室","信号钥匙与测潮移交说明；北门由调度台控制。",-6,6,38,50);
            R("inspection_link","巡检廊","连接档案室、东侧长廊与大厅；返程门接回泵压厅。",6,14,22,38,1,false,true);
            R("archive_exit","档案东廊","钥匙解锁后通往巡检廊；北端为观测返程门。",6,14,38,50,1,false,true);
            R("east_service","东侧长廊","黄色方向线向北去测潮区，向南去信号室；可在灯控室调暗。",14,20,6,50,1,false,true,GroundKind.Metal);
            R("signal","信号室","三阀泄压与船坞东门解锁；可安全整理记录。",20,30,6,18,1,true);
            R("inspection","查验间","延时诱敌铃；与灯控室形成小回路。",20,30,18,32);
            R("light_control","灯控室","调暗东侧走廊，降低远距离被看见的风险。",20,30,32,50);
            R("north_gallery","北回廊","连接调度、仪器、测潮三个明确去向。",-30,30,50,56,2,false,true,GroundKind.Metal);
            R("dispatch","调度室","开启档案室北门，返回机房时减少绕行。",-30,-14,56,70,2);
            R("instruments","仪器间","测潮标尺；东门直接进入测潮大厅。",-14,6,56,70,2);
            R("tide","测潮大厅","安装标尺，读取今日三处水域刻度。",6,30,56,70,2);
        }
        private static void D(string a,string b,float x,float z,bool vertical,string sign,HarborGateRule rule=HarborGateRule.Open,string hint=null)
        {doors.Add(new HarborDoor{from=a,to=b,position=new Vector3(x,1.2f,z),vertical=vertical,rule=rule,hint=hint??sign});}
        private static void DefineDoors()
        {
            D("west_dock","hub",-11.2f,6,false,"HALL / AFTER PUMP",HarborGateRule.DockRepaired,"北楼入口：先在水控室安装联轴销修泵。");
            D("east_dock","hub",11.2f,6,false,"DOCK RETURN",HarborGateRule.DockReturn,"船坞东门：完成北楼探索后，从信号室解锁。");
            D("hub","west_service",-14,14,true,"WEST / PARTS",HarborGateRule.Open,"西侧服务廊：蓝色主路通向零件库。");
            D("hub","power",-10,22,false,"POWER / FUSE");
            D("hub","inspection_link",10,22,false,"EAST / SIGNAL KEY",HarborGateRule.SignalKey,"东区门：航务档案室的信号钥匙可解锁。");
            D("hub","east_service",14,14,true,"EAST / SIGNAL KEY",HarborGateRule.SignalKey,"东侧长廊：先从航务档案室取得信号钥匙。");
            D("west_service","workwear",-20,12,true,"WORKWEAR / MANUAL");
            D("west_service","spares",-20,26,true,"04 PARTS / FUSE");
            D("spares","workshop",-25,32,false,"WORKSHOP / RETURN");
            D("workshop","west_service",-20,40,true,"RETURN LATCH",HarborGateRule.WorkshopReturn,"维修回程门：门栓在机修车间内侧。");
            D("west_service","power",-14,32,true,"POWER SHORTCUT",HarborGateRule.WorkshopReturn,"机房侧门：在机修车间打开维修回程门栓。");
            D("west_service","north_gallery",-17,50,false,"NORTH / POWER LOCK",HarborGateRule.Power,"北区电锁：先恢复备用电源。");
            D("power","rest",-10,38,false,"REST / CHECKPOINT",HarborGateRule.Power,"维修休息室：送电后开放，可以记录重试位置。");
            D("power","pump_hall",-6,30,true,"PUMP HALL / POWER",HarborGateRule.Power,"泵压厅电锁：在备用机房安装熔断器并送电。");
            D("pump_hall","archive",0,38,false,"06 ARCHIVE",HarborGateRule.Power,"航务档案室：需要恢复备用电源。");
            D("archive","archive_exit",6,44,true,"EAST / SIGNAL KEY",HarborGateRule.SignalKey,"档案东门：本桌信号钥匙可以解锁。");
            D("archive_exit","inspection_link",10,38,false,"EAST SERVICE");
            D("inspection_link","east_service",14,30,true,"NORTH > TIDE");
            D("inspection_link","pump_hall",6,30,true,"OBSERVATION RETURN",HarborGateRule.ObservationReturn,"泵压厅返程门：从北回廊内侧开启观测回程门栓。");
            D("archive_exit","north_gallery",10,50,false,"OBSERVATION LATCH",HarborGateRule.ObservationReturn,"观测返程门：门栓位于北回廊一侧。");
            D("archive","north_gallery",0,50,false,"DISPATCH CONTROL",HarborGateRule.Dispatch,"档案北门：北侧调度室控制台可远程开启。");
            D("east_service","signal",20,12,true,"09 SIGNAL / SAFE",HarborGateRule.SignalKey,"信号室：需要航务档案室的钥匙。");
            D("east_service","inspection",20,25,true,"INSPECTION / BELL");
            D("inspection","light_control",25,32,false,"LIGHT CONTROL");
            D("light_control","east_service",20,42,true,"RETURN / EAST");
            D("east_service","north_gallery",17,50,false,"NORTH / OBSERVATION",HarborGateRule.SignalKey,"观测区门：信号钥匙可以解锁。");
            D("north_gallery","dispatch",-22,56,false,"DISPATCH / SHORTCUT");
            D("north_gallery","instruments",-4,56,false,"07 INSTRUMENTS");
            D("instruments","tide",6,62,true,"08 TIDE GAUGE");
            D("north_gallery","tide",17,56,false,"TIDE / RETURN");
        }
        private static void BuildRoom(HarborRoom r)
        {
            var b=r.bounds;var center=new Vector3(b.center.x,-.2f,b.center.y);
            var material=r.ground==GroundKind.Rubber?Mat("Quiet rubber",new Color(.07f,.15f,.13f)):floor;
            Cube(r.id+" / floor",center,new Vector3(b.width,.4f,b.height),material);
            float height=r.id=="hub"?5.4f:r.id=="tide"?4.6f:3.5f;
            Cube(r.id+" / ceiling",new Vector3(b.center.x,height,b.center.y),new Vector3(b.width,.2f,b.height),dark);
            Edge(b.xMin,b.yMin,b.yMax,true);Edge(b.xMax,b.yMin,b.yMax,true);Edge(b.yMin,b.xMin,b.xMax,false);Edge(b.yMax,b.xMin,b.xMax,false);
            int nx=Mathf.Max(1,Mathf.CeilToInt(b.width/8)),nz=Mathf.Max(1,Mathf.CeilToInt(b.height/9));
            for(int ix=0;ix<nx;ix++)for(int iz=0;iz<nz;iz++)
                Lamp(new Vector3(b.xMin+(ix+.5f)*b.width/nx,height-.6f,b.yMin+(iz+.5f)*b.height/nz),r.safe || r.id=="workwear");
            if(r.corridor)
            {
                bool vertical=b.height>b.width;
                Cube(r.id+" / route stripe",new Vector3(b.center.x,.012f,b.center.y),vertical?new Vector3(.16f,.008f,b.height-.5f):new Vector3(b.width-.5f,.008f,.16f),r.id=="west_service"?blue:brass,false);
            }
        }
        private static void Edge(float constant,float start,float end,bool vertical)
        {
            var gaps=doors.Where(d=>Mathf.Abs((vertical?d.position.x:d.position.z)-constant)<.01f && (vertical?d.position.z:d.position.x)>start+.1f && (vertical?d.position.z:d.position.x)<end-.1f)
                .Select(d=>vertical?d.position.z:d.position.x).OrderBy(v=>v).ToArray();
            float cursor=start;
            foreach(float gap in gaps){Segment(cursor,gap-1.25f);cursor=gap+1.25f;}Segment(cursor,end);
            void Segment(float a,float b)
            {
                if(b-a<.01f)return;string key=vertical+"/"+constant+"/"+a+"/"+b;if(!walls.Add(key))return;
                Cube("Harbor wall",vertical?new Vector3(constant,1.8f,(a+b)/2):new Vector3((a+b)/2,1.8f,constant),vertical?new Vector3(.24f,3.6f,b-a):new Vector3(b-a,3.6f,.24f),wall);
            }
        }
        private static void ConnectDock()
        {
            Object.DestroyImmediate(GameObject.Find("WEST / CONTROL north wall"));Object.DestroyImmediate(GameObject.Find("EAST / WINCH north wall"));Object.DestroyImmediate(GameObject.Find("North seawall"));
            WallZ(4.4f,-15.4f,-7,-11.2f);WallZ(4.4f,7,15.4f,11.2f);
            Cube("Expanded north seawall lower",new Vector3(0,-1.88f,5.36f),new Vector3(41.3f,3.64f,.5f),dark);
            WallZ(5.36f,-20.65f,20.65f,-11.2f,11.2f);
            foreach(float x in new[]{-11.2f,11.2f})Cube("Harbor portal threshold",new Vector3(x,-.15f,5.2f),new Vector3(2.5f,.3f,2),floor);
        }
        private static void BuildDoors()
        {
            for(int i=0;i<doors.Count;i++)
            {
                var d=doors[i];Vector3 p=d.position;
                Cube("Portal lintel",new Vector3(p.x,2.98f,p.z),d.vertical?new Vector3(.35f,.85f,2.5f):new Vector3(2.5f,.85f,.35f),dark);
                foreach(float side in new[]{-1.3f,1.3f})Cube("Portal frame",p+(d.vertical?Vector3.forward:Vector3.right)*side,new Vector3(.16f,2.4f,.16f),d.rule==HarborGateRule.Open?blue:brass,false);
                if(d.rule!=HarborGateRule.Open)
                {
                    d.blocker=Cube("Gate / "+d.from+" / "+d.to,p,d.vertical?new Vector3(.2f,2.4f,2.45f):new Vector3(2.45f,2.4f,.2f),red);
                    var marker=d.blocker.AddComponent<HarborDoorMarker>();marker.layout=layout;marker.index=i;
                }
                string english=DoorEnglish(d);
                d.sign=Label(english,new Vector3(p.x,2.95f,p.z)+(d.vertical?Vector3.left:Vector3.back)*.22f,d.vertical?90:0,.024f);
                Label(english,new Vector3(p.x,2.95f,p.z)+(d.vertical?Vector3.right:Vector3.forward)*.22f,d.vertical?-90:180,.024f);
            }
        }
        private static string DoorEnglish(HarborDoor d)
        {
            switch(d.to)
            {
                case "hub":return "CENTRAL HALL";case "west_service":return d.from=="hub"?"WEST / 04 PARTS":"WEST RETURN";
                case "spares":return "04 PARTS / FUSE";case "power":return "05 AUX POWER";case "archive":return "06 ARCHIVE / KEY";
                case "instruments":return "07 INSTRUMENTS";case "tide":return "08 TIDE GAUGE";case "signal":return "09 SIGNAL / SAFE";
                case "rest":return "REST / CHECKPOINT";case "workwear":return "WORKWEAR / MANUAL";case "workshop":return "WORKSHOP / LATCH";
                case "dispatch":return "DISPATCH / DOOR CONTROL";case "inspection":return "INSPECTION / DELAY BELL";
                case "light_control":return "LIGHT CONTROL";case "north_gallery":return d.rule==HarborGateRule.Open?"NORTH GALLERY":"NORTH / SERVICE GATE";
                case "pump_hall":return "PUMP HALL";case "east_service":return "EAST / NORTH TO TIDE";default:return "EAST INSPECTION";
            }
        }
        private static void Furnish()
        {
            Desk("Hall route desk",new Vector3(-9,0,10),3);
            Item(AnnexAction.ReadDutyLog,"阅读复航总图",new Vector3(-9,1.02f,10),paper,"三个阶段：修复船坞、恢复动力、测潮泄压。",new Vector3(.7f,.04f,.5f));
            // A recognisable hoist and gantry make each return to the central hall legible.
            foreach(float x in new[]{-3f,3f})Cube("Hall gantry column",new Vector3(x,2.6f,15),new Vector3(.45f,5.2f,.45f),brass);
            Cube("Hall gantry beam",new Vector3(0,5,15),new Vector3(7,.5f,.7f),brass);
            Cube("Suspended pump landmark",new Vector3(0,2.1f,15),new Vector3(3,2.4f,2.8f),blue);
            Label("07 / CENTRAL WATCH HALL",new Vector3(0,4,20.9f),0,.06f);
            Label("WEST: PARTS      NORTH: POWER      EAST: OBSERVATION",new Vector3(0,3.2f,20.9f),0,.029f);
            Desk("Workwear manual bench",new Vector3(-27,0,10),3);Shelf(new Vector3(-29.3f,0,14),true);
            Item(AnnexAction.ReadWorkshopManual,"阅读静音作业手册",new Vector3(-27,1.02f,10),paper,"了解轻步、遮挡与维修回程门。",new Vector3(.6f,.04f,.45f));
            Desk("Spare fuse bench",new Vector3(-27,0,29),3);
            annex.fuse=Item(AnnexAction.TakeFuse,"取走备用熔断器",new Vector3(-27,1.14f,29),brass,"送到大厅北墙左侧的备用机房。",new Vector3(.2f,.32f,.2f));
            Label("FUSE / RETURN TO POWER",new Vector3(-27,1.9f,30.7f),0,.03f);
            foreach(float z in new[]{21f,25f})Shelf(new Vector3(-29.3f,0,z),true);
            foreach(float z in new[]{36f,44f})Cube("Workshop machine cover",new Vector3(-28,1,z),new Vector3(2.4f,2,3),blue);
            Item(AnnexAction.OpenWorkshopLoop,"打开维修回程门栓",new Vector3(-21.2f,1.2f,39),brass,"开启车间东门与机房侧门，缩短带件返程。",new Vector3(.3f,.6f,.3f));
            Cube("Aux generator",new Vector3(-12,.7f,27),new Vector3(1.8f,1.4f,3),dark);
            Cylinder("Generator rotor",new Vector3(-12,1.5f,27),.55f,2.5f,blue,Quaternion.Euler(90,0,0));
            Pedestal(AnnexAction.RestorePower,"安装熔断器并送电",-8,24,"开放泵压厅、北区与维修休息室。");
            Desk("Rest checkpoint bench",new Vector3(-12,0,46),2.5f);
            Item(AnnexAction.RestAtBench,"在休息凳记录进度",new Vector3(-12,1.02f,46),paper,"之后被拦截可从维修休息室重试。",new Vector3(.6f,.04f,.5f));
            Label("REST / SAFE / CHECKPOINT",new Vector3(-10,2.7f,49.6f),0,.028f);
            foreach(float x in new[]{-4f,4f})
            {Cylinder("Pressure vessel",new Vector3(x,1.2f,33),.8f,2.4f,blue,Quaternion.identity);Cube("Vessel base",new Vector3(x,.25f,33),new Vector3(1.9f,.5f,1.9f),dark);}
            Label("PUMP PRESSURE HALL",new Vector3(0,2.85f,37.7f),0,.033f);
            Desk("Archive reading table",new Vector3(0,0,46),3);
            annex.signalKey=Item(AnnexAction.TakeSignalKey,"取走信号钥匙",new Vector3(.65f,1.08f,46),brass,"开放档案东门与东侧观测路线。",new Vector3(.3f,.08f,.18f));
            Item(AnnexAction.ReadArchiveIndex,"阅读测潮移交清单",new Vector3(-.65f,1.02f,46),paper,"说明仪器间标尺、测潮架与返程捷径。",new Vector3(.7f,.04f,.45f));
            foreach(float x in new[]{-5.3f,5.3f})Shelf(new Vector3(x,0,47),false);
            Pedestal(AnnexAction.OpenObservationLoop,"打开观测返程门栓",11.5f,52.3f,"开启档案东廊北门，以及巡检廊通向泵压厅的门。");
            Pedestal(AnnexAction.OpenServiceLoop,"开启档案室北门",-17,59,"通电后远程开启档案室北门。");
            Desk("Dispatch chart table",new Vector3(-24,0,66),5);
            Cube("Dispatch shelving cover",new Vector3(-25,1.1f,60),new Vector3(2,2.2f,1.5f),blue);
            Desk("Instrument calibration bench",new Vector3(-9,0,66),4);
            layout.calibrationPlate=Item(AnnexAction.TakeCalibrationPlate,"取走测潮标尺",new Vector3(-9,1.13f,66),brass,"仪器间东门通向测潮大厅，把标尺装到测潮架。",new Vector3(.8f,.16f,.35f));
            Label("CALIBRATION PLATE > TIDE",new Vector3(-9,2.2f,68),0,.032f);
            foreach(float x in new[]{-12f,2f})Cube("Instrument rack",new Vector3(x,1,59),new Vector3(1.8f,2,2),blue);
            Desk("Tide recording console",new Vector3(23,0,66),4);
            Item(AnnexAction.ReadTideRecord,"读取今日潮位记录",new Vector3(23,1.02f,66),paper,"先安装标尺完成标定；读后记录在 M 地图中。",new Vector3(.8f,.04f,.5f));
            Pedestal(AnnexAction.AlignTideGauge,"把标尺装入测潮架",19,65,"安装仪器间取得的标尺，恢复今日刻度。");
            for(int i=0;i<3;i++)
            {
                Cylinder("Tall tide gauge",new Vector3(21+i*2.6f,1.7f,68),.18f,3.4f,brass,Quaternion.identity);
                for(int j=0;j<7;j++)Cube("Gauge index",new Vector3(21+i*2.6f,.3f+j*.45f,67.75f),new Vector3(.7f,.035f,.04f),paper,false);
            }
            Label("TIDE OBSERVATORY / 07",new Vector3(18,3.5f,69.7f),0,.05f);
            Desk("Inspection delay bell table",new Vector3(27,0,28),3);
            layout.bell=Item(AnnexAction.RingInspectionBell,"启动延时查验铃",new Vector3(27,1.13f,28),red,"3 秒后响铃，提前从另一扇门离开；12 秒复位。",new Vector3(.4f,.26f,.4f)).transform;
            Cube("Inspection sight blocker",new Vector3(22,1,29),new Vector3(1.4f,2,2),blue);
            Pedestal(AnnexAction.ToggleEastLights,"切换东廊照明",27,46,"调暗后远处更难被看见，脚步仍会暴露位置。");
            Desk("Signal control console",new Vector3(25,0,15),4);
            annex.dialLabels=new TextMesh[3];
            for(int i=0;i<3;i++)
            {
                float x=23.7f+i*1.25f;
                Item((AnnexAction)((int)AnnexAction.DialHarbor+i),new[]{"调节内港阀","调节坞池阀","调节外海阀"}[i],new Vector3(x,1.16f,15),blue,"每按一次 E 增加一档，6 后回到 0。",new Vector3(.7f,.3f,.65f));
                Cylinder("Signal hand wheel",new Vector3(x,1.37f,15),.3f,.07f,brass,Quaternion.identity);
                annex.dialLabels[i]=Label(new[]{"INNER 0","DOCK 0","SEA 0"}[i],new Vector3(x,1.9f,15.5f),0,.025f);
            }
            Pedestal(AnnexAction.ConfirmPressure,"确认泄压程序",28,12,"控制台顺序：内港 → 坞池 → 外海。");
            Pedestal(AnnexAction.OpenShortcut,"解锁船坞东侧回程门",22,8,"经东侧长廊返回中央大厅，从大厅南门回船坞。");
            Label("SIGNAL / SAFE",new Vector3(25,2.85f,17.7f),0,.034f);
            layout.eastLights=root.GetComponentsInChildren<Light>().Where(l=>l.transform.position.x>=14 && l.transform.position.z>=18 && l.transform.position.z<50).ToArray();
            // Human-scale details keep enlarged rooms readable without filling the travel lanes.
            foreach(var room in rooms.Where(r=>r.sector>0 && !r.corridor && r.id!="hub"))
            {
                var b=room.bounds;
                Cube(room.id+" / wall service pipe",new Vector3(b.xMin+.45f,2.65f,b.center.y),new Vector3(.12f,.12f,b.height-1),brass,false);
                Cube(room.id+" / threshold stripe",new Vector3(b.center.x,.015f,b.yMin+.8f),new Vector3(b.width-1,.012f,.12f),blue,false);
            }
        }
        private static void Pedestal(AnnexAction action,string label,float x,float z,string description)
        {
            Cube(label+" pedestal",new Vector3(x,.5f,z),new Vector3(.65f,1,.65f),dark);
            Item(action,label,new Vector3(x,1.15f,z),brass,description,new Vector3(.5f,.3f,.5f));
        }
        private static Vector3 P(float x,float z){return new Vector3(x,.03f,z);}
        private static void BuildPatrols()
        {
            var original=layout.dock.threat;
            AddGuard(original,"西廊巡检员",new[]{P(-17,14),P(-17,26),P(-25,26),P(-25,35),P(-25,42),P(-17,42)},new[]{0,1,2,3,4,3,2,1,5,1},new Vector2(-30,6),new Vector2(-14,50),red);
            AddGuard(original,"东廊看守",new[]{P(17,20),P(17,25),P(25,25),P(25,36),P(25,42),P(17,42)},new[]{0,1,2,3,4,5,1},new Vector2(14,18),new Vector2(30,50),blue);
            AddGuard(original,"测潮巡查员",new[]{P(17,53),P(17,62),P(10,62),P(-4,62),P(-4,53)},new[]{0,1,2,3,4},new Vector2(-14,50),new Vector2(30,70),brass);
            layout.guards=root.GetComponentsInChildren<ThreatEncounter>().OrderBy(g=>g.displayName=="西廊巡检员"?0:g.displayName=="东廊看守"?1:2).ToArray();
        }
        public static void Validate()
        {
            PrototypeValidation.Validate();var h=Object.FindFirstObjectByType<HarborLayout>();
            if(h==null || h.rooms.Length!=27 || h.doors.Length!=30 || h.guards.Length!=3)throw new InvalidOperationException("Harbor topology incomplete");
            if(Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None).Length!=4)throw new InvalidOperationException("Harbor needs four guards");
            if(Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).Length!=20)throw new InvalidOperationException("Harbor needs twenty interactions");
            foreach(var door in h.doors)if(h.Room(door.from)==null || h.Room(door.to)==null)throw new InvalidOperationException("Unknown door endpoint");
        }
        [MenuItem("Waterline/16 Build staged harbor")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/Harbor");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Harbor/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Harbor build failed");
        }
    }
}


