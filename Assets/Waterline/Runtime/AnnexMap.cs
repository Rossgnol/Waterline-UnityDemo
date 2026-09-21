using UnityEngine;
namespace Waterline
{
    public sealed class AnnexMap : MonoBehaviour
    {
        public AnnexSession annex;
        private GUIStyle label, title, text;
        private void OnGUI()
        {
            if(annex.Harbor!=null && !annex.NoteOpen)return;
            if(!annex.dock.MapVisible && !annex.NoteOpen)return;
            if(label==null)
            {
                var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
                label=new GUIStyle(GUI.skin.label){font=font,fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true};label.normal.textColor=Color.white;
                text=new GUIStyle(label){alignment=TextAnchor.UpperLeft};title=new GUIStyle(text){fontSize=26};
            }
            var previous=GUI.matrix;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-12;
            float x=Screen.width/scale/2-605,y=Screen.height/scale/2-330;
            if(annex.NoteOpen)
            {
                Fill(new Rect(x+250,y+55,710,550),new Color(.045f,.075f,.085f,.99f));
                GUI.Label(new Rect(x+285,y+85,640,45),annex.NoteTitle,title);
                GUI.Label(new Rect(x+285,y+155,640,380),annex.NoteBody,text);
                if(GUI.Button(new Rect(x+285,y+545,640,40),"E / Esc  合上记录",label))annex.NoteOpen=false;
                GUI.matrix=previous;GUI.depth=depth;return;
            }
            Fill(new Rect(x,y,1210,660),new Color(.025f,.055f,.07f,.99f));
            GUI.Label(new Rect(x+25,y+18,1150,40),"船坞与北楼 / M 或 Esc 返回",title);
            GUI.Label(new Rect(x+25,y+65,1150,35),"目标："+annex.dock.Objective,text);
            Rect map=new Rect(x+25,y+120,690,490);
            Fill(map,new Color(.015f,.03f,.04f));
            var layout=annex.Layout;
            if(layout!=null)
            {
                foreach(var room in layout.rooms)
                    Room(map,room.bounds.xMin,room.bounds.xMax,room.bounds.yMin,room.bounds.yMax,room.label,room.safe);
                foreach(var door in layout.doors)
                    Door(map,door.position.x,door.position.y,door.vertical,door.blocker!=null && door.blocker.activeSelf);
            }
            else
            {
            Room(map,-11,-5,-5,5,"水控室\n修泵 / 注水",true);
            Room(map,5,11,-5,5,"绞盘间\n解锁 / 展桥",false);
            Room(map,-5,5,2.5f,4.5f,annex.dock.State.BridgeDeployed?"检修桥":"桥未展开",false);
            Room(map,-11,11,-7.5f,-4.5f,"南侧栈道",false);
            Room(map,-11,-5,6,11,"值班室\n记录 / 暂避",true);
            Room(map,-11,-5,11,18,"备件库\n熔断器",false);
            Room(map,-5,1,6,12,"备用机房\n恢复电力",false);
            Room(map,-5,1,12,18,"潮汐档案室\n钥匙 / 线索",false);
            Room(map,1,5,6,18,"检\n修\n廊",false);
            Room(map,5,11,6,12,"信号室\n三阀泄压",true);
            Room(map,5,11,12,18,"查验间\n绕行 / 巡查",false);
            Room(map,-11,11,18,21,"北侧回廊 / 金属地面",false);
            Door(map,-5,9,true,false);Door(map,1,9,true,false);Door(map,1,15,true,!annex.State.PowerRestored);
            Door(map,5,9,true,!annex.State.HasSignalKey);Door(map,5,15,true,false);
            Door(map,-8,11,false,false);Door(map,-2,12,false,!annex.State.PowerRestored);
            Door(map,8,12,false,!annex.State.HasSignalKey);Door(map,-8,18,false,false);Door(map,8,18,false,false);
            Door(map,-8,5.5f,false,false);Door(map,8,5.5f,false,!annex.State.ShortcutOpen);
            }
            var player=annex.dock.playerTransform.position;
            if(player.y>-.5f){Vector2 p=Project(map,player.x,player.z);Fill(new Rect(p.x-5,p.y-5,10,10),Color.white);}
            var s=annex.State;
            GUI.Label(new Rect(x+750,y+122,425,40),"下层路线",title);
            var lower=new Rect(x+750,y+175,425,82);
            Fill(lower,annex.dock.State.Phase==Core.DockPhase.Dry?new Color(.13f,.23f,.26f):new Color(.33f,.13f,.10f));
            Fill(new Rect(lower.center.x-23,lower.y+13,46,55),new Color(.02f,.04f,.05f));
            GUI.Label(new Rect(lower.x+30,lower.y+15,95,52),"工具间\n西梯",label);
            GUI.Label(new Rect(lower.x+290,lower.y+15,100,52),"东梯\n上绞盘间",label);
            if(player.y<-.5f)
            {
                float px=lower.x+(player.x+15)/30*lower.width,pz=lower.y+(5.5f-player.z)/11*lower.height;
                Fill(new Rect(px-4,pz-4,8,8),Color.white);
            }
            GUI.Label(new Rect(x+750,y+270,425,135),"北楼进度\n备用电源："+(s.PowerRestored?"已恢复":"未恢复")+"\n信号室钥匙："+(s.HasSignalKey?"已取得":"未取得")+"\n泄压程序："+(s.PressureBalanced?"已通过":"未通过")+"\n回程门："+(s.ShortcutOpen?"已打开":"从信号室内打开"),text);
            GUI.Label(new Rect(x+750,y+422,425,120),s.RecordRead?"已读潮位记录\n外海 6 档 · 坞池 2 档 · 内港 4 档\n控制台顺序：内港 → 坞池 → 外海":(layout!=null?"阀位线索\n尚未查阅潮位记录。今日记录位于东北测潮间。":"档案线索\n送电后进入档案室，阅读桌面潮位记录。"),text);
            if(layout!=null)GUI.Label(new Rect(x+750,y+548,425,65),"档案北门："+(s.ServiceLoopOpen?"已开放":"调度室送电后开启")+"\n西翼橡胶地面：脚步较轻 / 查验铃延时 3 秒",text);
            GUI.Label(new Rect(x+25,y+620,1150,30),"白点：你    青色房间：可暂避    红色门：尚未解锁    北楼两名巡查者    查看地图时暂停",text);
            GUI.matrix=previous;GUI.depth=depth;
        }
        private Vector2 Project(Rect r,float x,float z){return annex.Layout!=null?new Vector2(r.x+(x+21)/33*r.width,r.y+(41-z)/50*r.height):new Vector2(r.x+(x+13)/26*r.width,r.y+(22-z)/31*r.height);}
        private void Room(Rect r,float a,float b,float south,float north,string name,bool safe)
        {
            Vector2 tl=Project(r,a,north),br=Project(r,b,south);
            var rect=new Rect(tl.x+2,tl.y+2,br.x-tl.x-4,br.y-tl.y-4);
            Fill(rect,safe?new Color(.09f,.34f,.32f):new Color(.15f,.21f,.25f));GUI.Label(rect,name,label);
        }
        private void Door(Rect map,float x,float z,bool vertical,bool locked)
        {
            var p=Project(map,x,z);Fill(new Rect(p.x-(vertical?3:19),p.y-(vertical?15:3),vertical?6:38,vertical?30:6),locked?new Color(.85f,.33f,.2f):new Color(.35f,.8f,.67f));
        }
        private static void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
