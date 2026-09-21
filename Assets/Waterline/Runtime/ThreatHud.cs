using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class ThreatHud : MonoBehaviour
    {
        public ThreatEncounter threat;
        private ThreatEncounter primary;
        private ThreatEncounter[] encounters;
        private void Start() {primary=threat;encounters=FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None);}
        private GUIStyle text, title, button;
        private void OnGUI()
        {
            if(threat.session.State==null || threat.session.HideGameplayHud)return;
            if(encounters!=null && encounters.Length>1)
            {
                float best=float.NegativeInfinity;
                foreach(var e in encounters)
                {
                    if(!e.Available)continue;
                    float score=(e.CaptureProgress>0?10000:e.Awareness>0?5000:!e.InSafeArea?1000:0)-Vector3.Distance(e.transform.position,e.player.transform.position);
                    if(score>best){best=score;threat=e;}
                }
            }
            if(text==null)
            {
                var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
                text=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true};text.normal.textColor=Color.white;
                title=new GUIStyle(text){fontSize=28};button=new GUIStyle(GUI.skin.button){font=font,fontSize=18};
            }
            var previous=GUI.matrix;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f),width=Screen.width/scale,height=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-3;
            var session=threat.session;
            Fill(new Rect(24,238,350,142),new Color(.035f,.075f,.09f,.95f));
            string mode=threat.Mode==ThreatMode.Patrol?"巡查":threat.Mode==ThreatMode.Investigate?"调查声音":
                threat.Mode==ThreatMode.Chase?"正在追踪你":threat.Mode==ThreatMode.Search?"搜索最后位置":"沿东梯上楼";
            GUI.Label(new Rect(40,249,315,26),threat.InSafeArea?"安全区 · 可暂避巡查":threat.displayName+" · "+mode,text);
            Vector3 toEnemy=threat.transform.position-threat.player.transform.position;
            float angle=Vector3.SignedAngle(threat.player.transform.forward,new Vector3(toEnemy.x,0,toEnemy.z),Vector3.up);
            string direction=Mathf.Abs(angle)>135?"后方":Mathf.Abs(angle)<45?"前方":angle>0?"右侧":"左侧";
            GUI.Label(new Rect(40,280,315,26),toEnemy.magnitude<18?"拖链声来自"+direction:"远处暂时没有拖链声",text);
            var groundKind=ThreatEncounter.GroundAt(threat.player.transform.position);
            string ground=groundKind==GroundKind.Metal?"金属":groundKind==GroundKind.Rubber?"橡胶":"普通地面";
            GUI.Label(new Rect(40,311,315,26),ground+" · Ctrl 轻步 / Shift 快走",text);
            Fill(new Rect(40,350,315,5),new Color(.2f,.25f,.28f));
            Fill(new Rect(40,350,315*Mathf.Clamp01((primary==null?threat:primary).NoiseRadius/16),5),new Color(.95f,.63f,.25f));
            if(threat.Awareness>0 || threat.CaptureProgress>0)
            {
                Fill(new Rect(width/2-135,108,270,60),new Color(.3f,.045f,.035f,.9f));
                GUI.Label(new Rect(width/2-120,116,240,26),threat.CaptureProgress>0?"立刻离开！即将被拦截":"正在被注意 · 遮挡视线",text);
                Fill(new Rect(width/2-120,151,240*Mathf.Max(threat.Awareness,threat.CaptureProgress),5),Color.red);
            }
            if(session.Caught)
            {
                GUI.depth=-20;
                float x=width/2-300,y=height/2-155;
                Fill(new Rect(x,y,600,310),new Color(.12f,.035f,.03f,.99f));
                GUI.Label(new Rect(x+28,y+25,545,45),"被巡查者拦截",title);
                GUI.Label(new Rect(x+28,y+85,545,90),"金属上快走会传播更远的声音。试试 Ctrl 轻步，利用船体或设备挡住视线。\n"+(session.annex!=null && session.annex.Harbor!=null?"中央大厅、维修休息室与信号室可以暂避。":"船坞西侧、北楼值班室与信号室可以暂避。"),text);
                if(GUI.Button(new Rect(x+28,y+222,545,54),session.HasCheckpoint?"从检查点重试 / R":"从起点重试 / R",button))session.RetryCheckpoint();
            }
            GUI.matrix=previous;GUI.depth=depth;
        }
        private static void Fill(Rect r,Color c){var previous=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=previous;}
    }
}
