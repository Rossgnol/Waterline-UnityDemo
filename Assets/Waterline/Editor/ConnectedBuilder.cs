using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using static Waterline.Editor.AnnexBuilder;
using Object=UnityEngine.Object;
namespace Waterline.Editor
{
    public static class ConnectedBuilder
    {
        public const string Path="Assets/Waterline/Scenes/Day06_ConnectedAnnex.unity";
        [MenuItem("Waterline/13 Open connected annex")]
        public static void Open() {EditorSceneManager.OpenScene(Path);}
        public static void Generate()
        {
            BeginExtension();root.name="NORTH ANNEX - Ten rooms with connected service loops";
            // Enlarge the room depth, retaining human-sized furnishings and interaction targets.
            foreach(Transform t in root.Cast<Transform>().ToArray())
            {
                var p=t.position;if(p.z<6)continue;
                p.z=ConnectedLayout.ExpandZ(p.z);t.position=p;
                bool stretch=t.name.StartsWith("Wall ") || t.name.Contains("foundation") || t.name=="Annex ceiling" ||
                    t.name=="Roof stringer" || t.name=="Floor panel seam" || t.name.Contains("electric lock") || t.name.Contains("key lock");
                if(stretch){var s=t.localScale;s.z*=1.6f;t.localScale=s;}
                var guard=t.GetComponent<ThreatEncounter>();if(guard==null)continue;
                foreach(var n in guard.graph.nodes){var np=n.position;np.z=ConnectedLayout.ExpandZ(np.z);n.position=np;}
                guard.areaMax.y=40;guard.areaMin.y=ConnectedLayout.ExpandZ(guard.areaMin.y);
            }
            // Open the old outer walls; each wing connects at both ends.
            foreach(Transform t in root.Cast<Transform>().ToArray())
            {
                var p=t.position;
                if((t.name=="Wall vertical" && Mathf.Abs(p.x+11)<.05f) ||
                   (t.name=="Wall horizontal" && Mathf.Abs(p.z-30)<.05f) ||
                   (t.name=="Wall horizontal" && Mathf.Abs(p.z-25.2f)<.05f && p.x>-5 && p.x<1))Object.DestroyImmediate(t.gameObject);
            }
            WallX(-11,6,30,10,22,27.5f);
            WallZ(25.2f,-5,1,-2);
            WallZ(30,-11,11,-8,-2,3,8);
            WallX(-20,6,30);WallZ(6,-20,-11);WallZ(30,-20,-11);
            WallZ(18,-20,-11,-16);
            WallX(-11,30,40);WallX(1,30,40,35);WallX(5,30,40,35);WallX(11,30,40);WallZ(40,-11,11);
            Cube("West wing foundation",new Vector3(-15.5f,-.2f,18),new Vector3(9,.4f,24),floor);
            Cube("West wing ceiling",new Vector3(-15.5f,3.5f,18),new Vector3(9,.18f,24),dark);
            Cube("North wing foundation",new Vector3(0,-.2f,35),new Vector3(22.2f,.4f,10.2f),floor);
            Cube("North wing ceiling",new Vector3(0,3.5f,35),new Vector3(22.2f,.18f,10.2f),dark);
            var rubber=Mat("Quiet rubber",new Color(.075f,.17f,.15f));
            Cube("West wing quiet rubber floor",new Vector3(-15.5f,.012f,18),new Vector3(8.7f,.02f,23.7f),rubber,false);
            foreach(float z in new[]{9f,15f,21f,27f})Lamp(new Vector3(-16,2.8f,z),true);
            foreach(float x in new[]{-8f,-2f,3f,8f})Lamp(new Vector3(x,2.8f,35),false);
            RoomSign("10  WORKSHOP / QUIET ROUTE",-15.5f,17.75f);
            RoomSign("11  PACKING / BYPASS",-15.5f,29.75f);
            RoomSign("12  DISPATCH / DOOR CONTROL",-5,39.75f);
            RoomSign("13  TIDE GAUGE / DAILY RECORD",8,39.75f);
            Label("< QUIET WING     DISPATCH ^     TIDE GAUGE >",new Vector3(-1,2.5f,29.7f),0,.032f);
            Label("< WORKSHOP / QUIET BYPASS",new Vector3(-10.75f,2.5f,10),90,.028f);
            Desk("Workshop drafting desk",new Vector3(-18.2f,0,12),2.5f);
            Item(AnnexAction.ReadWorkshopManual,"阅读管线维修手册",new Vector3(-18.2f,1.02f,12),paper,"控制台顺序与西翼静音通路说明。",new Vector3(.6f,.035f,.45f));
            foreach(float z in new[]{8f,15f,21f,26f})Shelf(new Vector3(-19.3f,0,z),true);
            foreach(float z in new[]{20.4f,24.5f})Cube("Packing sight-break crate",new Vector3(-14,1,z),new Vector3(1.5f,2,2),blue);
            Cube("Workshop pump assembly",new Vector3(-12.4f,.7f,14.5f),new Vector3(1.1f,1.4f,2),dark);
            Desk("Dispatch chart table",new Vector3(-5,0,37),4);
            Cube("Dispatch sight-break filing wall",new Vector3(-5,1.1f,33),new Vector3(2,2.2f,.6f),blue);
            var layout=root.gameObject.AddComponent<ConnectedLayout>();
            layout.serviceGate=Gate("Archive north dispatch-controlled lock",new Vector3(-2,1.2f,25.2f),false);
            Cube("Dispatch switch stand",new Vector3(-.45f,.5f,32),new Vector3(.6f,1,.6f),dark);
            Item(AnnexAction.OpenServiceLoop,"开启档案室北门",new Vector3(-.45f,1.15f,32),brass,"通电后开启南北贯通捷径，无须完成泄压。",new Vector3(.45f,.3f,.45f));
            Label("ARCHIVE NORTH DOOR",new Vector3(-2,2.2f,25.05f),0,.027f);
            Desk("Tide gauge recording desk",new Vector3(8,0,37),3);
            var record=root.GetComponentsInChildren<AnnexInteractable>().First(i=>i.action==AnnexAction.ReadTideRecord);
            record.transform.position=new Vector3(8,1.015f,37);record.description="查阅今日三个水域的标定刻度，读后记录在 M 地图中。";
            foreach(var sign in root.GetComponentsInChildren<TextMesh>())
            {
                if(sign.text.StartsWith("SEA 6"))sign.transform.position=new Vector3(8,2.15f,38.95f);
                if(sign.text.StartsWith("< SPARES"))Object.DestroyImmediate(sign.gameObject);
            }
            Item(AnnexAction.ReadArchiveIndex,"阅读档案移交清单",new Vector3(-2.65f,1.015f,22.8f),paper,"信号钥匙在本桌；今日记录已移交东北测潮间。",new Vector3(.7f,.035f,.45f));
            for(int i=0;i<3;i++)
            {
                Cube("Tide gauge column",new Vector3(5.5f+i*.8f,1.45f,39.4f),new Vector3(.25f,2.3f,.25f),brass);
                for(int j=0;j<7;j++)Cube("Gauge scale mark",new Vector3(5.5f+i*.8f,.4f+j*.32f,39.22f),new Vector3(.38f,.04f,.04f),paper,false);
            }
            layout.bell=Item(AnnexAction.RingInspectionBell,"启动延时查验铃",new Vector3(9.6f,1.12f,19.6f),red,"3 秒后响铃吸引巡查者，趁机从另一扇门离开；12 秒复位。",new Vector3(.45f,.25f,.45f)).transform;
            Label("DELAY BELL / 3 SEC",new Vector3(9.2f,2,20.5f),0,.027f);
            // Both annex guards can now follow the full loop, including the west bypass and north wing.
            var north=root.GetComponentsInChildren<ThreatEncounter>().First(e=>e.displayName=="北楼巡检员");
            SetGraph(north,new[]{P(-8,20.4f),P(-8,27.5f),P(-16,27.5f),P(-16,22),P(-8,22),P(-8,35),P(-2,35),P(-2,27.5f),P(3,27.5f),P(3,20.4f),P(3,10.8f),P(-2,10.8f),P(-16,15),P(-16,10)},new[]{0,1,2,3,12,13,12,3,4,0,1,5,6,7,8,9,10,11,10,9,8,7,6,5,1});
            north.areaMin=new Vector2(-20,6);north.areaMax=new Vector2(5,40);
            var east=root.GetComponentsInChildren<ThreatEncounter>().First(e=>e.displayName=="东区看守");
            SetGraph(east,new[]{P(8,20.4f),P(8,27.5f),P(8,35),P(3,35),P(3,27.5f),P(3,20.4f)},new[]{0,1,2,3,4,5});
            east.areaMin=new Vector2(1,15.6f);east.areaMax=new Vector2(11,40);
            ConfigureMap(layout);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Path);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true)};AssetDatabase.SaveAssets();ValidateConnected();
            Debug.Log("[Waterline] CONNECTED GENERATED: 10 annex rooms, 964 square metres, west and north loops.");
        }
        private static Vector3 P(float x,float z){return new Vector3(x,.03f,z);}
        private static void SetGraph(ThreatEncounter e,Vector3[] p,int[] route)
        {
            var links=p.Select(_=>new HashSet<int>()).ToArray();
            for(int i=0;i<route.Length;i++){int a=route[i],b=route[(i+1)%route.Length];if(a!=b){links[a].Add(b);links[b].Add(a);}}
            e.graph.nodes=p.Select((v,i)=>new ThreatNode{position=v,level=-1,label=e.displayName+" / "+i,links=links[i].ToArray()}).ToArray();
            e.graph.lowerPatrol=route;e.graph.upperPatrol=route;e.graph.upperArrival=0;e.transform.position=p[0];
        }
        private static void ConfigureMap(ConnectedLayout l)
        {
            var rooms=new List<MapRoom>();
            void R(float a,float b,float s,float n,string label,bool safe=false,GroundKind ground=GroundKind.Concrete)
                {rooms.Add(new MapRoom{bounds=Rect.MinMaxRect(a,s,b,n),label=label,safe=safe,ground=ground});}
            R(-11,-5,-5,5,"水控室\n修泵 / 注水",true);R(5,11,-5,5,"绞盘间\n解锁 / 展桥");R(-5,5,2.5f,4.5f,"检修桥",false,GroundKind.Metal);R(-11,11,-7.5f,-4.5f,"南侧栈道",false,GroundKind.Rubber);
            R(-11,-5,6,14,"值班室\n记录 / 暂避",true);R(-11,-5,14,25.2f,"备件库\n熔断器");
            R(-5,1,6,15.6f,"备用机房\n恢复电力");R(-5,1,15.6f,25.2f,"档案室\n钥匙 / 移交单");R(1,5,6,25.2f,"检\n修\n廊",false,GroundKind.Metal);
            R(5,11,6,15.6f,"信号室\n三阀泄压",true);R(5,11,15.6f,25.2f,"查验间\n延时诱敌铃");R(-11,11,25.2f,30,"北回廊 / 金属",false,GroundKind.Metal);
            R(-20,-11,6,18,"维修间\n管线手册\n橡胶静音路",false,GroundKind.Rubber);R(-20,-11,18,30,"包装库\n避敌 / 西翼回路",false,GroundKind.Rubber);
            R(-11,1,30,40,"调度室\n开启档案北门");R(1,5,30,40,"观\n测\n廊",false,GroundKind.Metal);R(5,11,30,40,"测潮间\n今日阀位记录");l.rooms=rooms.ToArray();
            var doors=new List<MapDoor>();void D(float x,float z,bool vertical=false,GameObject gate=null){doors.Add(new MapDoor{position=new Vector2(x,z),vertical=vertical,blocker=gate});}
            D(-8,5.5f);D(8,5.5f,false,annex.shortcutLock);D(-8,14);D(-5,10.8f,true);D(1,10.8f,true);
            D(-2,15.6f,false,annex.archiveLocks[0]);D(1,20.4f,true,annex.archiveLocks[1]);D(5,10.8f,true,annex.signalLocks[0]);D(8,15.6f,false,annex.signalLocks[1]);D(5,20.4f,true);
            D(-8,25.2f);D(-2,25.2f,false,l.serviceGate);D(8,25.2f);D(-11,10,true);D(-11,22,true);D(-11,27.5f,true);D(-16,18);
            D(-8,30);D(-2,30);D(3,30);D(8,30);D(1,35,true);D(5,35,true);l.doors=doors.ToArray();
        }
        public static void ValidateConnected()
        {
            ThreatBuilder.Validate();var l=Object.FindFirstObjectByType<ConnectedLayout>();
            if(l==null || l.rooms.Length!=17 || l.doors.Length!=23)throw new InvalidOperationException("Connected topology incomplete");
            if(Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).Length!=14)throw new InvalidOperationException("Expected 14 annex interactions");
        }
        [MenuItem("Waterline/14 Build connected demo")]
        public static void BuildWindows()
        {
            Open();ValidateConnected();Directory.CreateDirectory("Builds/Connected");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Path},locationPathName="Builds/Connected/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Connected build failed");
        }
        public static void FinalizeConnected() {Generate();AnnexPreview.CaptureConnected();BuildWindows();}
    }
}


