using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class AnnexSession : MonoBehaviour
    {
        public DockSession dock;
        public AnnexState State { get; private set; }
        public GameObject[] archiveLocks, signalLocks;
        public GameObject shortcutLock, fuse, signalKey;
        public TextMesh[] dialLabels;
        public Light[] powerLights;
        public bool NoteOpen;
        public string NoteTitle, NoteBody;
        private AnnexState checkpoint;
        private ThreatEncounter[] guards;
        private float previousNoise;
        private int lastStep;
        public ConnectedLayout Layout { get { return GetComponent<ConnectedLayout>(); } }
        public HarborLayout Harbor { get { return GetComponent<HarborLayout>(); } }
        public HarborSupplies Supplies { get { return GetComponent<HarborSupplies>(); } }
        public HarborJournal Journal { get { return GetComponent<HarborJournal>(); } }
        private void Awake() { State=new AnnexState();State.CalibrationRequired=Harbor!=null; }
        private void Start() {guards=FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None);}
        public void BroadcastFootstep(float radius, Vector3 position)
        {
            // Forward only a newly emitted footstep, not every frame of the fading HUD bar.
            if(radius>0 && (previousNoise<=0 || dock.threat.StepSequence!=lastStep))
            {
                if(guards!=null)foreach(var guard in guards)if(guard!=dock.threat)guard.HearNoise(position,radius);
            }
            previousNoise=radius;lastStep=dock.threat.StepSequence;
        }
        private void Update() { Apply(); }
        public string Objective
        {
            get
            {
                if(Harbor!=null)return Harbor.Objective;
                if(!State.PowerRestored)return State.HasFuse?"北楼备用机房：恢复电力":"北楼备件库：寻找熔断器";
                if(!State.HasSignalKey)return "潮汐档案室：钥匙与线索";
                if(!State.RecordRead)return Layout!=null?"东北测潮间：查阅阀位记录":"潮汐档案室：查阅阀位记录";
                return "信号室：设置三阀并确认";
            }
        }
        public bool Perform(AnnexAction action)
        {
            if(dock.InputBlocked)return false;
            if(Harbor!=null && Harbor.Planner!=null){var blocked=Harbor.Planner.BlockedReason(action);if(blocked.Length>0){dock.Announce(blocked);return false;}}
            string deployReason="";
            if(action==AnnexAction.DeployDecoy && (Supplies==null || !Supplies.CanDeploy(out deployReason)))
            {dock.Announce(Supplies==null?"当前区域没有便携诱敌器。":deployReason);return false;}
            if(action==AnnexAction.RingInspectionBell && (Harbor!=null?!Harbor.ScheduleBell():(Layout==null || !Layout.ScheduleBell())))return false;
            if(action==AnnexAction.OpenShortcut && dock.playerTransform.position.z<5.8f)
            {dock.Announce("门栓在北楼信号室内，需从另一侧打开。");return false;}
            bool ok=State.TryApply(action,out var message);if(Harbor!=null)message=LocalizeHarbor(message);dock.Announce(message);
            if(ok && (action==AnnexAction.ReadDutyLog || action==AnnexAction.ReadTideRecord))
            {
                NoteOpen=true;
                NoteTitle=action==AnnexAction.ReadDutyLog?"北楼值班记录":"潮位抄录 / 第七码头";
                NoteBody=action==AnnexAction.ReadDutyLog?
                    "备用电源熔断器已烧毁，替换件放在北侧备件库。\n\n送电后，潮汐档案室电锁会释放。信号室钥匙和潮位抄录都在档案桌上。\n\n去信号室完成三阀泄压，船坞才允许注水。信号室南门可从内侧打开，直接返回绞盘间。\n\n北楼仍有人巡查。值班室可暂避；按 M 查看房间位置。":
                    "今日标定的泄压刻度：\n\n外海：6 档\n坞池：2 档\n内港：4 档\n\n信号控制台从左至右为：内港 → 坞池 → 外海。\n每只旋钮在 0–6 档之间循环。设好后按确认按钮。\n\n记录已抄入路线图，按 M 可回看。";
            }
            if(ok && dock.HasCheckpoint)SaveCheckpoint();
            if(ok && Layout!=null)
            {
                if(action==AnnexAction.ReadDutyLog)
                    NoteBody="备件库提供熔断器，备用机房负责送电。\n\n档案室保存信号钥匙；今日的潮位记录已移交东北测潮间。完成三阀泄压后，返回船坞注水撤离。\n\n西翼维修间与包装库组成橡胶地面的绕行路线。北侧调度台通电后可打开档案室北门，缩短往返。\n\n查验间的延时铃会吸引巡查者；启动后有 3 秒可先离开。值班室和信号室可暂避。按 M 查看房间用途与门锁状态。";
                if(action==AnnexAction.ReadWorkshopManual || action==AnnexAction.ReadArchiveIndex)
                {
                    NoteOpen=true;NoteTitle=action==AnnexAction.ReadWorkshopManual?"维修手册 / 管线与静音通路":"档案移交清单";
                    NoteBody=message+(action==AnnexAction.ReadWorkshopManual?"\n\n控制台每只阀在 0–6 档循环。实际刻度以东北测潮间的今日记录为准。\n\n包装库连接备件库与北回廊，货架可切断视线。绕行较远，但橡胶地面的脚步声更小。":"\n\n先取走本桌钥匙，再经查验间、北回廊去测潮间。读完记录后可从查验间南门进入信号室。\n\n调度室的北门控制台可让档案室成为往返通道。");
                }
                if(action==AnnexAction.TakeSignalKey)dock.Announce("取得信号室钥匙。今日潮位记录在东北测潮间；本桌移交清单说明路线。");
                if(action==AnnexAction.RestorePower)dock.Announce("备用电源恢复，档案室电锁释放。前往档案室取钥匙；调度台也已通电。");
                if(action==AnnexAction.RingInspectionBell)dock.Announce("查验铃将在 3 秒后响起，先从另一扇门离开。");
            }
            if(ok && Harbor!=null)Harbor.AfterAction(action);
            if(ok && Supplies!=null)Supplies.AfterAction(action);
            if(ok && Journal!=null && NoteOpen)Journal.Record(action,NoteTitle,NoteBody);
            Apply();Debug.Log("[Annex] "+action+" | "+ok+" | "+message);return ok;
        }
        public void SaveCheckpoint() {checkpoint=State.Copy();}
        public string BlockedReason(AnnexAction action){if(Harbor!=null && Harbor.Planner!=null){var blocked=Harbor.Planner.BlockedReason(action);if(blocked.Length>0)return blocked;}var reason=State.BlockedReason(action);return Harbor!=null?LocalizeHarbor(reason):reason;}
        private static string LocalizeHarbor(string text){return text.Replace("北侧备件库","大厅西侧零件库").Replace("北楼备件库","西侧零件库").Replace("潮汐档案室","航务档案室");}
        public void RestoreCheckpoint() {State=checkpoint==null?new AnnexState():checkpoint.Copy();State.CalibrationRequired=Harbor!=null;NoteOpen=false;if(Layout!=null)Layout.ResetTransient();if(Harbor!=null)Harbor.ResetTransient();if(Supplies!=null)Supplies.ResetTransient();if(Journal!=null)Journal.Close();Apply();}
        public void Apply()
        {
            if(State==null)return;
            foreach(var gate in archiveLocks)if(gate!=null)gate.SetActive(!State.PowerRestored);
            foreach(var gate in signalLocks)if(gate!=null)gate.SetActive(!State.HasSignalKey);
            if(shortcutLock!=null)shortcutLock.SetActive(!State.ShortcutOpen);
            if(Layout!=null && Layout.serviceGate!=null)Layout.serviceGate.SetActive(!State.ServiceLoopOpen);
            if(fuse!=null)fuse.SetActive(!State.HasFuse && !State.PowerRestored);
            if(signalKey!=null)signalKey.SetActive(!State.HasSignalKey);
            for(int i=0;i<dialLabels.Length;i++)if(dialLabels[i]!=null)dialLabels[i].text=new[]{"INNER ","DOCK ","SEA "}[i]+new[]{State.Harbor,State.Dock,State.Sea}[i];
            foreach(var lamp in powerLights)if(lamp!=null)lamp.color=State.PowerRestored?new Color(.5f,.9f,.8f):new Color(1,.35f,.12f);
        }
    }
}
