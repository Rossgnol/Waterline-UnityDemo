using System;
using Waterline.Core;

namespace Waterline.Checks
{
    // Executed both by the standalone C# check and the Unity editor validation menu.
    public static class DockStateChecks
    {
        public static int RunAll()
        {
            int checks = 0;
            var state = new DockState();
            string message;
            Require(!state.TryApply(DockAction.InstallPin, out message), "Cannot install missing item"); checks++;
            Require(!state.TryApply(DockAction.BoardBoat, out message), "Cannot board a grounded boat"); checks++;
            Require(!state.TryApply(DockAction.StartFlood, out message), "Cannot flood with unsatisfied conditions"); checks++;
            Require(state.Phase == DockPhase.Dry && state.WaterProgress == 0, "Failed action leaves world unchanged"); checks++;

            // Every incomplete combination must reject flooding; all three conditions are necessary.
            for (int mask = 0; mask < 8; mask++)
            {
                state = new DockState();
                if ((mask & 1) != 0) { Apply(state, DockAction.TakePin); Apply(state, DockAction.InstallPin); }
                if ((mask & 2) != 0) Apply(state, DockAction.UnlockGate);
                if ((mask & 4) != 0) Apply(state, DockAction.DeployBridge);
                bool started = state.TryApply(DockAction.StartFlood, out message);
                Require(started == (mask == 7), "Flood prerequisites mask " + mask); checks++;
            }

            // Prerequisites can be completed in different orders without corrupting progress.
            int[][] orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            foreach (var order in orders)
            {
                state = new DockState();
                foreach (int step in order)
                {
                    if (step == 0) { Apply(state, DockAction.TakePin); Apply(state, DockAction.InstallPin); }
                    else Apply(state, step == 1 ? DockAction.UnlockGate : DockAction.DeployBridge);
                }
                Apply(state, DockAction.StartFlood);
                Require(!state.TryApply(DockAction.StartFlood, out message), "Repeated start rejected"); checks++;
                state.Tick(6, 12);
                Require(state.Phase == DockPhase.Filling && Math.Abs(state.WaterProgress - 0.5f) < 0.0001f, "Water halfway"); checks++;
                Require(!state.TryApply(DockAction.BoardBoat, out message), "Cannot board mid-fill"); checks++;
                state.Tick(100, 12);
                Require(state.Phase == DockPhase.Flooded && state.WaterProgress == 1, "Water clamps and completes"); checks++;
                Apply(state, DockAction.BoardBoat);
                Require(!state.TryApply(DockAction.BoardBoat, out message), "Cannot complete twice"); checks++;
                Require(!state.HasPin && state.PinInstalled, "Installed item cannot be duplicated"); checks++;
            }

            state = new DockState();
            int eventCount = 0; state.Changed += () => eventCount++;
            state.TryApply(DockAction.StartFlood, out message);
            Require(eventCount == 0, "Failed action does not fire Changed"); checks++;
            Apply(state, DockAction.TakePin);
            Require(eventCount == 1, "Successful action fires Changed once"); checks++;
            Require(!state.TryApply(DockAction.TakePin, out message), "Cannot take pin twice"); checks++;
            Apply(state, DockAction.InstallPin); Apply(state, DockAction.UnlockGate); Apply(state, DockAction.DeployBridge);
            Apply(state, DockAction.StartFlood);
            state.Tick(float.NaN, 12); state.Tick(float.PositiveInfinity, 12); state.Tick(-1, 12);
            Require(state.WaterProgress == 0, "Invalid time steps ignored"); checks++;
            state.Tick(1, 0);
            Require(state.Phase == DockPhase.Flooded, "Invalid duration does not divide by zero"); checks++;
            state = new DockState(); Apply(state, DockAction.TakePin);
            int sourceChanges = 0; state.Changed += () => sourceChanges++;
            var snapshot = state.Copy(); Apply(snapshot, DockAction.InstallPin);
            Require(state.HasPin && !state.PinInstalled && snapshot.PinInstalled && !snapshot.HasPin, "Checkpoint has independent inventory"); checks++;
            Require(sourceChanges == 0, "Checkpoint does not copy event subscribers"); checks++;
            Apply(snapshot, DockAction.UnlockGate); Apply(snapshot, DockAction.DeployBridge); Apply(snapshot, DockAction.StartFlood); snapshot.Tick(3, 12);
            var flooding = snapshot.Copy(); snapshot.Tick(100, 12);
            Require(flooding.Phase == DockPhase.Filling && Math.Abs(flooding.WaterProgress - .25f) < .0001f, "Snapshot retains independent world progress"); checks++;
            foreach (GroundKind ground in Enum.GetValues(typeof(GroundKind)))
            {
                Require(NoiseRules.Radius(ground, MotionKind.Still) == 0, "Stationary player is silent"); checks++;
                Require(NoiseRules.Radius(ground, MotionKind.Quiet) < NoiseRules.Radius(ground, MotionKind.Walk) &&
                    NoiseRules.Radius(ground, MotionKind.Walk) < NoiseRules.Radius(ground, MotionKind.Run), "Movement has an audible cost"); checks++;
            }
            Require(NoiseRules.Radius(GroundKind.Rubber, MotionKind.Run) < NoiseRules.Radius(GroundKind.Metal, MotionKind.Walk), "Route surface changes noise risk"); checks++;
            return checks;
        }

        private static void Apply(DockState state, DockAction action)
        {
            string message;
            Require(state.TryApply(action, out message), "Expected success: " + action + " | " + message);
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Rule check failed: " + message);
        }
    }
}
