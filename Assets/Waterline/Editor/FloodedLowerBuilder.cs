using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static class FloodedLowerBuilder
    {
        public const string ScenePath = "Assets/Waterline/Scenes/Day10_FloodedLower.unity";
        private static Transform root;
        private static HarborLayout layout;
        private static Material floor, wall, steel, amber, teal;
        private static readonly List<HarborDoor> added = new List<HarborDoor>();
        [MenuItem("Waterline/23 Open flooded lower deck")]
        public static void Open() { EditorSceneManager.OpenScene(ScenePath); }

        public static void Generate()
        {
            CirculationBuilder.Open(); added.Clear();
            layout = Object.FindFirstObjectByType<HarborLayout>();
            root = new GameObject("DAY 10 / tidal maintenance chambers").transform;
            var lower = layout.gameObject.AddComponent<FloodedLowerDeck>(); lower.layout = layout;
            floor = GameObject.Find("hub / floor").GetComponent<Renderer>().sharedMaterial;
            wall = GameObject.Find("Expanded north seawall lower").GetComponent<Renderer>().sharedMaterial;
            steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Deck steel.mat");
            amber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Safety amber.mat");
            teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Signal teal.mat");
            // Existing upper portals and lower enemy patrol edges remain where they were.
            Object.DestroyImmediate(GameObject.Find("Expanded north seawall lower"));
            WallZ("Lower seawall", 5.36f, -20.65f, 20.65f, -10, 10);
            Box("Maintenance floor", new Vector3(0,-3.7f,12.55f),new Vector3(28,.4f,14.7f),floor);
            Box("Maintenance north wall",new Vector3(0,-1.95f,20),new Vector3(28,3.1f,.3f),wall);
            foreach(float x in new[]{-14f,14f})Box("Maintenance outside wall",new Vector3(x,-1.95f,12.8f),new Vector3(.3f,3.1f,14.4f),wall);
            foreach(float x in new[]{-6f,6f})
            {
                Box("Chamber divider south",new Vector3(x,-1.95f,10.55f),new Vector3(.25f,3.1f,9.9f),wall);
                Box("Chamber divider north",new Vector3(x,-1.95f,19.1f),new Vector3(.25f,3.1f,1.8f),wall);
                Box("Chamber door lintel",new Vector3(x,-.7f,17),new Vector3(.25f,.6f,2.8f),amber);
            }
            Object.DestroyImmediate(GameObject.Find("hub / floor"));
            Box("Hall floor west",new Vector3(-9,-.2f,14),new Vector3(10,.4f,16),floor);
            Box("Hall floor east",new Vector3(9,-.2f,14),new Vector3(10,.4f,16),floor);
            Box("Hall floor north",new Vector3(0,-.2f,16.5f),new Vector3(8,.4f,11),floor);
            Box("Hall floor south lip",new Vector3(0,-.2f,6.15f),new Vector3(8,.4f,.3f),floor);
            foreach(float z in new[]{6.3f,11f})
                Rail(Box("Well fixed upper railing",new Vector3(0,.7f,z),new Vector3(8.3f,1.4f,.13f),steel),false);
            foreach(float x in new[]{-4f,4f})
            {
                Rail(Box("Well side railing south",new Vector3(x,.7f,6.85f),new Vector3(.13f,1.4f,1.1f),steel),true);
                Rail(Box("Well side railing north",new Vector3(x,.7f,10.45f),new Vector3(.13f,1.4f,1.1f),steel),true);
            }
            lower.platform = Box("Rising float platform",new Vector3(0,-3.65f,8.65f),new Vector3(8,.3f,4.7f),steel).transform;
            // Deck markings follow the floating slab, without becoming obstacles.
            for(int i=0;i<7;i++)
            {
                var stripe=Box("Float deck stripe",new Vector3(-3+i,-3.492f,8.65f),new Vector3(.035f,.012f,4.2f),amber,false);
                stripe.transform.SetParent(lower.platform,true);
            }
            lower.water = Object.Instantiate(layout.dock.water.gameObject,root).transform;
            lower.water.name="Maintenance water surface";lower.water.position=new Vector3(0,layout.dock.dryWaterHeight,12.5f);
            lower.water.localScale=new Vector3(27.7f,.06f,15f);
            var rooms=layout.rooms.ToList();
            rooms.Add(Room("rigging_store","缆具间","联轴销移交工作台；北侧维护通道通向浮台井。",-14,-6,5.6f,20,-1,0));
            rooms.Add(Room("float_pit","浮台井","干坞时可绕行；注水后平台升到大厅，成为上层通路。",-6,6,5.6f,20,-1,0));
            rooms.Add(Room("drain_service","排水检修间","注水影响记录；南门回坞底东侧，再上东梯。",6,14,5.6f,20,-1,0));
            rooms.Add(Room("float_link","浮台通路","注水到顶后跨过大厅检修井，安全连接大厅东西两侧。",-4,4,6.3f,11,0,1));
            layout.rooms=rooms.ToArray();
            layout.Room("lower_tools").purpose="西梯下层入口；北门去缆具间取销，也可从东门进入坞底。";
            Door("lower_tools","rigging_store",-10,-2.1f,5.36f,false,HarborGateRule.DryDock,"RIGGING / PIN","维护舱：注水前可通行，注水后永久封闭。");
            Door("lower_dock","drain_service",10,-2.1f,5.36f,false,HarborGateRule.DryDock,"DRAIN SERVICE","排水检修间；注水后入口封闭。");
            Door("rigging_store","float_pit",-6,-2.1f,17,true,HarborGateRule.Open,"FLOAT PIT","下层静音绕行，经浮台井前往排水检修间。");
            Door("float_pit","drain_service",6,-2.1f,17,true,HarborGateRule.Open,"DRAIN SERVICE","排水检修间南门回东侧坞底。");
            Door("hub","float_link",-4,.8f,8.65f,true,HarborGateRule.FloatLink,"FLOAT / WAIT","浮台护门：水位到顶后自动开放。另一侧仍是安全大厅。");
            Door("float_link","hub",4,.8f,8.65f,true,HarborGateRule.FloatLink,"FLOAT / WAIT","注水后跨井通路：大厅东侧可经已解锁东门前往登艇台。");
            layout.doors=layout.doors.Concat(added).ToArray();
            lower.statusLabels=added.Where(d=>d.rule==HarborGateRule.FloatLink).Select(d=>d.sign).ToArray();
            var pin=Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==DockAction.TakePin);
            pin.transform.position=new Vector3(-10,-2.38f,16);
            pin.description="联轴销已移交缆具间。取走后可从北侧维护舱绕行至东梯。";
            Box("Rigging workbench",new Vector3(-10,-3.05f,16),new Vector3(2,.8f,1.2f),steel);
            var note=Box("Flood impact record",new Vector3(11.5f,-2.37f,16),new Vector3(.65f,.08f,.55f),amber);
            Box("Drain record desk",new Vector3(11.5f,-3.05f,16),new Vector3(2,.8f,1.2f),steel);
            var interaction=note.AddComponent<AnnexInteractable>();interaction.annex=layout.annex;interaction.action=AnnexAction.ReadFloodPlan;
            interaction.displayName="排水检修记录";interaction.description="查看注水会封闭哪些区域，以及浮台升起后的新通路。";
            foreach(float x in new[]{-12.5f,12.5f})for(int i=0;i<3;i++)
                Box("Chamber wall equipment",new Vector3(x,-2.6f,8+i*2),new Vector3(1,1.6f,1.1f),steel);
            foreach(float x in new[]{-13.8f,13.8f})Box("Flood height warning line",new Vector3(x,-.6f,12.8f),new Vector3(.03f,.07f,13.8f),amber,false);
            foreach(float x in new[]{-10f,0f,10f})
            {
                var go=new GameObject("Maintenance lamp");go.transform.SetParent(root);go.transform.position=new Vector3(x,-.7f,17);
                var lamp=go.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=10;lamp.intensity=3;lamp.color=new Color(.5f,.8f,.9f);
            }
            Label("01 RIGGING / PIN",new Vector3(-10,-1.3f,19.8f),0,.045f);
            Label("FLOAT WELL / RISES WITH WATER",new Vector3(0,-1.25f,19.8f),0,.033f);
            Label("DRAIN SERVICE / FLOOD PLAN",new Vector3(10,-1.3f,19.8f),0,.033f);
            Label("PIN MOVED > NORTH / RIGGING",new Vector3(-11.2f,-1.2f,3.6f),0,.03f);
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(label.text.StartsWith("01 TOOL ROOM")){label.text="TOOLS / NORTH > RIGGING";label.characterSize=.038f;}
            foreach(var item in Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None))
                if(item.action==DockAction.StartFlood)item.description="先带走下层物品。注水淹没维护舱，并把大厅浮台升成上层通路；长按确认。";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] LOWER DECK GENERATED: three lower rooms, flooded loss of access and raised hall link.");
        }
        private static HarborRoom Room(string id,string title,string purpose,float west,float east,float south,float north,int level,int sector)
        {return new HarborRoom{id=id,title=title,purpose=purpose,bounds=Rect.MinMaxRect(west,south,east,north),level=level,sector=sector,safe=true,ground=GroundKind.Rubber};}
        private static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static TextMesh Label(string text,Vector3 p,float yaw,float size)
        {
            var go=new GameObject("Lower sign / "+text);go.transform.SetParent(root);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=72;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var depth=go.AddComponent<WorldLabel>();depth.template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/World text.mat");depth.Refresh();return label;
        }
        private static void Rail(GameObject barrier,bool vertical)
        {
            barrier.GetComponent<Renderer>().enabled=false;
            Vector3 p=barrier.transform.position,s=barrier.transform.lossyScale;
            var bar=Box("Guard handrail",p+Vector3.up*(s.y*.5f-.06f),vertical?new Vector3(.1f,.1f,s.z):new Vector3(s.x,.1f,.1f),amber,false);
            bar.transform.SetParent(barrier.transform,true);
            int posts=Mathf.Max(2,Mathf.CeilToInt((vertical?s.z:s.x)/1.2f)+1);
            for(int i=0;i<posts;i++)
            {
                float offset=Mathf.Lerp(-.48f,.48f,i/(float)(posts-1))*(vertical?s.z:s.x);
                var post=Box("Guard upright",p+(vertical?Vector3.forward:Vector3.right)*offset,new Vector3(.08f,s.y,.08f),steel,false);
                post.transform.SetParent(barrier.transform,true);
            }
        }
        private static void WallZ(string name,float z,float min,float max,params float[] holes)
        {
            float cursor=min;
            foreach(float hole in holes)
            {
                Box(name,new Vector3((cursor+hole-1.4f)/2,-1.88f,z),new Vector3(hole-1.4f-cursor,3.64f,.5f),wall);
                Box("Lower portal lintel",new Vector3(hole,-.6f,z),new Vector3(2.8f,1.08f,.5f),wall);cursor=hole+1.4f;
            }
            Box(name,new Vector3((cursor+max)/2,-1.88f,z),new Vector3(max-cursor,3.64f,.5f),wall);
        }
        private static void Door(string from,string to,float x,float y,float z,bool vertical,HarborGateRule rule,string caption,string hint)
        {
            int index=layout.doors.Length+added.Count;
            var d=new HarborDoor{from=from,to=to,position=new Vector3(x,y,z),vertical=vertical,rule=rule,hint=hint};
            if(rule!=HarborGateRule.Open)
                d.blocker=Box("Tidal gate / "+from+" / "+to,d.position,vertical?new Vector3(.16f,rule==HarborGateRule.FloatLink?1.6f:2.8f,2.6f):new Vector3(2.8f,2.8f,.16f),amber);
            d.sign=Label(caption,d.position+Vector3.up*(rule==HarborGateRule.FloatLink?1.5f:1.1f),vertical?90:0,.025f);
            if(rule==HarborGateRule.FloatLink)
            {
                Rail(d.blocker,true);
                d.sign.text="FLOAT / LOW";d.sign.characterSize=.0118f;
                d.sign.transform.position=new Vector3(x+Mathf.Sign(x)*.2f,1.65f,z+1.75f);
                d.sign.transform.rotation=Quaternion.Euler(0,x<0?90:-90,0);
                Box("Float status backing",new Vector3(x+Mathf.Sign(x)*.16f,1.65f,z+1.75f),new Vector3(.045f,.4f,1.6f),wall,false);
            }
            // Markers are placed beside the portal, never across an open doorway.
            var marker=Box("Portal information",d.position+(vertical?Vector3.forward:Vector3.right)*1.55f,new Vector3(.25f,.45f,.25f),teal);
            var info=marker.AddComponent<HarborDoorMarker>();info.layout=layout;info.index=index;added.Add(d);
            if(d.blocker!=null)d.blocker.SetActive(rule==HarborGateRule.FloatLink);
        }
        public static void Validate()
        {
            PrototypeValidation.Validate();var h=Object.FindFirstObjectByType<HarborLayout>();
            if(h.rooms.Length!=31 || h.doors.Length!=36 || h.LowerDeck==null)throw new Exception("Lower topology mismatch");
            if(Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None).Length!=4)throw new Exception("Guard count changed");
            if(h.LowerDeck.platform==null || h.LowerDeck.water==null)throw new Exception("Flood presentation references missing");
            foreach(var d in h.doors)if(h.Room(d.from)==null || h.Room(d.to)==null)throw new Exception("Unknown room endpoint");
        }
        [MenuItem("Waterline/24 Build flooded lower deck")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/FloodedLower");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/FloodedLower/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Lower deck build failed");
        }
    }
}
