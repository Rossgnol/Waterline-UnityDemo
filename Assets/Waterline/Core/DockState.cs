using System;
using System.Collections.Generic;

namespace Waterline.Core
{
    public enum DockPhase { Dry, Filling, Flooded, Escaped }
    public enum DockAction { TakePin, InstallPin, UnlockGate, DeployBridge, StartFlood, BoardBoat }

    // Pure gameplay rules: no scene objects, animation or engine dependency.
    // The same rule result drives the interaction prompt, HUD and console log.
    public sealed class DockState
    {
        public DockPhase Phase { get; private set; }
        public bool HasPin { get; private set; }
        public bool PinInstalled { get; private set; }
        public bool GateUnlocked { get; private set; }
        public bool BridgeDeployed { get; private set; }
        public float WaterProgress { get; private set; }
        public event Action Changed;

        public DockState() { Phase = DockPhase.Dry; }

        public DockState Copy()
        {
            return new DockState { Phase = Phase, HasPin = HasPin, PinInstalled = PinInstalled,
                GateUnlocked = GateUnlocked, BridgeDeployed = BridgeDeployed, WaterProgress = WaterProgress };
        }

        public string BlockedReason(DockAction action)
        {
            if (Phase == DockPhase.Escaped) return "本次撤离已经完成。";
            switch (action)
            {
                case DockAction.TakePin:
                    return HasPin || PinInstalled ? "联轴销已经取走。" : "";
                case DockAction.InstallPin:
                    if (PinInstalled) return "联轴销已经安装。";
                    return HasPin ? "" : "先从值班室工作台取回联轴销。";
                case DockAction.UnlockGate:
                    return GateUnlocked ? "坞门机械锁已经解除。" : "";
                case DockAction.DeployBridge:
                    return BridgeDeployed ? "检修桥已经展开。" : "";
                case DockAction.StartFlood:
                    if (Phase != DockPhase.Dry) return "注水已经启动。";
                    var missing = MissingFloodConditions();
                    return missing.Length == 0 ? "" : "无法注水：" + string.Join("、", missing) + "。";
                case DockAction.BoardBoat:
                    return Phase == DockPhase.Flooded ? "" : "检修艇尚未浮起，请先完成注水。";
                default: return "未知操作。";
            }
        }

        public string[] MissingFloodConditions()
        {
            var missing = new List<string>();
            if (!PinInstalled) missing.Add("联轴销未安装");
            if (!GateUnlocked) missing.Add("坞门未解锁");
            if (!BridgeDeployed) missing.Add("检修桥未展开");
            return missing.ToArray();
        }

        public bool TryApply(DockAction action, out string message)
        {
            message = BlockedReason(action);
            if (message.Length != 0) return false;
            switch (action)
            {
                case DockAction.TakePin: HasPin = true; message = "取得联轴销。返回控制台安装。"; break;
                case DockAction.InstallPin: HasPin = false; PinInstalled = true; message = "泵已修复。前往东侧解除坞门锁、展开检修桥。"; break;
                case DockAction.UnlockGate: GateUnlocked = true; message = "坞门机械锁已解除。"; break;
                case DockAction.DeployBridge: BridgeDeployed = true; message = "检修桥已展开。可以从桥上返回水控室。"; break;
                case DockAction.StartFlood: Phase = DockPhase.Filling; message = "注水开始。请在上层等待检修艇浮起。"; break;
                case DockAction.BoardBoat: Phase = DockPhase.Escaped; message = "撤离成功。你完成了第一个机关回环。"; break;
                default: return false;
            }
            if (Changed != null) Changed();
            return true;
        }

        public void Tick(float seconds, float fillingDuration)
        {
            if (Phase != DockPhase.Filling || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            if (float.IsNaN(fillingDuration) || float.IsInfinity(fillingDuration) || fillingDuration <= 0) fillingDuration = 1;
            WaterProgress = Math.Min(1f, WaterProgress + seconds / fillingDuration);
            if (WaterProgress >= 1f) Phase = DockPhase.Flooded;
            if (Changed != null) Changed();
        }
    }
}
