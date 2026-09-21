using UnityEngine;
using UnityEngine.SceneManagement;
using Waterline.Core;

namespace Waterline
{
    public sealed class DockSession : MonoBehaviour
    {
        public DockTuning tuning;
        public Transform water;
        public Transform boat;
        public Transform bridge;
        public Transform serviceDoor;
        public bool explorationLayout;
        public Transform playerTransform;
        public ThreatEncounter threat;
        public AnnexSession annex;
        public Vector2 dockScale=Vector2.one;
        public Vector3 DockToWorld(Vector3 p) {return new Vector3(p.x*dockScale.x,p.y,6+(p.z-6)*dockScale.y);}
        public Vector3 WorldToDock(Vector3 p) {return new Vector3(p.x/dockScale.x,p.y,6+(p.z-6)/dockScale.y);}
        public bool Caught { get; private set; }
        public bool HasCheckpoint { get { return checkpoint != null; } }
        public TidalRoutePlanner Planner { get { return annex!=null && annex.Harbor!=null?annex.Harbor.Planner:null; } }
        public bool PlayerSafeForFlood { get { return playerTransform != null && (Planner!=null?Planner.SafeToStart(playerTransform.position):playerTransform.position.y >= -0.25f && Mathf.Abs(WorldToDock(playerTransform.position).x) < 10.6f); } }
        private DockState checkpoint;
        private Vector3 checkpointPosition;
        public float dryWaterHeight = -3.3f;
        public float fullWaterHeight = -0.6f;
        public float dryBoatHeight = -2.5f;
        public float fullBoatHeight = -0.05f;
        public DockState State { get; private set; }
        public string LastMessage { get; private set; }
        public bool DebugVisible { get; set; }
        public bool MapVisible { get; set; }
        public bool HideGameplayHud { get { return MapVisible || (annex != null && annex.NoteOpen); } }
        public bool Paused { get; set; }
        public bool InputBlocked { get { return Caught || Paused || DebugVisible || MapVisible || (annex!=null && annex.NoteOpen) || State.Phase == DockPhase.Escaped; } }
        public string Objective
        {
            get
            {
                if(annex!=null && annex.Harbor!=null && annex.State!=null)return annex.Harbor.Objective;
                if (State.Phase == DockPhase.Escaped) return "撤离完成";
                if (threat != null && threat.FloodRequested && State.Phase == DockPhase.Dry)
                    return threat.EvacuationComplete ? "回到上层房间，准备自动注水" : "警报疏散：避让东侧楼梯";
                if (State.Phase == DockPhase.Flooded) return "前往东侧黄色控制台登艇";
                if (State.Phase == DockPhase.Filling) return "下层封闭，等待检修艇浮起";
                if (!explorationLayout) return "设备复航测试";
                if (!State.HasPin && !State.PinInstalled) return "西梯下楼，寻找工具间联轴销";
                if (!State.GateUnlocked) return "穿过坞底，经东梯前往绞盘间";
                if (!State.BridgeDeployed) return "展开检修桥，接回西侧水控室";
                if (!State.PinInstalled) return "回西侧控制台安装联轴销";
                if(annex!=null && !annex.State.PressureBalanced)return annex.Objective;
                return "长按启动注水；下层将封闭";
            }
        }
        private float bridgeProgress;
        private float doorProgress;
        private Vector3 doorClosed;

        private void Awake()
        {
            State = new DockState();
            LastMessage = explorationLayout ? "联轴销存放在下层工具间。从左侧蓝色楼梯下去；按 M 查看两层路线。" :
                "先取工作台上的联轴销。WASD 移动，鼠标观察，E 交互。";
            if (serviceDoor != null) doorClosed = serviceDoor.position;
            ApplyPresentation();
        }

        private void Update()
        {
            var previousPhase = State.Phase;
            if (!InputBlocked) State.Tick(Time.deltaTime, tuning.fillingSeconds);
            if (previousPhase == DockPhase.Filling && State.Phase == DockPhase.Flooded)
                LastMessage = annex!=null && annex.Harbor!=null && annex.Harbor.LowerDeck!=null?"水位到顶，下层维护舱已淹没。大厅浮台已升起，可从安全大厅跨到东侧，再经东门到黄色登艇台。":"水位已到浮航线。前往东侧靠坞边的黄色登艇控制台，按 E 撤离。";
            if (!InputBlocked)
            {
                bridgeProgress = Mathf.MoveTowards(bridgeProgress, State.BridgeDeployed && (Planner==null || !Planner.BridgeClosed) ? 1 : 0, Time.deltaTime * 0.8f);
                bool doorOpen = explorationLayout ? State.GateUnlocked : State.PinInstalled;
                doorProgress = Mathf.MoveTowards(doorProgress, doorOpen ? 1 : 0, Time.deltaTime);
            }
            if(previousPhase==DockPhase.Filling && State.Phase==DockPhase.Flooded && Planner!=null)LastMessage="水位到顶。"+Planner.FloodRoute;
            ApplyPresentation();
            Cursor.lockState = InputBlocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = InputBlocked;
        }

        private void ApplyPresentation()
        {
            if (water != null) SetHeight(water, Mathf.Lerp(dryWaterHeight, fullWaterHeight, State.WaterProgress));
            if (boat != null) SetHeight(boat, Mathf.Lerp(dryBoatHeight, fullBoatHeight, State.WaterProgress));
            if (bridge != null)
            {
                bridge.localScale = new Vector3(Mathf.Max(0.02f, bridgeProgress * 10f*dockScale.x), 0.24f, 2f*dockScale.y);
                bridge.position = DockToWorld(new Vector3(-5f + bridgeProgress * 5f, -0.12f, 3.5f));
                var collider = bridge.GetComponent<Collider>();
                if (collider != null) collider.enabled = bridgeProgress >= 0.99f;
            }
            if (serviceDoor != null) serviceDoor.position = doorClosed + Vector3.up * (doorProgress * 3.2f);
        }

