using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class AnnexBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day05_NorthAnnex.unity";
        private const string Folder="Assets/Waterline/Generated/Annex";
        internal static Transform root;
        internal static Material wall, floor, dark, brass, red, blue, paper, glow;
        internal static AnnexSession annex;
        [MenuItem("Waterline/11 Open expanded north annex")]
        public static void Open()
        {
            if(!File.Exists(ScenePath))Generate();
            else if(Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Generate()
        {
            ThreatBuilder.Open();Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            root=new GameObject("NORTH ANNEX - Six rooms and two patrols").transform;
            wall=Mat("Painted plaster",new Color(.28f,.34f,.35f));floor=Mat("Worn tiles",new Color(.17f,.22f,.23f));
            dark=Mat("Machine steel",new Color(.055f,.085f,.095f),.5f);brass=Mat("Warning brass",new Color(.7f,.44f,.13f),.45f);
            red=Mat("Oxide panels",new Color(.4f,.105f,.06f),.35f);blue=Mat("Cabinet blue",new Color(.07f,.25f,.29f),.35f);
            paper=Mat("Old records",new Color(.73f,.68f,.48f));glow=Mat("Worklight",new Color(.9f,.72f,.4f));
            glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.66f,.25f)*2);
            var dock=Object.FindFirstObjectByType<DockSession>();annex=root.gameObject.AddComponent<AnnexSession>();annex.dock=dock;dock.annex=annex;
            root.gameObject.AddComponent<AnnexMap>().annex=annex;Object.FindFirstObjectByType<DockMap>().enabled=false;
            OpenDockPortals();
            Cube("Annex continuous foundation",new Vector3(0,-.2f,13.5f),new Vector3(22.4f,.4f,15.8f),floor);
            WallX(-11,6,21);WallX(11,6,21);WallZ(21,-11,11);
            WallZ(6,-11,11,-8,8);
            WallX(-5,6,18,9);WallX(1,6,18,9,15);WallX(5,6,18,9,15);
            WallZ(11,-11,-5,-8);WallZ(12,-5,1,-2);WallZ(12,5,11,8);
            WallZ(18,-11,-5,-8);WallZ(18,-5,1);WallZ(18,5,11,8);
            // Low trim and ceiling beams make the annex read as connected industrial rooms.
            foreach(float z in new[]{6.15f,11.9f,18.1f,20.85f})Cube("Ceiling cross beam",new Vector3(0,3.15f,z),new Vector3(22,.18f,.22f),dark,false);
            foreach(float x in new[]{-10.85f,-5.1f,1.1f,5.1f,10.85f})Cube("Roof stringer",new Vector3(x,3.2f,13.5f),new Vector3(.16f,.2f,15),dark,false);
            Cube("Annex ceiling",new Vector3(0,3.5f,13.5f),new Vector3(22.4f,.18f,15.8f),dark);
            foreach(float z in new[]{7f,8f,9f,10f,11f,12f,13f,14f,15f,16f,17f,18f,19f,20f})
                Cube("Floor expansion joint",new Vector3(0,.007f,z),new Vector3(22,.006f,.018f),dark,false);
            foreach(float x in new[]{-10f,-8f,-6f,-4f,-2f,0f,2f,4f,6f,8f,10f})
                Cube("Floor panel seam",new Vector3(x,.007f,13.5f),new Vector3(.018f,.006f,15),dark,false);
            Label("NORTH ANNEX / 07",new Vector3(-8,2.7f,4.76f),0,.045f);
            RoomSign("04  DUTY / SAFE",-8,10.8f);RoomSign("05  SPARES",-8,17.8f);
            RoomSign("06  AUX POWER",-2,11.8f);RoomSign("07  TIDE ARCHIVE",-2,17.8f);
            RoomSign("08  SIGNAL / SAFE",8,11.8f);RoomSign("09  INSPECTION",8,17.8f);
            Label("< SPARES    NORTH GALLERY    INSPECTION >",new Vector3(0,2.3f,20.8f),0,.032f);
            Lamp(new Vector3(-8,2.8f,8),true);Lamp(new Vector3(-8,2.8f,15),false);Lamp(new Vector3(-2,2.8f,9),true);
            Lamp(new Vector3(-2,2.8f,15),false);Lamp(new Vector3(8,2.8f,9),true);Lamp(new Vector3(8,2.8f,15),false);
            Lamp(new Vector3(3,2.8f,12),false);Lamp(new Vector3(0,2.8f,19.5f),false);

            Desk("Duty desk",new Vector3(-9.6f,0,7.5f),2.1f);
            Item(AnnexAction.ReadDutyLog,"阅读北楼值班记录",new Vector3(-9.5f,1.02f,7.5f),paper,"了解备用电源与泄压程序。",new Vector3(.45f,.035f,.33f));
            Shelf(new Vector3(-10.4f,0,14),true);Shelf(new Vector3(-5.6f,0,16.3f),true);
            Desk("Spare fuse bench",new Vector3(-9.65f,0,16.7f),1.8f);
            annex.fuse=Item(AnnexAction.TakeFuse,"取走备用熔断器",new Vector3(-9.6f,1.15f,16.7f),brass,"备用机房的配电柜缺少此件。",new Vector3(.18f,.32f,.18f));
            Label("SPARE FUSE",new Vector3(-9.6f,1.65f,17.15f),0,.025f);
            // Generator and switchboard are beside the travel corridor, not in its collision path.
            Cube("Generator skid",new Vector3(-3.5f,.4f,7.1f),new Vector3(2.3f,.8f,1.1f),dark);
            Cylinder("Generator housing",new Vector3(-3.5f,1.05f,7.1f),.45f,1.9f,blue,Quaternion.Euler(0,0,90));
            for(int i=0;i<7;i++)Cube("Generator cooling rib",new Vector3(-4.25f+i*.25f,1.05f,7.1f),new Vector3(.035f,.83f,1),dark,false);
            Cube("Distribution board",new Vector3(-.45f,1.15f,6.4f),new Vector3(1,2.3f,.55f),dark);
            Item(AnnexAction.RestorePower,"安装熔断器并送电",new Vector3(-.45f,1.3f,6.75f),brass,"恢复档案室电锁及信号台供电。",new Vector3(.6f,.5f,.12f));
            var lamp=new GameObject("Power status light");lamp.transform.SetParent(root);lamp.transform.position=new Vector3(-.45f,2.5f,6.7f);
            var powerLight=lamp.AddComponent<Light>();powerLight.type=LightType.Point;powerLight.range=4;powerLight.intensity=1.5f;annex.powerLights=new[]{powerLight};
            Shelf(new Vector3(-4.45f,0,15),false);Shelf(new Vector3(.45f,0,16.5f),false);
            Desk("Archive reading table",new Vector3(-2.2f,0,16.5f),2.2f);
            Item(AnnexAction.ReadTideRecord,"查阅潮位抄录",new Vector3(-2.65f,1.015f,16.5f),paper,"抄录顺序与信号台不同，请辨认三个地点。",new Vector3(.7f,.035f,.45f));
            annex.signalKey=Item(AnnexAction.TakeSignalKey,"取走信号室钥匙",new Vector3(-1.5f,1.08f,16.5f),brass,"解除东南信号室门锁。",new Vector3(.3f,.08f,.18f));
            Label("SEA 6 / DOCK 2 / INNER 4",new Vector3(-2.4f,1.7f,17.7f),0,.03f);
            Desk("Signal console",new Vector3(8,0,10.4f),3.3f);
            annex.dialLabels=new TextMesh[3];
            for(int i=0;i<3;i++)
            {
                float x=6.85f+i*1.05f;
                Item((AnnexAction)((int)AnnexAction.DialHarbor+i),new[]{"调节内港阀（0–6）","调节坞池阀（0–6）","调节外海阀（0–6）"}[i],new Vector3(x,1.16f,10.4f),blue,"每按一次 E 增加一档，6 后回到 0。",new Vector3(.65f,.3f,.65f));
                Cylinder("Hand valve wheel",new Vector3(x,1.36f,10.4f),.27f,.06f,brass,Quaternion.identity);
                Cube("Valve spoke",new Vector3(x,1.41f,10.4f),new Vector3(.5f,.03f,.07f),dark,false);
                annex.dialLabels[i]=Label(new[]{"INNER 0","DOCK 0","SEA 0"}[i],new Vector3(x,1.8f,10.8f),0,.022f);
            }
            Item(AnnexAction.ConfirmPressure,"确认泄压程序",new Vector3(9.95f,1.1f,9.5f),red,"输入顺序：内港 → 坞池 → 外海。",new Vector3(.42f,.25f,.42f));
            Cube("Confirm pedestal",new Vector3(9.95f,.48f,9.5f),new Vector3(.45f,.96f,.45f),dark);
            Label("INNER > DOCK > SEA",new Vector3(8,2.18f,11.75f),0,.027f);
            Desk("Inspection bench",new Vector3(9.6f,0,14.5f),1.9f);
            Cube("Inspection visual cover",new Vector3(6.1f,1,16.25f),new Vector3(1.3f,2,1.7f),blue);
            Shelf(new Vector3(10.45f,0,16.3f),true);
            annex.archiveLocks=new[]{Gate("Archive south electric lock",new Vector3(-2,1.2f,12),false),Gate("Archive east electric lock",new Vector3(1,1.2f,15),true)};
            annex.signalLocks=new[]{Gate("Signal west key lock",new Vector3(5,1.2f,9),true),Gate("Signal north key lock",new Vector3(8,1.2f,12),false)};
            annex.shortcutLock=Gate("Signal to dock shortcut",new Vector3(8,1.2f,5.6f),false);
            Item(AnnexAction.OpenShortcut,"拉开回程门内侧门栓",new Vector3(9.15f,1.15f,6.45f),brass,"从信号室打开，直接返回东侧绞盘间。",new Vector3(.25f,.5f,.3f));
            Label("POWER REQUIRED",new Vector3(-2,1.8f,11.85f),0,.026f);
            Label("SIGNAL KEY",new Vector3(4.82f,1.8f,9),90,.026f);
            var original=dock.threat;original.areaLimited=true;original.areaMin=new Vector2(-16,-8);original.areaMax=new Vector2(16,5.5f);
            AddGuard(original,"北楼巡检员",new[]{V(-8,15),V(-8,19.5f),V(-2,19.5f),V(3,19.5f),V(3,15),V(3,9),V(-2,9),V(-8,12.6f)},
                new[]{0,1,2,3,4,5,6,5,4,3,2,1,0,7},new Vector2(-11,6),new Vector2(5,21),red);
            AddGuard(original,"东区看守",new[]{V(8,15),V(8,19.5f),V(3,19.5f),V(3,15)},new[]{0,1,2,3},new Vector2(1,12),new Vector2(11,21),blue);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] ANNEX GENERATED: six rooms, pressure puzzle, three enemies.");
        }
        internal static void BeginExtension()
        {
            Open();annex=Object.FindFirstObjectByType<AnnexSession>();root=annex.transform;
            wall=Mat("Painted plaster",new Color(.28f,.34f,.35f));floor=Mat("Worn tiles",new Color(.17f,.22f,.23f));
            dark=Mat("Machine steel",Color.gray);brass=Mat("Warning brass",Color.yellow);red=Mat("Oxide panels",Color.red);
            blue=Mat("Cabinet blue",Color.blue);paper=Mat("Old records",Color.white);glow=Mat("Worklight",Color.white);
        }
        internal static Vector3 V(float x,float z){return new Vector3(x,.03f,z);}
        internal static void OpenDockPortals()
        {
            Object.DestroyImmediate(GameObject.Find("WEST / CONTROL north wall"));Object.DestroyImmediate(GameObject.Find("EAST / WINCH north wall"));
            WallZ(5,-11,-5,-8);WallZ(5,5,11,8);
            Object.DestroyImmediate(GameObject.Find("North seawall"));
            Cube("North seawall lower retained",new Vector3(0,-1.88f,5.6f),new Vector3(29.5f,3.64f,.5f),dark);
            WallZ(5.6f,-14.75f,14.75f,-8,8);
            Cube("West portal threshold",new Vector3(-8,-.15f,5.5f),new Vector3(2.2f,.3f,1.2f),floor);
            Cube("East portal threshold",new Vector3(8,-.15f,5.5f),new Vector3(2.2f,.3f,1.2f),floor);
        }
        internal static void AddGuard(ThreatEncounter original,string name,Vector3[] points,int[] route,Vector2 min,Vector2 max,Material coat)
        {
            var go=Object.Instantiate(original.gameObject,root);go.name=name;
            var enemy=go.GetComponent<ThreatEncounter>();enemy.displayName=name;enemy.managesDockFlood=false;enemy.areaLimited=true;enemy.areaMin=min;enemy.areaMax=max;
            var tuning=AssetDatabase.LoadAssetAtPath<ThreatTuning>(Folder+"/"+name+".asset");
            if(tuning==null){tuning=Object.Instantiate(original.tuning);tuning.patrolSpeed=1.05f;tuning.recognitionSeconds=1.2f;tuning.chaseSpeed=3.5f;AssetDatabase.CreateAsset(tuning,Folder+"/"+name+".asset");}enemy.tuning=tuning;
            var graph=go.AddComponent<ThreatGraph>();graph.nodes=new ThreatNode[points.Length];var links=new HashSet<int>[points.Length];
            for(int i=0;i<points.Length;i++)links[i]=new HashSet<int>();
            for(int i=0;i<route.Length;i++){int a=route[i],b=route[(i+1)%route.Length];if(a!=b){links[a].Add(b);links[b].Add(a);}}
            for(int i=0;i<points.Length;i++)graph.nodes[i]=new ThreatNode{label=name+" / "+i,position=points[i],level=-1,links=links[i].ToArray()};
            graph.lowerPatrol=route;graph.upperPatrol=route;graph.upperArrival=0;enemy.graph=graph;go.transform.position=points[0];
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())if(renderer.name.Contains("Raincoat") || renderer.name.Contains("Hood"))renderer.sharedMaterial=coat;
        }
        internal static void WallZ(float z,float left,float right,params float[] doors)
        {
            float start=left;
            foreach(float door in doors.OrderBy(v=>v))
            {if(door-1>start)Cube("Wall horizontal",new Vector3((start+door-1)/2,1.6f,z),new Vector3(door-1-start,3.2f,.2f),wall);
                Cube("Door lintel",new Vector3(door,2.85f,z),new Vector3(2,.7f,.22f),dark);start=door+1;}
            if(start<right)Cube("Wall horizontal",new Vector3((start+right)/2,1.6f,z),new Vector3(right-start,3.2f,.2f),wall);
        }
        internal static void WallX(float x,float south,float north,params float[] doors)
        {
            float start=south;
            foreach(float door in doors.OrderBy(v=>v))
            {if(door-1>start)Cube("Wall vertical",new Vector3(x,1.6f,(start+door-1)/2),new Vector3(.2f,3.2f,door-1-start),wall);
                Cube("Door lintel",new Vector3(x,2.85f,door),new Vector3(.22f,.7f,2),dark);start=door+1;}
            if(start<north)Cube("Wall vertical",new Vector3(x,1.6f,(start+north)/2),new Vector3(.2f,3.2f,north-start),wall);
        }
        internal static GameObject Gate(string name,Vector3 p,bool vertical){return Cube(name,p,vertical?new Vector3(.18f,2.4f,1.92f):new Vector3(1.92f,2.4f,.18f),red);}
        internal static void Desk(string name,Vector3 p,float width)
        {
            Cube(name,p+Vector3.up*.94f,new Vector3(width,.12f,.9f),dark);
            foreach(float offset in new[]{-width*.42f,width*.42f})Cube("Desk leg",p+new Vector3(offset,.45f,0),new Vector3(.1f,.9f,.7f),dark);
        }
        internal static void Shelf(Vector3 p,bool spares)
        {
            Cube("Shelf back",p+Vector3.up*1.1f,new Vector3(.65f,2.2f,2.1f),dark);
            for(int i=0;i<4;i++)
            {
                Cube("Shelf ledge",p+new Vector3(0,.25f+i*.5f,0),new Vector3(.85f,.07f,2.2f),brass,false);
                for(int j=0;j<4;j++)Cube(spares?"Spare parts bin":"Archive binder",p+new Vector3(.04f,.44f+i*.5f,-.8f+j*.5f),new Vector3(.65f,.27f,.35f),spares?blue:paper,false);
            }
        }
        internal static void Lamp(Vector3 p,bool warm)
        {
            Cube("Pendant casing",p+Vector3.up*.12f,new Vector3(1,.16f,.32f),dark,false);Cube("Light diffuser",p,new Vector3(.85f,.04f,.25f),glow,false);
            var go=new GameObject("Annex work light");go.transform.SetParent(root);go.transform.position=p-Vector3.up*.2f;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=3.3f;light.color=warm?new Color(1,.75f,.46f):new Color(.55f,.78f,.9f);
        }
        internal static GameObject Item(AnnexAction action,string name,Vector3 p,Material mat,string description,Vector3 size)
        {
            var go=Cube(name,p,size,mat);var item=go.AddComponent<AnnexInteractable>();item.annex=annex;item.action=action;item.displayName=name;item.description=description;return go;
        }
        internal static Material Mat(string name,Color color,float metallic=0)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(m,path);return m;
        }
        internal static GameObject Cube(string name,Vector3 p,Vector3 size,Material mat,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        internal static void Cylinder(string name,Vector3 p,float radius,float length,Material mat,Quaternion rotation)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.rotation=rotation;
            go.transform.localScale=new Vector3(radius*2,length/2,radius*2);go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        internal static void RoomSign(string value,float x,float z){Label(value,new Vector3(x,2.78f,z),0,.030f);}
        internal static TextMesh Label(string value,Vector3 p,float yaw,float size)
        {
            var go=new GameObject("Annex sign "+value);go.transform.SetParent(root);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=72;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label=go.AddComponent<WorldLabel>();label.template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/World text.mat");label.Refresh();return text;
        }
        public static void Validate()
        {
            ThreatBuilder.Validate();var level=Object.FindFirstObjectByType<AnnexSession>();
            if(level==null || level.dock.annex!=level || level.archiveLocks.Length!=2 || level.signalLocks.Length!=2)throw new InvalidOperationException("Annex references missing.");
            if(Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None).Length!=3)throw new InvalidOperationException("Expected three patrols.");
            if(Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).Length!=10)throw new InvalidOperationException("Expected ten annex interactions.");
        }
        [MenuItem("Waterline/12 Build expanded demo")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/Expanded");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Expanded/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Expanded build failed.");Debug.Log("[Waterline] Expanded demo build ready.");
        }
    }
}
