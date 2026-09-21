using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    // Day11 only: optional preparation changes which route survives the flood.
    public sealed class TidalRoutePlanner : MonoBehaviour
    {
        public HarborLayout layout;
        public GameObject supplyPickup;
        public GameObject boardingGangway;
        public GameObject[] bridgeBarriers;
        public GameObject[] dryBridgeVisuals;
        public TextMesh intakeLabel, drainLabel, testLabel;
        public TextMesh testBackLabel;
        public Transform intakeHandle, drainHandle;
        public Renderer pressureLamp;
        public Material readyMaterial, idleMaterial;
        public bool BridgeClosed { get { return layout.dock.State.Phase!=DockPhase.Dry; } }
        public bool Ready { get { return layout.annex.State.FloatPrepared; } }
        public string FloodRoute { get { return Ready?"浮台已准备：经大厅西侧等待护门开放，跨到东侧回坞登艇。":"浮台未准备：沿水控室向南走南栈道，绕到东侧登艇。"; } }
        public string FloatStatus { get { return layout.LowerDeck.Raised?"FLOAT / OPEN":Ready?(BridgeClosed?"FLOAT / RISING":"FLOAT / READY"):"FLOAT / TEST REQUIRED"; } }
        public bool SafeToStart(Vector3 p)
        { return p.y>=-.25f && p.x < -7.5f && p.x > -15 && p.z>-10.8f && p.z<4.5f; }
        private void LateUpdate(){Apply();}
        public void Apply()
        {
            if(layout.annex.State==null || layout.dock.State==null)return;
            var s=layout.annex.State;
            supplyPickup.SetActive(!s.LowerSuppliesTaken);
            boardingGangway.SetActive(layout.dock.State.Phase==DockPhase.Flooded || layout.dock.State.Phase==DockPhase.Escaped);
            foreach(var barrier in bridgeBarriers)barrier.SetActive(BridgeClosed);
            if(dryBridgeVisuals!=null)foreach(var visual in dryBridgeVisuals)if(visual!=null)visual.SetActive(!BridgeClosed);
            intakeLabel.text="INLET / "+(s.FloatIntakeOpen?"OPEN":"CLOSED");
            drainLabel.text="DRAIN / "+(s.FloatDrainClosed?"CLOSED":"OPEN");
            testLabel.text=s.FloatPrepared?"PRESSURE / READY":!s.FloatIntakeOpen?"PRESSURE / NO FEED":!s.FloatDrainClosed?"PRESSURE / LEAK":"PRESSURE / TEST";
            testBackLabel.text=testLabel.text;
            intakeHandle.localRotation=Quaternion.Euler(0,0,s.FloatIntakeOpen?90:0);
            drainHandle.localRotation=Quaternion.Euler(0,0,s.FloatDrainClosed?90:0);
            pressureLamp.sharedMaterial=s.FloatPrepared?readyMaterial:idleMaterial;
        }
        public string BlockedReason(AnnexAction action)
        {
            if(action<AnnexAction.TakeLowerSupplies || action>AnnexAction.TestFloatCircuit)return "";
            return BridgeClosed?"注水后下层已封闭，不能再调整。" : "";
        }
        public void ReadPlan()
        {
            var a=layout.annex;a.NoteOpen=true;a.NoteTitle="浮台试压 / 提前准备撤离近路";
            a.NoteBody="检修桥横跨船只出坞航道，注水时自动收回。南侧栈道始终可绕行，但仍有巡查。\n\n大厅泵组隔开东西两侧。若想在注水后从大厅直接跨到东侧，先在干坞时准备井内浮台。\n\n沿蓝色进水管打开进水阀；沿橙色旁通管关闭排水阀，让水留在浮箱。最后到浮台井试压，绿灯亮起即准备完成。\n\n这是可选探索，不影响船只注水。缆具间还留有一枚应急诱敌器，可带往北楼使用。所有下层操作必须在注水前完成。";
        }
    }
}
