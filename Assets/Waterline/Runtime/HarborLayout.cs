using System;
using System.Collections.Generic;
using UnityEngine;
using Waterline.Core;
namespace Waterline
{
    public enum HarborGateRule { Open, DockRepaired, Power, SignalKey, Dispatch, DockReturn, WorkshopReturn, ObservationReturn, DryDock, FloatLink }
    [Serializable] public sealed class HarborRoom
    {
        public string id, title, purpose;
        public Rect bounds;
        public int level, sector;
        public bool safe, corridor;
        public GroundKind ground;
    }
    [Serializable] public sealed class HarborDoor
    {
        public string from, to, hint;
        public Vector3 position;
        public bool vertical;
        public HarborGateRule rule;
        public GameObject blocker;
        public TextMesh sign;
    }
    public sealed class HarborLayout : MonoBehaviour
    {
        public static HarborLayout Active { get; private set; }
        public DockSession dock;
        public AnnexSession annex;
        public HarborRoom[] rooms;
        public HarborDoor[] doors;
        public ThreatEncounter[] guards;
        public Light[] eastLights;
        public Transform bell;
        public GameObject calibrationPlate;
        public HashSet<string> Visited { get; }=new HashSet<string>();
        public string CurrentRoom { get; private set; }
        public FloodedLowerDeck LowerDeck { get { return GetComponent<FloodedLowerDeck>(); } }
        public TidalRoutePlanner Planner { get { return GetComponent<TidalRoutePlanner>(); } }
        public int Revision { get; private set; }
        private AudioSource bellSource;
        private float bellTimer=-1, bellCooldown;
        private void Awake(){Active=this;}
        private void Start()
        {
            annex.State.CalibrationRequired=true;
            bellSource=bell.gameObject.AddComponent<AudioSource>();bellSource.spatialBlend=1;bellSource.maxDistance=40;
            bellSource.volume=.6f;bellSource.clip=DockAudio.Pulse("Harbor delayed bell",1.3f,640,true);
            Apply();dock.Announce(LowerDeck!=null && Planner==null?"检修艇困在干坞。沿蓝色西梯下楼，经工具间北门进入缆具间取联轴销。M 可切换上下层路线。":"检修艇困在干坞。先沿蓝色西梯去下层工具间取联轴销；北楼入口将在修泵后开放。M 查看当前区域。");
        }
        private void OnDestroy(){if(Active==this)Active=null;if(bellSource!=null && bellSource.clip!=null)Destroy(bellSource.clip);}
        private void Update()
        {
            Apply();
            if(dock.InputBlocked)return;
            var room=RoomAt(dock.playerTransform.position);
            if(room!=null){CurrentRoom=room.id;if(Visited.Add(room.id))Revision++;}
            bellCooldown=Mathf.Max(0,bellCooldown-Time.deltaTime);
            if(bellTimer<0)return;
            bellTimer-=Time.deltaTime;if(bellTimer>0)return;bellTimer=-1;
            bellSource.Play();foreach(var guard in guards)guard.HearNoise(bell.position,70);
            dock.Announce("查验铃响起。巡查者正在调查，可以沿另一侧通道绕开。");
        }
        public bool ScheduleBell()
        {
            if(bellCooldown>0){dock.Announce("查验铃正在复位，请稍候。");return false;}
            bellTimer=3;bellCooldown=12;return true;
        }
        public void ResetTransient(){bellTimer=-1;bellCooldown=0;Apply();}
        public bool IsOpen(HarborGateRule rule)
        {
            var s=annex.State;if(s==null)return rule==HarborGateRule.Open;
            switch(rule)
            {
                case HarborGateRule.DryDock:return dock.State.Phase==DockPhase.Dry;
                case HarborGateRule.FloatLink:return LowerDeck!=null && LowerDeck.Raised;
                case HarborGateRule.DockRepaired:return dock.State.PinInstalled;
                case HarborGateRule.Power:return s.PowerRestored;
                case HarborGateRule.SignalKey:return s.HasSignalKey;
                case HarborGateRule.Dispatch:return s.ServiceLoopOpen;
                case HarborGateRule.DockReturn:return s.ShortcutOpen;
                case HarborGateRule.WorkshopReturn:return s.WorkshopLoopOpen;
                case HarborGateRule.ObservationReturn:return s.ObservationLoopOpen;
                default:return true;
            }
        }
        public void Apply()
        {
            if(annex.State==null)return;
            if(calibrationPlate!=null)calibrationPlate.SetActive(!annex.State.HasCalibrationPlate && !annex.State.GaugeAligned);
            foreach(var door in doors)
            {
                bool open=IsOpen(door.rule);if(door.blocker!=null)door.blocker.SetActive(!open);
                if(door.sign!=null)door.sign.color=open?new Color(.45f,.9f,.76f):new Color(1,.64f,.3f);
            }
            for(int i=0;i<guards.Length;i++)
            {
                bool awake=i==0?dock.State.PinInstalled:i==1?annex.State.PowerRestored:annex.State.HasSignalKey;
                var g=guards[i];bool changed=g.Available!=awake;g.Available=awake;
                if(changed && awake)g.ResetEncounter();
                if(g.GetComponent<CharacterController>().enabled!=awake)g.GetComponent<CharacterController>().enabled=awake;
                if(g.body!=null && g.body.gameObject.activeSelf!=awake)g.body.gameObject.SetActive(awake);
            }
            foreach(var lamp in eastLights)if(lamp!=null)lamp.intensity=annex.State.EastLightsDim?.22f:2.6f;
        }
        public HarborRoom RoomAt(Vector3 p)
        {
            if(p.y>=-.6f && LowerDeck!=null && LowerDeck.Opening.Contains(new Vector2(p.x,p.z)))return Room("float_link");
            int level=p.y<-.6f?-1:0;foreach(var r in rooms)if(r.level==level && r.bounds.Contains(new Vector2(p.x,p.z)))return r;
            return null;
        }
        public HarborRoom Room(string id){foreach(var r in rooms)if(r.id==id)return r;return null;}
        public bool SafeAt(Vector3 p){var r=RoomAt(p);return r!=null && r.safe;}
        public GroundKind GroundAt(Vector3 p){var r=RoomAt(p);return r==null?GroundKind.Concrete:r.ground;}
        public float SightMultiplier(Vector3 p){return annex.State.EastLightsDim && p.x>=14 && p.z>=18 && p.z<50?.6f:1;}
        public string GoalRoom
        {
            get
            {
                var d=dock.State;var s=annex.State;
                if(d.Phase==DockPhase.Escaped)return "east_dock";
                if(d.Phase==DockPhase.Filling || (dock.threat.FloodRequested && d.Phase==DockPhase.Dry))return "west_dock";
                if(d.Phase==DockPhase.Flooded)return "east_dock";
                if(!d.HasPin && !d.PinInstalled)return LowerDeck!=null && Planner==null?"rigging_store":"lower_tools";
                if(!d.GateUnlocked || !d.BridgeDeployed)return "east_dock";
                if(!d.PinInstalled)return "west_dock";
                if(!s.PowerRestored)return s.HasFuse?"power":"spares";
                if(!s.HasSignalKey)return "archive";
                if(!s.GaugeAligned)return s.HasCalibrationPlate?"tide":"instruments";
                if(!s.RecordRead)return "tide";
                if(!s.PressureBalanced)return "signal";
                return "west_dock";
            }
        }
        public string Objective
        {
            get
            {
                var s=annex.State;var d=dock.State;
                if(d.Phase==DockPhase.Escaped)return "检修艇已离坞";
                if(d.Phase==DockPhase.Flooded)return "东侧登艇台 · 按 E 撤离";
                if(d.Phase==DockPhase.Filling)return "留在上层 · 等待检修艇浮起";
                if(dock.threat.FloodRequested)return "留在水控室 · 等待东梯疏散";
                if(!d.HasPin && !d.PinInstalled)return LowerDeck!=null && Planner==null?"01 缆具间 · 经工具间北门取联轴销":"01 工具间 · 经西梯取联轴销";
                if(!d.GateUnlocked)return "02 绞盘间 · 经东梯解除坞门锁";
                if(!d.BridgeDeployed)return "02 绞盘间 · 展开检修桥";
                if(!d.PinInstalled)return "03 水控室 · 过桥安装联轴销";
                if(!s.PowerRestored)return s.HasFuse?"05 备用机房 · 安装熔断器":"04 零件库 · 经大厅西门取熔断器";
                if(!s.HasSignalKey)return "06 航务档案室 · 取信号钥匙";
                if(!s.GaugeAligned)return s.HasCalibrationPlate?"08 测潮大厅 · 安装标尺":"07 仪器间 · 取得测潮标尺";
                if(!s.RecordRead)return "08 测潮大厅 · 读取今日刻度";
                if(!s.PressureBalanced)return "09 信号室 · 完成三阀泄压";
                return "10 水控室 · 返回船坞长按注水";
            }
        }
        public string RouteHint
        {
            get
            {
                if(Planner!=null && dock.State.Phase!=DockPhase.Dry)return "检修桥已收回，让出船只航道。"+Planner.FloodRoute;
                if(Planner!=null && GoalRoom=="lower_tools")return "西梯下楼取联轴销。工具间北门是可选探索：应急物资和浮台试压，提前准备注水后的撤离近路。";
                if(Planner!=null && GoalRoom=="east_dock")return "取销后可直接经坞底去橙色东梯；工具间北门的维护舱是可选支线。先在绞盘间解锁、展桥，再回西侧修泵。";
                switch(GoalRoom)
                {
                    case "rigging_store":return "蓝色西梯下楼，进入工具间；沿北墙 RIGGING / PIN 标牌进入缆具间，工作台上取联轴销。下层维护舱可从北侧绕向东梯。";
                    case "lower_tools":return "开场先认蓝色西梯。下层取得联轴销后，再沿船体北侧绕去橙色东梯。";
                    case "east_dock":return dock.State.Phase==DockPhase.Flooded?(LowerDeck!=null?"大厅浮台已升起，可从安全大厅跨到东侧，经已解锁东门去黄色登艇台；原检修桥与南栈道仍可用。":"经过上层检修桥或南栈道，到东侧靠船的黄色登艇台。"):LowerDeck!=null?"从缆具间北侧绕行维护舱，或返回坞底，最后沿橙色东梯上绞盘间。":"出工具间绕过船体，沿橙色东梯上楼。";
                    case "west_dock":return "检修桥连接东西两侧。北楼返程经过中央值守大厅，再走大厅南门。";
                    case "spares":return "修泵后进北楼大厅，选择蓝色 WEST / PARTS 西门。沿西侧服务廊到零件库。";
                    case "power":return "带熔断器回中央大厅，走北墙左侧 POWER 门进入备用机房。";
                    case "archive":return "送电打开机房东门。穿过泵压厅，向北进入航务档案室。";
                    case "instruments":return "取得钥匙后，从档案室东门进入巡检廊，转入东侧长廊向北，经北回廊到仪器间。";
                    case "tide":return "仪器间东门通向测潮大厅。先把标尺装到测潮架，再读取记录台。";
                    case "signal":return "从测潮大厅南门下到东侧长廊，一直向南到信号室。台面顺序是内港、坞池、外海。";
                    default:return "按房间编号辨认主线；黄色门牌说明当前锁的条件。";
                }
            }
        }
        public void AfterAction(AnnexAction action)
        {
            Apply();Revision++;
            if(action==AnnexAction.ReadFloodPlan && LowerDeck!=null)LowerDeck.ReadPlan();
            if(Planner!=null)Planner.Apply();
            if(action==AnnexAction.RestAtBench){dock.SaveCheckpoint(new Vector3(-10,.08f,44),false);dock.Announce("已在维修休息室记录进度。被拦截后可从这里重试。");}
            if(action==AnnexAction.ReadDutyLog || action==AnnexAction.ReadHarborPlan)
            {
                annex.NoteOpen=true;annex.NoteTitle="第七码头 / 复航总图";
                annex.NoteBody="检修艇是最终出口。修好船坞后，再完成北楼泄压。\n\n第一段：西梯下层工具间取销 → 东梯绞盘间解锁展桥 → 西侧水控室修泵。\n\n第二段：中央大厅西门 → 零件库取熔断器 → 回大厅北墙备用机房送电。\n\n第三段：经泵压厅去档案室取钥匙 → 东侧长廊、北回廊 → 仪器间与测潮大厅。\n\n最后：信号室三阀泄压 → 回中央大厅 → 船坞注水、登艇。\n\n门牌颜色：青色可通行，黄色需要条件。维修与观测捷径需从远端开启。地图只突出已探索区域与当前目标。";
                if(LowerDeck!=null && Planner==null)annex.NoteBody=annex.NoteBody.Replace("西梯下层工具间取销","西梯下楼，经工具间北门到缆具间取销")+"\n\n注水后下层维护舱被淹没，大厅浮台升起成为跨井通路。";
            }
            if(Planner!=null && (action==AnnexAction.ReadDutyLog || action==AnnexAction.ReadHarborPlan))annex.NoteBody+="\n\n大厅泵组隔开东西侧。注水时检修桥自动收回，为船只让出航道。干坞时可在下层维护舱试压浮台，准备跨厅近路；未准备则走南栈道。";
            if(action==AnnexAction.ReadWorkshopManual)
            {
                annex.NoteOpen=true;annex.NoteTitle="工装间 / 静音作业须知";
                annex.NoteBody="蓝色西廊通向零件库，熔断器在库内工作台上。\n\n西廊铺设橡胶，轻步声更小。北侧机修车间的货箱可以切断视线，车间回程门可从内侧开启，接回备用机房。\n\n房间越大，越要利用设备转角。开地图或读记录会暂停；中央值守大厅可安全整理路线。";
            }
            if(action==AnnexAction.ReadArchiveIndex)
            {
                annex.NoteOpen=true;annex.NoteTitle="航务档案 / 测潮移交清单";
                annex.NoteBody="信号钥匙留在本桌。拿到钥匙，档案室东门与大厅东区门会解锁。\n\n今日刻度需重新标定：先沿东侧长廊去北回廊，从仪器间取标尺，再由仪器间东门进入测潮大厅。\n\n测潮架完成标定后，读取三处水域刻度。信号室位于东侧长廊最南端。\n\n调度室可以开启档案室北门；巡检廊有返回泵压厅的内侧门栓。开启后可缩短返程。";
            }
            if(action==AnnexAction.TakeSignalKey || action==AnnexAction.RestorePower || action==AnnexAction.OpenShortcut)
                dock.Announce(Objective+"。"+RouteHint);
            if(action==AnnexAction.ReadArchiveIndex)
            {
                annex.NoteBody=annex.NoteBody.Replace("巡检廊有返回泵压厅的内侧门栓。开启后可缩短返程。","北回廊的观测门栓同时打开档案东廊北门与巡检廊通往泵压厅的门，开启后可缩短返程。");
                if(GetComponent<HarborReturnLink>()!=null)annex.NoteBody+="\n\n信号钥匙也开放西侧服务廊北端的门。观测回程门由同一处门栓联动，沿青色管线可辨认它们之间的连接。";
            }
            if(action==AnnexAction.TakeFuse)dock.Announce("取得熔断器。回中央大厅，从北墙左侧进入备用机房；车间回程门也可接近机房侧门。");
            if(action==AnnexAction.RingInspectionBell)dock.Announce("查验铃将在 3 秒后响起，先沿其他通道离开。");
        }
    }
}
