using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Waterline.Editor
{
    public static class ThreatBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day03_Threat.unity";
        private static Transform root;
        [MenuItem("Waterline/08 Open threat level")]
        public static void Open()
        {
            if(!File.Exists(ScenePath))Build();
            else if(Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if(File.Exists(ScenePath)){Open();return;}
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            ExplorationBuilder.Open();
            root=new GameObject("DAY 03 - Sound and pursuit").transform;
            var session=Object.FindFirstObjectByType<DockSession>();var player=Object.FindFirstObjectByType<FirstPersonController>();
            var tuning=ScriptableObject.CreateInstance<ThreatTuning>();AssetDatabase.CreateAsset(tuning,"Assets/Waterline/Settings/ThreatTuning.asset");
            var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Dark steel.mat");
            var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Safety amber.mat");
            var coat=new Material(Shader.Find("Universal Render Pipeline/Lit"));coat.SetColor("_BaseColor",new Color(.43f,.33f,.09f));
            AssetDatabase.CreateAsset(coat,"Assets/Waterline/Generated/Drag worker coat.mat");
            Cube("South west sight blocker",new Vector3(-3,.95f,-5.1f),new Vector3(2.5f,1.9f,1),dark);
            Cube("South east sight blocker",new Vector3(3,.95f,-6.9f),new Vector3(2.5f,1.9f,1),dark);
            Label("QUIET WALK / CTRL",new Vector3(-8,-1.1f,4.79f),0,.045f);
            Label("SAFE WORKSHOP",new Vector3(-5.17f,-1.1f,0),-90,.026f);
            Label("RUBBER / QUIETER",new Vector3(-6.5f,.065f,-6.6f),0,.028f,true);
            Label("METAL / NOISY",new Vector3(0,.065f,3.5f),0,.028f,true);
            var actor=new GameObject("Drag worker");actor.transform.SetParent(root);actor.transform.position=new Vector3(3,-3.47f,3.7f);
            var controller=actor.AddComponent<CharacterController>();controller.height=1.85f;controller.radius=.28f;
            controller.center=new Vector3(0,.925f,0);controller.stepOffset=.3f;controller.skinWidth=.025f;
            var visual=new GameObject("Worker silhouette").transform;visual.SetParent(actor.transform,false);
            Part(visual,"Raincoat",new Vector3(0,1.12f,0),new Vector3(.62f,.92f,.43f),coat);
            Part(visual,"Hood",new Vector3(0,1.7f,0),new Vector3(.44f,.4f,.4f),coat);
            Part(visual,"Dark visor",new Vector3(0,1.72f,.211f),new Vector3(.31f,.13f,.035f),dark);
            Part(visual,"Work lamp",new Vector3(.19f,1.45f,.25f),new Vector3(.09f,.09f,.08f),amber);
            Part(visual,"Backpack",new Vector3(0,1.14f,-.31f),new Vector3(.45f,.57f,.24f),dark);
            var limbs=new Transform[4];
            limbs[0]=Part(visual,"Left leg",new Vector3(-.18f,.34f,0),new Vector3(.19f,.67f,.22f),dark);
            limbs[1]=Part(visual,"Right leg",new Vector3(.18f,.34f,0),new Vector3(.19f,.67f,.22f),dark);
            limbs[2]=Part(visual,"Left arm",new Vector3(-.39f,1.02f,0),new Vector3(.16f,.7f,.18f),coat);
            limbs[3]=Part(visual,"Right arm",new Vector3(.39f,1.02f,0),new Vector3(.16f,.7f,.18f),coat);
            for(int i=0;i<6;i++)Part(visual,"Trailing chain",new Vector3(.43f,.08f,-.25f-i*.16f),new Vector3(.085f,.09f,.17f),dark);
            var graph=root.gameObject.AddComponent<ThreatGraph>();BuildGraph(graph);
            var threat=actor.AddComponent<ThreatEncounter>();threat.session=session;threat.player=player;threat.graph=graph;threat.tuning=tuning;threat.body=visual;threat.limbs=limbs;
            session.threat=threat;root.gameObject.AddComponent<ThreatHud>().threat=threat;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] Threat scene generated.");
        }

        private static void BuildGraph(ThreatGraph graph)
        {
            var nodes=new List<ThreatNode>();
            Add(nodes,"Lower SW",-3,-3.47f,-3.6f,-1); Add(nodes,"Lower NW",-3,-3.47f,3.7f,-1);
            Add(nodes,"Lower NE",3,-3.47f,3.7f,-1); Add(nodes,"Lower SE",3,-3.47f,-3.6f,-1);
            Add(nodes,"East lower north",10,-3.47f,3.7f,-1);Add(nodes,"East lower south",10,-3.47f,-3.6f,-1);
            Add(nodes,"Stair approach",10,-3.47f,4.8f,-1);Add(nodes,"Stair foot",12.65f,-3.47f,4.8f,0);
            Add(nodes,"Stair 18",12.65f,-3.3f,3.6f,0);Add(nodes,"Stair 14",12.65f,-2.6f,2,0);
            Add(nodes,"Stair 8",12.65f,-1.6f,0,0);Add(nodes,"Stair 3",12.65f,-.5f,-2,0);
            Add(nodes,"Stair top",12.65f,.03f,-4,0);Add(nodes,"East doorway",10,.03f,-4,1);
            Add(nodes,"East side",10,.03f,-2.5f,1);Add(nodes,"East overlook",10,.03f,3.5f,1);
            Add(nodes,"Bridge entry",6.4f,.03f,3.5f,1);Add(nodes,"Bridge center",0,.03f,3.5f,1);Add(nodes,"Bridge west",-4.2f,.03f,3.5f,1);
            Add(nodes,"South east",8,.03f,-6,1);Add(nodes,"South cover east",5,.03f,-5.6f,1);Add(nodes,"South center",0,.03f,-5.6f,1);
            Add(nodes,"South bend",-.8f,.03f,-6.6f,1);Add(nodes,"South cover west",-5.5f,.03f,-6.6f,1);Add(nodes,"South west",-8,.03f,-6,1);
            graph.nodes=nodes.ToArray();
            var links=new List<int>[nodes.Count];for(int i=0;i<links.Length;i++)links[i]=new List<int>();
            Connect(links,0,1);Connect(links,1,2);Connect(links,2,3);Connect(links,3,0);
            Connect(links,2,4);Connect(links,3,5);Connect(links,4,5);Connect(links,4,6);
            for(int i=6;i<18;i++)Connect(links,i,i+1);
            Connect(links,14,19);for(int i=19;i<24;i++)Connect(links,i,i+1);
            for(int i=0;i<links.Length;i++)graph.nodes[i].links=links[i].ToArray();
            graph.lowerPatrol=new[]{2,1,2,4,3,0,3};graph.upperPatrol=new[]{15,17,15,19,21,19};graph.upperArrival=15;
        }
        private static void Add(List<ThreatNode> nodes,string label,float x,float y,float z,int level)
        {
            var p=new Vector3(x,y,z);
            if(Physics.Raycast(p+Vector3.up*.7f,Vector3.down,out var hit,1.5f,~0,QueryTriggerInteraction.Ignore))p.y=hit.point.y+.03f;
            nodes.Add(new ThreatNode{label=label,position=p,level=level});
        }
        private static void Connect(List<int>[] links,int a,int b){links[a].Add(b);links[b].Add(a);}
        private static GameObject Cube(string name,Vector3 position,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
        private static Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;
        }
        private static void Label(string value,Vector3 position,float yaw,float size,bool floor=false)
        {
            var go=new GameObject("Threat sign "+value);go.transform.SetParent(root);go.transform.position=position;go.transform.rotation=Quaternion.Euler(floor?90:0,yaw,0);
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=72;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var depth=go.AddComponent<WorldLabel>();depth.template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/World text.mat");depth.Refresh();
        }
        public static void Validate()
        {
            ExplorationBuilder.Validate();var threat=Object.FindFirstObjectByType<DockSession>().threat;
            if(threat==null || threat.tuning==null || threat.graph==null || threat.session.threat!=threat)throw new System.InvalidOperationException("Threat references missing.");
            if(threat.graph.Path(2,threat.graph.upperArrival,2).Count<5)throw new System.InvalidOperationException("Evacuation path missing.");
            foreach(var node in threat.graph.nodes) if(ThreatEncounter.IsSafe(node.position))throw new System.InvalidOperationException("Enemy route enters a safe room: "+node.label);
        }
        [MenuItem("Waterline/10 Select threat settings")]
        public static void SelectSettings()
        {Selection.activeObject=AssetDatabase.LoadAssetAtPath<ThreatTuning>("Assets/Waterline/Settings/ThreatTuning.asset");}
        [MenuItem("Waterline/09 Build threat demo")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/Threat");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Threat/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new System.InvalidOperationException("Threat build failed.");
            Debug.Log("[Waterline] Threat Windows build ready.");
        }
    }
}
