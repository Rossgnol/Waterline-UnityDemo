namespace Waterline.Core
{
    public enum AnnexAction { ReadDutyLog, TakeFuse, RestorePower, ReadTideRecord, TakeSignalKey, DialHarbor, DialDock, DialSea, ConfirmPressure, OpenShortcut, ReadWorkshopManual, ReadArchiveIndex, OpenServiceLoop, RingInspectionBell, OpenWorkshopLoop, OpenObservationLoop, TakeCalibrationPlate, AlignTideGauge, ToggleEastLights, ReadHarborPlan, RestAtBench, ReadSupplyManifest, DialSupplyRepair, DialSupplyInspection, DialSupplyTide, OpenSupplyCabinet, DeployDecoy, ReadFloodPlan, TakeLowerSupplies, ToggleFloatIntake, ToggleFloatDrain, TestFloatCircuit }

    // The record supplies the values; the signal desk supplies the order: harbor, dock, sea.
    public sealed class AnnexState
    {
        public bool LogRead, HasFuse, PowerRestored, RecordRead, HasSignalKey, PressureBalanced, ShortcutOpen;
        public int Harbor, Dock, Sea;
        public bool ManualRead, IndexRead, ServiceLoopOpen;
        public bool CalibrationRequired, HasCalibrationPlate, GaugeAligned, WorkshopLoopOpen, ObservationLoopOpen, EastLightsDim, PlanRead;
        public bool SupplyManifestRead, SupplyCabinetOpen;
        public bool FloodPlanRead;
        public bool LowerSuppliesTaken, FloatIntakeOpen, FloatDrainClosed, FloatPrepared;
        public int SupplyRepair, SupplyInspection, SupplyTide, DecoyCharges;
        public AnnexState Copy() { return (AnnexState)MemberwiseClone(); }
        public string BlockedReason(AnnexAction action)
        {
            if(action==AnnexAction.TakeLowerSupplies && LowerSuppliesTaken)return "应急物资已取走。";
            if(action>=AnnexAction.ToggleFloatIntake && action<=AnnexAction.TestFloatCircuit && FloatPrepared)return "浮台试压已通过，阀位已锁定。注水后自动升起。";
            if(action==AnnexAction.TestFloatCircuit)return !FloatIntakeOpen?"压力为零：沿蓝色管线打开进水阀。":!FloatDrainClosed?"压力流失：沿橙色旁通管关闭排水阀，再试压。":"";
            if(action>=AnnexAction.DialSupplyRepair && action<=AnnexAction.OpenSupplyCabinet && SupplyCabinetOpen)return "物资柜已经领取，无需再次调整。";
            if(action==AnnexAction.DeployDecoy && DecoyCharges<=0)return "没有诱敌器。工装间的领用单提供机修车间物资柜的线索。";
            if(action==AnnexAction.TakeFuse && (HasFuse || PowerRestored))return "熔断器已取走。";
            if(action==AnnexAction.RestorePower)return PowerRestored?"备用电源已恢复。":!HasFuse?"缺少熔断器。北侧备件库有一枚可用备件。":"";
            if(action==AnnexAction.TakeSignalKey && HasSignalKey)return "信号室钥匙已取走。";
            if(action>=AnnexAction.DialHarbor && action<=AnnexAction.ConfirmPressure)
            {
                if(!PowerRestored)return "先在备用机房恢复电力。";
                if(!HasSignalKey)return "先从潮汐档案室取得信号室钥匙。";
                if(PressureBalanced)return "泄压程序已锁定，无需再调整。";
            }
            if(action==AnnexAction.OpenShortcut && ShortcutOpen)return "回程门已经打开。";
            if(action==AnnexAction.OpenServiceLoop)return !PowerRestored?"调度台未通电，先恢复备用电源。":ServiceLoopOpen?"档案室北门已打开。":"";
            if(action==AnnexAction.ReadTideRecord && CalibrationRequired && !GaugeAligned)return "测潮架缺少标尺。先从相邻仪器间取得标尺，装入测潮架。";
            if(action==AnnexAction.TakeCalibrationPlate && (HasCalibrationPlate || GaugeAligned))return "标尺已经取走。";
            if(action==AnnexAction.AlignTideGauge)return GaugeAligned?"测潮架已完成标定。":!PowerRestored?"测潮架没有供电。":!HasCalibrationPlate?"缺少标尺，存放在北侧仪器间。":"";
            if(action==AnnexAction.OpenWorkshopLoop && WorkshopLoopOpen)return "维修回程门已打开。";
            if(action==AnnexAction.OpenObservationLoop && ObservationLoopOpen)return "观测回程门已打开。";
            if(action==AnnexAction.ToggleEastLights && !PowerRestored)return "照明控制尚未供电。";
            return "";
        }
        public bool TryApply(AnnexAction action, out string message)
        {
            message=BlockedReason(action);if(message.Length>0)return false;
            switch(action)
            {
                case AnnexAction.TakeLowerSupplies: LowerSuppliesTaken=true;DecoyCharges++;message="取得一枚应急诱敌器。进入北楼巡查区后，按 Q 部署。";break;
                case AnnexAction.ToggleFloatIntake: FloatIntakeOpen=!FloatIntakeOpen;message=FloatIntakeOpen?"进水阀已开。查看排水旁通，再到浮台井试压。":"进水阀已关，浮台没有供水。";break;
                case AnnexAction.ToggleFloatDrain: FloatDrainClosed=!FloatDrainClosed;message=FloatDrainClosed?"排水旁通已关，可在浮台井试压。":"排水旁通已开，试压水会流走。";break;
                case AnnexAction.TestFloatCircuit: FloatPrepared=true;message="试压通过，浮台已准备。注水后可跨过大厅泵组，避开南侧栈道绕行。";break;
                case AnnexAction.ReadFloodPlan: FloodPlanRead=true;message="已抄录注水前后的通路变化。J 可回看。";break;
                case AnnexAction.ReadSupplyManifest: SupplyManifestRead=true;message="领用单已收入随身记录。机修车间物资柜按部门输入余量，按 J 可回看。";break;
                case AnnexAction.DialSupplyRepair: SupplyRepair=(SupplyRepair+1)%5;message="机修余量："+SupplyRepair+"。";break;
                case AnnexAction.DialSupplyInspection: SupplyInspection=(SupplyInspection+1)%5;message="查验余量："+SupplyInspection+"。";break;
                case AnnexAction.DialSupplyTide: SupplyTide=(SupplyTide+1)%5;message="测潮余量："+SupplyTide+"。";break;
                case AnnexAction.OpenSupplyCabinet:
                    if(SupplyRepair!=3 || SupplyInspection!=1 || SupplyTide!=4){message="物资柜校验失败。输入各部门的剩余数量；领用单在工装间。";return false;}
                    SupplyCabinetOpen=true;DecoyCharges+=2;message="取得两枚发条诱敌器。到巡查区按 Q 放在脚边，3 秒后发声。先离开，利用遮挡绕行。";break;
                case AnnexAction.DeployDecoy: DecoyCharges--;message="诱敌器已放置，3 秒后发声。剩余 "+DecoyCharges+" 枚。";break;
                case AnnexAction.ReadDutyLog: LogRead=true;message="值班记录：备用电源缺熔断器，备件在北库。送电后去档案室取信号钥匙，按潮位记录完成泄压，船坞才允许注水。";break;
                case AnnexAction.TakeFuse: HasFuse=true;message="取得熔断器。回南侧备用机房，装入配电柜。";break;
                case AnnexAction.RestorePower: HasFuse=false;PowerRestored=true;message="北楼电源恢复，潮汐档案室电锁释放。前往档案室取钥匙，查阅潮位记录。";break;
                case AnnexAction.ReadTideRecord: RecordRead=true;message="潮位记录：外海 6 档，坞池 2 档，内港 4 档。信号台要求从左至右按“内港 → 坞池 → 外海”输入；M 可回看记录。";break;
                case AnnexAction.TakeSignalKey: HasSignalKey=true;message="取得信号室钥匙。东南信号室现可进入；档案桌上还有泄压所需的潮位记录。";break;
                case AnnexAction.DialHarbor: Harbor=(Harbor+1)%7;message="内港刻度："+Harbor+"。";break;
                case AnnexAction.DialDock: Dock=(Dock+1)%7;message="坞池刻度："+Dock+"。";break;
                case AnnexAction.DialSea: Sea=(Sea+1)%7;message="外海刻度："+Sea+"。";break;
                case AnnexAction.ConfirmPressure:
                    if(Harbor!=4 || Dock!=2 || Sea!=6){message="泄压校验未通过。请对照潮位记录，并留意控制台的输入顺序。";return false;}
                    PressureBalanced=true;message="泄压程序通过。回程门可从信号室内打开，返回船坞完成注水撤离。";break;
                case AnnexAction.OpenShortcut: ShortcutOpen=true;message="已打开通向东侧绞盘间的回程门。";break;
                case AnnexAction.ReadWorkshopManual: ManualRead=true;message="维修手册：内港 → 坞池 → 外海是管线顺序。西翼铺有橡胶，可轻步绕过中央巡逻。";break;
                case AnnexAction.ReadArchiveIndex: IndexRead=true;message="档案移交：今日潮位记录在东北测潮间，信号钥匙仍在本桌。北侧调度室能远程开启档案室北门。";break;
                case AnnexAction.OpenServiceLoop: ServiceLoopOpen=true;message="档案室北门已开启。现在可以穿过档案室往返机房与北回廊。";break;
                case AnnexAction.RingInspectionBell: message="查验铃已响。巡查者会调查铃声，可趁机从另一扇门绕行。";break;
                case AnnexAction.OpenWorkshopLoop: WorkshopLoopOpen=true;message="维修回程门已打开，可以从车间直接回到中央值守大厅。";break;
                case AnnexAction.OpenObservationLoop: ObservationLoopOpen=true;message="观测回程门已打开，可经泵压厅返回值守大厅。";break;
                case AnnexAction.TakeCalibrationPlate: HasCalibrationPlate=true;message="取得测潮标尺。到东侧测潮大厅安装并读取三个水域刻度。";break;
                case AnnexAction.AlignTideGauge: HasCalibrationPlate=false;GaugeAligned=true;message="测潮架已标定，旁边记录台现在可以读取今日刻度。";break;
                case AnnexAction.ToggleEastLights: EastLightsDim=!EastLightsDim;message=EastLightsDim?"东侧走廊照明已调暗，远处更难被看见。脚步仍会暴露位置。":"东侧走廊照明已恢复。";break;
                case AnnexAction.RestAtBench: message="休息点已记录当前进度。被拦截后从这里重试。";break;
                case AnnexAction.ReadHarborPlan: PlanRead=true;message="总图已记录：先修复船坞，再取熔断器送电；档案钥匙开放东区，最后标定测潮架并泄压撤离。";break;
            }
            return true;
        }
    }
}
