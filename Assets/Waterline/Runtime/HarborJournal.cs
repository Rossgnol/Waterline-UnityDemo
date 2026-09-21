using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class HarborJournal : MonoBehaviour
    {
        private sealed class Entry {public AnnexAction action;public string title,body;}
        public AnnexSession annex;
        public bool Showing { get; private set; }
        private readonly List<Entry> entries=new List<Entry>();
        private AnnexAction? selected;
        private GUIStyle text,title,button,small;
        public int CollectedCount {get{return entries.Count(e=>Collected(e.action));}}
        public void Record(AnnexAction action,string heading,string body)
        {
            var entry=entries.Find(e=>e.action==action);
            if(entry==null){entry=new Entry{action=action};entries.Add(entry);}
            entry.title=heading;entry.body=body;selected=action;
        }
        private bool Collected(AnnexAction action)
        {
            var s=annex.State;
            switch(action)
            {
                case AnnexAction.ReadFloodPlan:return s.FloodPlanRead;
                case AnnexAction.ReadDutyLog:return s.LogRead;
                case AnnexAction.ReadHarborPlan:return s.PlanRead;
                case AnnexAction.ReadWorkshopManual:return s.ManualRead;
                case AnnexAction.ReadArchiveIndex:return s.IndexRead;
                case AnnexAction.ReadTideRecord:return s.RecordRead;
                case AnnexAction.ReadSupplyManifest:return s.SupplyManifestRead;
                default:return false;
            }
        }
        public void Toggle(){if(annex.dock.Caught || annex.dock.State.Phase==DockPhase.Escaped)return;if(Showing && annex.dock.MapVisible)Close();else{Showing=true;annex.dock.MapVisible=true;}}
        public void Close(){Showing=false;annex.dock.MapVisible=false;}
        public void ShowMap(){Showing=false;annex.dock.MapVisible=true;}
        private void Update(){if(!annex.dock.MapVisible)Showing=false;}
        private void OnGUI()
        {
            if(!Showing || !annex.dock.MapVisible || annex.NoteOpen)return;
            if(text==null)
            {
                var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
                text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};text.normal.textColor=new Color(.9f,.95f,.96f);
                title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=15};
                button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,wordWrap=true};
            }
            var matrix=GUI.matrix;int depth=GUI.depth;GUI.depth=-16;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float x=Screen.width/scale/2-605,y=Screen.height/scale/2-330;
            Fill(new Rect(x,y,1210,660),new Color(.025f,.045f,.055f,.995f));
            GUI.Label(new Rect(x+24,y+20,650,45),"随身记录 / 已收集 "+CollectedCount+" 份",title);
            if(GUI.Button(new Rect(x+875,y+22,150,35),"路线图 / M",button))ShowMap();
            if(GUI.Button(new Rect(x+1040,y+22,145,35),"合上 / J",button))Close();
            var known=entries.Where(e=>Collected(e.action)).ToArray();
            var active=known.FirstOrDefault(e=>e.action==selected)??known.FirstOrDefault();
            for(int i=0;i<known.Length;i++)
            {
                var old=GUI.backgroundColor;GUI.backgroundColor=known[i]==active?new Color(.25f,.7f,.65f):Color.gray;
                if(GUI.Button(new Rect(x+24,y+92+i*76,320,64),known[i].title,button))selected=known[i].action;
                GUI.backgroundColor=old;
            }
            Fill(new Rect(x+365,y+90,820,500),new Color(.07f,.11f,.13f));
            GUI.Label(new Rect(x+390,y+110,770,40),active!=null?active.title:"还没有收集记录",title);
            GUI.Label(new Rect(x+390,y+170,770,390),active!=null?active.body:"在场景中对纸张按 E 阅读，内容会保存在这里。\n\n首次进入北楼时，可先查看中央大厅的复航总图。工装间还留有静音作业手册与夜班领用单。\n\n尚未找到的内容不会提前显示。",text);
            GUI.Label(new Rect(x+24,y+614,1160,32),"查看时暂停敌人与机关。重试保留检查点中的已读线索；退出游戏仍会清除本次进度。",small);
            GUI.matrix=matrix;GUI.depth=depth;
        }
        private static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    }
}