        private static void SetHeight(Transform target, float value)
        {
            var position = target.position;
            position.y = value;
            target.position = position;
        }

        public string BlockedReason(DockAction action)
        {
            if(action==DockAction.StartFlood && annex!=null && !annex.State.PressureBalanced)
                return "北楼泄压联锁未解除。"+annex.Objective+"。";
            string reason = State.BlockedReason(action);
            if (reason.Length > 0)
            {
                if (explorationLayout && action == DockAction.InstallPin && !State.HasPin && !State.PinInstalled && State.Phase != DockPhase.Escaped)
                    return annex!=null && annex.Harbor!=null && annex.Harbor.LowerDeck!=null && Planner==null?"联轴销已移交缆具间。经西侧蓝色楼梯下楼，再走工具间北门；M 查看下层路线。":"联轴销在下层工具间。经西侧蓝色楼梯下去，按 M 查看路线。";
                return reason;
            }
            if (action == DockAction.StartFlood && bridgeProgress < 0.99f && State.BridgeDeployed)
                return "请等待检修桥完全展开。";
            if (action == DockAction.StartFlood && explorationLayout &&
                !PlayerSafeForFlood)
                return "必须回到上层水控室才能启动注水。";
            return "";
        }

        public bool Perform(DockAction action)
        {
            if(Caught)return false;
            string reason = BlockedReason(action);
            if (reason.Length > 0) { LastMessage = reason; return false; }
            if(action == DockAction.StartFlood && threat != null && !threat.EvacuationComplete)
            {
                if(!threat.FloodRequested)threat.RequestFlood();
                else LastMessage="警报疏散中，无需再次操作。请避让东梯并留在上层。";
                return true;
            }
            string message;
            bool result = State.TryApply(action, out message);
            if (result && explorationLayout)
            {
                if (action == DockAction.TakePin) message = "取得联轴销。出工具间，沿船体绕到东侧，走橙色楼梯上绞盘间。";
                if(action==DockAction.TakePin && annex!=null && annex.Harbor!=null && annex.Harbor.LowerDeck!=null)message="取得联轴销。可经北侧维护舱绕行，或返回坞底；最后从橙色东梯上绞盘间。";
                if (action == DockAction.UnlockGate) message = "坞门锁已解除，南侧隔离门升起。展开检修桥，即可直接回到西侧。";
                if (action == DockAction.StartFlood) message = "注水启动，两侧下层入口已封闭。可经检修桥或南栈道前往登艇区。";
                if (action == DockAction.InstallPin && State.GateUnlocked && State.BridgeDeployed)
                    message = annex!=null && !annex.State.PressureBalanced?"船坞修复完成，北楼入口已开放。"+annex.Objective+"。":"泵已修复，所有条件就绪。注水后下层将不可进入；确认后长按注水控制台。";
            }
            LastMessage = message;
            if(result && Planner!=null && action==DockAction.TakePin)LastMessage="取得联轴销。可直接经坞底上东梯；也可进工具间北门，寻找应急物资并准备浮台近路。";
            if(result && Planner!=null && action==DockAction.StartFlood)LastMessage="注水启动，检修桥收回为船只让出航道。"+Planner.FloodRoute;
            if(result && threat != null && HasCheckpoint && State.Phase == DockPhase.Dry)
                SaveCheckpoint(DockToWorld(State.PinInstalled ? new Vector3(-8,.08f,1.6f) : new Vector3(10,.08f,-3.8f)),false);
            Debug.Log("[Waterline] " + action + " | " + result + " | " + message);
            return result;
        }

        public void Announce(string message) { LastMessage=message; }

        public void SaveCheckpoint(Vector3 position, bool announce=true)
        {
            if(State.Phase!=DockPhase.Dry)return;
            checkpoint=State.Copy();checkpointPosition=position;
            if(annex!=null)annex.SaveCheckpoint();
            if(announce)LastMessage="已抵达东侧上层，建立检查点。被拦截后可按 R 重试，保留机关进度。";
        }

        public void CapturePlayer()
        {
            if(State.Phase==DockPhase.Escaped || Caught)return;
            Caught=true;Paused=false;MapVisible=false;DebugVisible=false;
            LastMessage="被拖链工拦截。轻步通过，利用船体或设备遮挡视线。";
        }

        public void RetryCheckpoint()
        {
            State=checkpoint==null?new DockState():checkpoint.Copy();
            Caught=false;Paused=false;MapVisible=false;DebugVisible=false;
            bridgeProgress=State.BridgeDeployed?1:0;doorProgress=(explorationLayout?State.GateUnlocked:State.PinInstalled)?1:0;
            var player=FindFirstObjectByType<FirstPersonController>();
            player.ResetPose(checkpoint==null?DockToWorld(new Vector3(-8,.08f,-4)):checkpointPosition,checkpoint==null?-90:0);
            foreach(var item in FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(item.action==DockAction.TakePin)item.gameObject.SetActive(!State.HasPin && !State.PinInstalled);
            ApplyPresentation();
            if(annex!=null)annex.RestoreCheckpoint();
            foreach(var encounter in FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))encounter.CountRetry();
            var routes=FindFirstObjectByType<DockRoutes>();if(routes!=null)routes.ResetAttemptRoutes();
            LastMessage=checkpoint==null?"回到起点。工具间和西侧水控室为安全区，Ctrl 轻步减少声音。":"已恢复检查点与机关进度。拖链工回到下层，短暂宽限后开始巡查。";
        }

        public void Restart()
        {
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
