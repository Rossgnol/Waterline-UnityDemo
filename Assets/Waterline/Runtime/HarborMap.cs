using UnityEngine;
namespace Waterline
{
    public sealed class HarborMap : MonoBehaviour
    {
        public HarborLayout layout;
        private GUIStyle text,title,roomText,small,button;
        private int sector, floor;
        private bool wasOpen;
        private string selected;
        private void Update()
        {
            bool open=layout.dock.MapVisible;
            if(open && !wasOpen){var room=layout.RoomAt(layout.dock.playerTransform.position);sector=room!=null?room.sector:0;floor=layout.dock.playerTransform.position.y<-.6f?-1:0;selected=null;}
            wasOpen=open;
        }
        private void Styles()
        {
            if(text!=null)return;var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};text.normal.textColor=new Color(.91f,.95f,.94f);
            title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=14};
            roomText=new GUIStyle(text){fontSize=15,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=16,wordWrap=true};
        }
        private Rect Bounds {get {return sector==0?(floor<0 && layout.LowerDeck!=null?layout.LowerDeck.LowerMapBounds:Rect.MinMaxRect(-23,-18,23,6)):sector==1?Rect.MinMaxRect(-32,4,32,51):Rect.MinMaxRect(-32,48,32,72);}}
        private Vector2 Project(Rect map,Vector3 p){var b=Bounds;return new Vector2(map.x+(p.x-b.xMin)/b.width*map.width,map.y+(b.yMax-p.z)/b.height*map.height);}
        private void OnGUI()
        {
            if(!layout.dock.MapVisible || layout.annex.NoteOpen || (layout.annex.Journal!=null && layout.annex.Journal.Showing))return;Styles();
            var previous=GUI.matrix;int depth=GUI.depth;GUI.depth=-15;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float ox=Screen.width/scale/2-620,oy=Screen.height/scale/2-345;
            Fill(new Rect(ox,oy,1240,690),new Color(.02f,.04f,.055f,.995f));
            GUI.Label(new Rect(ox+22,oy+16,820,38),"第七码头 / 路线与探索",title);
            if(layout.annex.Journal!=null && GUI.Button(new Rect(ox+934,oy+18,144,32),"随身记录 / J",button))layout.annex.Journal.Toggle();
            if(GUI.Button(new Rect(ox+1095,oy+18,122,32),"返回 / M",button))layout.dock.MapVisible=false;
            var goal=layout.Room(layout.GoalRoom);
            for(int i=0;i<3;i++)
            {
                string label=new[]{"船坞 · 修复","动力区 · 送电","北区 · 测潮"}[i]+(goal!=null && goal.sector==i?"  ◆":"");
                var old=GUI.backgroundColor;GUI.backgroundColor=sector==i?new Color(.25f,.72f,.65f):Color.gray;
                if(GUI.Button(new Rect(ox+22+i*235,oy+65,225,38),label,button)){sector=i;selected=null;}GUI.backgroundColor=old;
            }
            if(sector==0)
            {
                if(GUI.Button(new Rect(ox+746,oy+65,76,38),"上层"+(goal!=null && goal.sector==0 && goal.level==0?" ◆":""),button))floor=0;
                if(GUI.Button(new Rect(ox+832,oy+65,76,38),"下层"+(goal!=null && goal.sector==0 && goal.level<0?" ◆":""),button))floor=-1;
            }
            var map=new Rect(ox+22,oy+120,886,496);Fill(map,new Color(.012f,.025f,.035f));
            foreach(var r in layout.rooms)
            {
                if(r.sector!=sector || (sector==0 && r.level!=floor))continue;
                Vector2 tl=Project(map,new Vector3(r.bounds.xMin,0,r.bounds.yMax)),br=Project(map,new Vector3(r.bounds.xMax,0,r.bounds.yMin));
                var rect=new Rect(tl.x+3,tl.y+3,br.x-tl.x-6,br.y-tl.y-6);
                bool known=layout.Visited.Contains(r.id),isGoal=r.id==layout.GoalRoom,current=r.id==layout.CurrentRoom;
                Color color=known?(r.safe?new Color(.08f,.32f,.29f):new Color(.15f,.23f,.29f)):new Color(.055f,.08f,.095f);
                bool flooded=layout.LowerDeck!=null && r.level<0 && layout.LowerDeck.Closed;
                if(flooded)color=new Color(.06f,.17f,.32f);
                if(r.id=="float_link")color=layout.LowerDeck.Raised?new Color(.09f,.4f,.47f):new Color(.25f,.2f,.1f);
                if(isGoal)Fill(new Rect(rect.x-2,rect.y-2,rect.width+4,rect.height+4),new Color(.88f,.63f,.27f));
                Fill(rect,current?new Color(.17f,.39f,.45f):color);
                bool resolved=known && layout.annex.Supplies!=null && layout.annex.Supplies.RoomResolved(r.id);
                string phase=flooded?"\n注水封闭":r.id=="float_link"?(layout.LowerDeck.Raised?"\n已升起 · 可通行":"\n待注水升起"):"";
                if(layout.Planner!=null && r.id=="float_link")phase=layout.LowerDeck.Raised?"\n已升起 · 可通行":layout.Planner.Ready?"\n已准备 · 等待注水":"\n未试压 · 不会升起";
                if(layout.Planner!=null && r.id=="bridge" && layout.Planner.BridgeClosed){phase="\n已收回 · 船只航道";color=new Color(.25f,.12f,.1f);Fill(rect,color);}
                GUI.Label(rect,(known || isGoal?r.title:"未探索")+(resolved?" ✓":"")+(isGoal?"\n◆ 当前目标":"")+phase,roomText);
                if((known || isGoal) && Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition)){selected=r.id;Event.current.Use();}
            }
            foreach(var door in layout.doors)
            {
                var a=layout.Room(door.from);var b=layout.Room(door.to);
                if(a==null || b==null || (a.sector!=sector && b.sector!=sector))continue;
                if(sector==0 && a.level!=floor && b.level!=floor)continue;
                if(!layout.Visited.Contains(a.id) && !layout.Visited.Contains(b.id))continue;
                if(!Bounds.Contains(new Vector2(door.position.x,door.position.z)))continue;
                Vector2 p=Project(map,door.position);bool open=layout.IsOpen(door.rule);
                Fill(new Rect(p.x-(door.vertical?3:9),p.y-(door.vertical?9:3),door.vertical?6:18,door.vertical?18:6),open?new Color(.4f,.86f,.72f):new Color(.92f,.52f,.22f));
            }
            var playerRoom=layout.RoomAt(layout.dock.playerTransform.position);
            if(playerRoom!=null && playerRoom.sector==sector && (sector!=0 || playerRoom.level==floor))
            {
                Vector2 p=Project(map,layout.dock.playerTransform.position);Fill(new Rect(p.x-5,p.y-5,10,10),Color.white);
                Vector3 f=layout.dock.playerTransform.forward;Vector2 tip=Project(map,layout.dock.playerTransform.position+f*1.1f);Fill(new Rect(tip.x-2,tip.y-2,4,4),Color.white);
            }
            float sx=ox+934;GUI.Label(new Rect(sx,oy+120,280,34),"下一步",title);
            GUI.Label(new Rect(sx,oy+162,280,75),layout.Objective,text);
            GUI.Label(new Rect(sx,oy+244,280,158),layout.RouteHint,text);
            var chosen=layout.Room(selected);
            string progress=chosen!=null && layout.annex.Supplies!=null?layout.annex.Supplies.RoomStatus(chosen.id):"";
            GUI.Label(new Rect(sx,oy+412,280,125),chosen!=null?chosen.title+"\n"+chosen.purpose+(progress.Length>0?"\n"+progress:""):"点击已知房间查看用途。\n灰色空间尚未探索；门只在接近对应区域后显示。",small);
            var s=layout.annex.State;
            GUI.Label(new Rect(sx,oy+546,280,93),s.RecordRead?"已抄录刻度\n外海 6 / 坞池 2 / 内港 4\n控制台：内港 → 坞池 → 外海":"随身记录\n熔断器 "+(s.HasFuse?"携带":"—")+" / 钥匙 "+(s.HasSignalKey?"已取":"—")+"\n标尺 "+(s.GaugeAligned?"已安装":s.HasCalibrationPlate?"携带":"未取得"),small);
            GUI.Label(new Rect(ox+22,oy+638,1190,30),"白点：你  ◆：当前目标  青色房间：可暂避  橙色门：尚未解锁  "+(layout.annex.Supplies!=null?"✓：一次性目标完成  ":"")+"查看地图时暂停",small);
            GUI.matrix=previous;GUI.depth=depth;
        }
        private static void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
