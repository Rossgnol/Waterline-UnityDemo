using System;
using Waterline.Core;
namespace Waterline.Checks
{
    public static class AnnexStateChecks
    {
        public static int RunAll()
        {
            int count=0;string message;var s=new AnnexState();
            Check(!s.TryApply(AnnexAction.RestorePower,out message),"Missing fuse accepted");count++;
            Check(!s.TryApply(AnnexAction.DialHarbor,out message),"Unpowered controls active");count++;
            s.TryApply(AnnexAction.TakeFuse,out message);Check(s.HasFuse && !s.TryApply(AnnexAction.TakeFuse,out message),"Fuse duplicated");count++;
            s.TryApply(AnnexAction.RestorePower,out message);Check(s.PowerRestored && !s.HasFuse,"Power did not consume fuse");count++;
            Check(!s.TryApply(AnnexAction.ConfirmPressure,out message),"Keyless signal confirmation allowed");count++;
            s.TryApply(AnnexAction.TakeSignalKey,out message);
            for(int a=0;a<7;a++)for(int b=0;b<7;b++)for(int c=0;c<7;c++)
            {
                var trial=s.Copy();trial.Harbor=a;trial.Dock=b;trial.Sea=c;
                bool pass=trial.TryApply(AnnexAction.ConfirmPressure,out message);
                Check(pass==(a==4&&b==2&&c==6),"Incorrect valve solution accepted");count++;
                Check(!s.PressureBalanced && s.Harbor==0 && s.Dock==0 && s.Sea==0,"Snapshot was mutated");count++;
            }
            for(int i=0;i<7;i++)s.TryApply(AnnexAction.DialHarbor,out message);
            Check(s.Harbor==0,"Dial does not wrap");count++;
            var checkpoint=s.Copy();s.TryApply(AnnexAction.ReadTideRecord,out message);s.TryApply(AnnexAction.OpenShortcut,out message);
            Check(!checkpoint.RecordRead && !checkpoint.ShortcutOpen,"Checkpoint shares world progress");count++;
            s.Harbor=4;s.Dock=2;s.Sea=6;s.TryApply(AnnexAction.ConfirmPressure,out message);
            Check(!s.TryApply(AnnexAction.DialDock,out message) && s.Dock==2,"Solved puzzle can be corrupted");count++;
            var extra=new AnnexState();
            Check(!extra.TryApply(AnnexAction.OpenServiceLoop,out message),"Unpowered service loop opened");count++;
            extra.TryApply(AnnexAction.TakeFuse,out message);extra.TryApply(AnnexAction.RestorePower,out message);
            Check(extra.TryApply(AnnexAction.OpenServiceLoop,out message) && extra.ServiceLoopOpen,"Powered service loop failed");count++;
            Check(!extra.TryApply(AnnexAction.OpenServiceLoop,out message),"Service unlock repeated");count++;
            var copy=extra.Copy();extra.TryApply(AnnexAction.ReadWorkshopManual,out message);extra.TryApply(AnnexAction.ReadArchiveIndex,out message);
            Check(!copy.ManualRead && !copy.IndexRead && copy.ServiceLoopOpen,"Optional progress corrupts snapshot");count++;
            var harbor=new AnnexState{CalibrationRequired=true};
            Check(!harbor.TryApply(AnnexAction.ReadTideRecord,out message),"Uncalibrated record readable");count++;
            Check(!harbor.TryApply(AnnexAction.AlignTideGauge,out message),"Unpowered gauge aligned");count++;
            Check(!harbor.TryApply(AnnexAction.ToggleEastLights,out message),"Unpowered lighting toggled");count++;
            harbor.TryApply(AnnexAction.TakeFuse,out message);harbor.TryApply(AnnexAction.RestorePower,out message);
            Check(!harbor.TryApply(AnnexAction.AlignTideGauge,out message),"Missing calibration plate accepted");count++;
            harbor.TryApply(AnnexAction.TakeCalibrationPlate,out message);
            Check(!harbor.TryApply(AnnexAction.TakeCalibrationPlate,out message),"Calibration plate duplicated");count++;
            Check(!harbor.TryApply(AnnexAction.ReadTideRecord,out message),"Carried plate bypassed installation");count++;
            Check(harbor.TryApply(AnnexAction.AlignTideGauge,out message) && harbor.GaugeAligned && !harbor.HasCalibrationPlate,"Gauge does not consume plate");count++;
            Check(harbor.TryApply(AnnexAction.ReadTideRecord,out message) && harbor.RecordRead,"Calibrated record unavailable");count++;
            Check(!harbor.TryApply(AnnexAction.TakeCalibrationPlate,out message),"Installed plate respawned");count++;
            Check(!harbor.TryApply(AnnexAction.AlignTideGauge,out message),"Gauge installation repeated");count++;
            var harborSave=harbor.Copy();harbor.TryApply(AnnexAction.OpenWorkshopLoop,out message);harbor.TryApply(AnnexAction.OpenObservationLoop,out message);harbor.TryApply(AnnexAction.ToggleEastLights,out message);
            Check(!harborSave.WorkshopLoopOpen && !harborSave.ObservationLoopOpen && !harborSave.EastLightsDim && harborSave.GaugeAligned,"Harbor checkpoint aliased");count++;
            Check(harbor.EastLightsDim && harbor.TryApply(AnnexAction.ToggleEastLights,out message) && !harbor.EastLightsDim,"Lighting is not reversible");count++;
            for(int a=0;a<5;a++)for(int b=0;b<5;b++)for(int c=0;c<5;c++)
            {
                var cabinet=new AnnexState{SupplyRepair=a,SupplyInspection=b,SupplyTide=c};
                bool solved=cabinet.TryApply(AnnexAction.OpenSupplyCabinet,out message);
                Check(solved==(a==3 && b==1 && c==4),"Supply cabinet accepted wrong deduction");count++;
                Check(cabinet.DecoyCharges==(solved?2:0),"Supply reward mismatch");count++;
            }
            var supplies=new AnnexState();
            Check(!supplies.TryApply(AnnexAction.DeployDecoy,out message) && supplies.DecoyCharges==0,"Empty inventory underflow");count++;
            for(int i=0;i<5;i++)supplies.TryApply(AnnexAction.DialSupplyRepair,out message);
            Check(supplies.SupplyRepair==0,"Supply dial does not wrap");count++;
            supplies.SupplyRepair=3;supplies.SupplyInspection=1;supplies.SupplyTide=4;
            supplies.TryApply(AnnexAction.ReadSupplyManifest,out message);supplies.TryApply(AnnexAction.OpenSupplyCabinet,out message);
            var supplySave=supplies.Copy();
            Check(supplies.TryApply(AnnexAction.DeployDecoy,out message) && supplies.DecoyCharges==1,"First decoy not consumed");count++;
            Check(supplySave.DecoyCharges==2 && supplySave.SupplyManifestRead,"Decoy inventory aliases checkpoint");count++;
            Check(!supplies.TryApply(AnnexAction.OpenSupplyCabinet,out message) && supplies.DecoyCharges==1,"Opened supply cabinet refilled");count++;
            Check(!supplies.TryApply(AnnexAction.DialSupplyRepair,out message),"Opened cabinet remains editable");count++;
            Check(supplies.TryApply(AnnexAction.DeployDecoy,out message) && supplies.DecoyCharges==0,"Second decoy not consumed");count++;
            Check(!supplies.TryApply(AnnexAction.DeployDecoy,out message) && supplies.DecoyCharges==0,"Third decoy created");count++;
            for(int inlet=0;inlet<2;inlet++)for(int drain=0;drain<2;drain++)
            {
                var circuit=new AnnexState{FloatIntakeOpen=inlet==1,FloatDrainClosed=drain==1};
                var before=circuit.Copy();bool ready=circuit.TryApply(AnnexAction.TestFloatCircuit,out message);
                Check(ready==(inlet==1 && drain==1) && circuit.FloatPrepared==ready,"Float accepted leaking or unfed circuit");count++;
                Check(!before.FloatPrepared,"Float snapshot mutated");count++;
                if(ready){Check(!circuit.TryApply(AnnexAction.ToggleFloatDrain,out message) && !circuit.TryApply(AnnexAction.ToggleFloatIntake,out message),"Ready circuit can be corrupted");count++;}
            }
            var cache=new AnnexState();cache.TryApply(AnnexAction.TakeLowerSupplies,out message);
            Check(cache.LowerSuppliesTaken && cache.DecoyCharges==1 && !cache.TryApply(AnnexAction.TakeLowerSupplies,out message),"Lower supply duplicated");count++;
            cache.SupplyRepair=3;cache.SupplyInspection=1;cache.SupplyTide=4;cache.TryApply(AnnexAction.OpenSupplyCabinet,out message);
            Check(cache.DecoyCharges==3,"Cabinet overwrote lower reward");count++;
            var cacheSave=cache.Copy();cache.TryApply(AnnexAction.DeployDecoy,out message);
            Check(cacheSave.DecoyCharges==3 && cache.DecoyCharges==2 && cacheSave.LowerSuppliesTaken,"Reward snapshot aliased");count++;
            return count;
        }
        private static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException("Annex rule: "+message);}
    }
}
