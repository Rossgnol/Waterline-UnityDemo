using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    // Only the optional exploration scene owns this component; older scenes keep their rules.
    public sealed class HarborSupplies : MonoBehaviour
    {
        public AnnexSession annex;
        public TextMesh[] dialLabels;
        public Transform[] dialVisuals;
        public Transform cabinetLid;
        public GameObject cabinetContents;
        public Material decoyMaterial;
        public float delaySeconds=3, ringingSeconds=6, hearingRadius=26;
        public bool Pending { get { return device!=null && delay>0; } }
        public bool Ringing { get { return device!=null && delay<=0; } }
        public bool Busy { get { return device!=null; } }
        public Vector3 DevicePosition { get { return device!=null?device.transform.position:Vector3.zero; } }
        public int NoisePulses { get; private set; }
        private GameObject device;
        private AudioSource source;
        private AudioClip clip;
        private float delay, remaining, pulse;
        private GUIStyle hud;

        private void Start(){clip=DockAudio.Pulse("Clockwork decoy",.4f,880,true);Apply();}
        private void Update(){Apply();Advance(Time.deltaTime);}
        public bool CanDeploy(out string reason)
        {
            reason=annex.State.BlockedReason(AnnexAction.DeployDecoy);
            if(reason.Length>0)return false;
            if(annex.dock.InputBlocked){reason="合上界面后才能放置诱敌器。";return false;}
            if(Busy){reason="上一枚诱敌器仍在运转。";return false;}
            var p=annex.dock.playerTransform.position;
            var room=annex.Harbor.RoomAt(p);
            if(room==null || room.sector==0 || room.safe)
            {reason="到北楼巡查区再放置；安全房与船坞不消耗诱敌器。";return false;}
            if(!Physics.Raycast(p+Vector3.up*.2f,Vector3.down,out var hit,1f) || hit.normal.y<.8f)
            {reason="站稳在平整地面后再放置。";return false;}
            return true;
        }
        public void AfterAction(AnnexAction action)
        {
            if(action==AnnexAction.ReadSupplyManifest)
            {
                annex.NoteOpen=true;annex.NoteTitle="工装间 / 夜班物资领用单";
                annex.NoteBody="机修车间的物资柜已改用各部门的剩余数量校验。\n\n测潮组：原有 4 盒，今晚没有领用。\n机修组：原有 4 盒，领走 1 盒。\n查验组：原有 3 盒，领走 2 盒。\n\n柜面按部门名称排列，每只轮盘在 0–4 之间循环。\n\n柜内保留两枚发条诱敌器：放下后 3 秒发声，持续 6 秒。可趁敌人调查时，从遮挡后绕行。追逐中的人不会因响声立刻放弃你。\n\n这是一条可选物资支线，不影响船坞复航。按 J 随时回看已读记录。";
            }
            if(action==AnnexAction.DeployDecoy)
            {
                device=GameObject.CreatePrimitive(PrimitiveType.Sphere);device.name="Deployed clockwork decoy";
                device.transform.position=annex.dock.playerTransform.position+Vector3.up*.14f;
                device.transform.localScale=new Vector3(.28f,.28f,.28f);
                device.GetComponent<Collider>().enabled=false;
                device.GetComponent<Renderer>().sharedMaterial=decoyMaterial;
                source=device.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=1;source.volume=.65f;source.maxDistance=hearingRadius;
                delay=delaySeconds;remaining=ringingSeconds;pulse=0;NoisePulses=0;
            }
            Apply();
        }
        public void Apply()
        {
            if(annex.State==null)return;
            var s=annex.State;
            int[] values={s.SupplyRepair,s.SupplyInspection,s.SupplyTide};
            for(int i=0;i<dialLabels.Length;i++)
            {
                dialLabels[i].text=new[]{"REPAIR ","INSPECT ","TIDE "}[i]+values[i];
                if(dialVisuals!=null && i<dialVisuals.Length && dialVisuals[i]!=null)dialVisuals[i].localRotation=Quaternion.Euler(0,0,-values[i]*72);
            }
            cabinetLid.localRotation=Quaternion.Euler(s.SupplyCabinetOpen?75:0,0,0);
            cabinetContents.SetActive(!s.SupplyCabinetOpen);
        }
        public void Advance(float dt)
        {
            if(!Busy)return;
            if(annex.dock.InputBlocked){if(source!=null)source.Pause();return;}
            if(source!=null)source.UnPause();
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt))return;
            if(delay>0){delay-=dt;if(delay>0)return;dt=-delay;delay=0;}
            pulse-=dt;
            if(pulse<=0)
            {
                source.Play();NoisePulses++;
                foreach(var guard in annex.Harbor.guards)guard.HearNoise(device.transform.position,hearingRadius);
                pulse=1;
            }
            remaining-=dt;
            if(remaining<=0)ResetTransient();
        }
        public void ResetTransient()
        {if(source!=null)source.Stop();if(device!=null)Destroy(device);device=null;source=null;delay=remaining=pulse=0;Apply();}
        public bool RoomResolved(string id)
        {
            var s=annex.State;
            switch(id)
            {
                case "rigging_store":return annex.Harbor.Planner!=null?s.LowerSuppliesTaken:annex.dock.State.HasPin || annex.dock.State.PinInstalled;
                case "float_pit":return s.FloatPrepared;
                case "drain_service":return annex.Harbor.Planner!=null?s.FloatPrepared:s.FloodPlanRead;
                case "hub":return s.LogRead;
                case "workwear":return s.ManualRead && s.SupplyManifestRead;
                case "spares":return s.HasFuse || s.PowerRestored;
                case "workshop":return s.WorkshopLoopOpen && s.SupplyCabinetOpen;
                case "power":return s.PowerRestored;
                case "archive":return s.HasSignalKey && s.IndexRead;
                case "dispatch":return s.ServiceLoopOpen;
                case "instruments":return s.HasCalibrationPlate || s.GaugeAligned;
                case "tide":return s.GaugeAligned && s.RecordRead;
                case "signal":return s.PressureBalanced && s.ShortcutOpen;
                default:return false;
            }
        }
        public string RoomStatus(string id)
        {
            if(RoomResolved(id))return "本室线索 / 一次性目标已完成。";
            var s=annex.State;
            switch(id)
            {
                case "workwear":return s.SupplyManifestRead?"领用单已抄录；仍有静音手册可读。":"仍有物资领用线索可收集。";
                case "workshop":return s.SupplyCabinetOpen?"物资已领取；维修门栓尚未开启。":"可选物资柜尚未领取。";
                case "archive":return s.HasSignalKey?"钥匙已取；移交清单尚未阅读。":"本室保留信号钥匙与移交线索。";
                case "inspection":return "诱敌铃可重复使用，每次复位 12 秒。";
                case "light_control":return s.EastLightsDim?"东廊已调暗；可随时恢复。":"东廊照明正常；可以调暗。";
                case "rest":return "休息凳可重复设置本次运行的重试位置。";
                default:return "";
            }
        }
        private void OnDestroy(){if(device!=null)Destroy(device);if(clip!=null)Destroy(clip);}
        private void OnGUI()
        {
            if(annex.dock.HideGameplayHud || annex.dock.InputBlocked)return;
            if(hud==null){hud=new GUIStyle(GUI.skin.label){font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},16),fontSize=16,wordWrap=true};hud.normal.textColor=Color.white;}
            var previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale;var rect=new Rect(width-320,140,296,86);var color=GUI.color;GUI.color=new Color(.025f,.055f,.07f,.94f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;
            string inventory=annex.State.SupplyCabinetOpen || annex.State.LowerSuppliesTaken?"Q  放置诱敌器 · 剩余 "+annex.State.DecoyCharges:"可选探索 · 工装间领用单 → 车间物资柜";
            GUI.Label(new Rect(rect.x+14,rect.y+10,268,67),inventory+"\n"+(Pending?"发声倒计时："+Mathf.CeilToInt(delay)+" 秒":Ringing?"诱敌器正在发声 · 利用遮挡绕行":"J 随身记录 · M 路线图"),hud);GUI.matrix=previous;
        }
    }
}
