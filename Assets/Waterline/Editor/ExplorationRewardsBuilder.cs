using System;
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
    public static class ExplorationRewardsBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day08_ExplorationRewards.unity";
        [MenuItem("Waterline/17 Open exploration rewards")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        public static void Generate()
        {
            HarborBuilder.Open();annex=Object.FindFirstObjectByType<AnnexSession>();
            var harbor=annex.Harbor;
            root=new GameObject("Optional workshop supplies and journal").transform;
            var supplies=annex.gameObject.AddComponent<HarborSupplies>();supplies.annex=annex;
            annex.gameObject.AddComponent<HarborJournal>().annex=annex;
            dark=Mat("Supply cabinet steel",new Color(.1f,.19f,.21f),.6f);
            brass=Mat("Supply cabinet amber",new Color(.85f,.48f,.12f),.3f);
            paper=Mat("Supply manifest paper",new Color(.86f,.82f,.66f));
            supplies.decoyMaterial=brass;
            Item(AnnexAction.ReadSupplyManifest,"阅读夜班物资领用单",new Vector3(-26.15f,1.04f,10),paper,"按各部门剩余数量解开机修车间物资柜。读过后可按 J 回看。",new Vector3(.55f,.05f,.45f));
            Label("OPTIONAL / SUPPLY MANIFEST",new Vector3(-26.5f,1.9f,10.65f),0,.022f);
            Cube("Workshop supply cabinet",new Vector3(-24,.85f,47),new Vector3(4,1.7f,1),dark);
            foreach(float x in new[]{-25.92f,-22.08f})Cube("Cabinet edge trim",new Vector3(x,.87f,46.47f),new Vector3(.06f,1.6f,.06f),brass,false);
            Cube("Cabinet lower trim",new Vector3(-24,.08f,46.47f),new Vector3(3.9f,.06f,.06f),brass,false);
            supplies.cabinetContents=Cube("Two clockwork decoy cases",new Vector3(-24,1.5f,47),new Vector3(1.2f,.22f,.55f),brass,false);
            var hinge=new GameObject("Supply lid hinge").transform;hinge.SetParent(root);hinge.position=new Vector3(-24,1.75f,47.45f);
            var lid=Cube("Supply cabinet lid",new Vector3(-24,1.75f,47),new Vector3(4,.12f,1),dark,false);
            lid.transform.SetParent(hinge,true);supplies.cabinetLid=hinge;
            supplies.dialLabels=new TextMesh[3];
            supplies.dialVisuals=new Transform[3];
            for(int i=0;i<3;i++)
            {
                float x=-25+i;
                var dial=Item((AnnexAction)((int)AnnexAction.DialSupplyRepair+i),new[]{"调节机修余量","调节查验余量","调节测潮余量"}[i],new Vector3(x,1.22f,46.4f),brass,"按 E 增加一格，4 后回到 0。需要对应部门的剩余数量。",new Vector3(.55f,.42f,.24f));
                dial.GetComponent<Renderer>().enabled=false;
                var visual=new GameObject("Rotary dial visual").transform;visual.SetParent(root);visual.position=new Vector3(x,1.22f,46.29f);supplies.dialVisuals[i]=visual;
                var previousRoot=root;root=visual;
                Cylinder("Supply rotary wheel",new Vector3(x,1.22f,46.29f),.23f,.12f,brass,Quaternion.Euler(90,0,0));
                Cube("Supply wheel pointer",new Vector3(x,1.31f,46.22f),new Vector3(.045f,.17f,.03f),paper,false);
                root=previousRoot;
                supplies.dialLabels[i]=Label(new[]{"REPAIR 0","INSPECT 0","TIDE 0"}[i],new Vector3(x,2.1f,47.1f),0,.024f);
            }
            Item(AnnexAction.OpenSupplyCabinet,"校验并领取诱敌器",new Vector3(-21.7f,1.15f,46.4f),brass,"物资柜从左到右：机修、查验、测潮。线索在工装间。",new Vector3(.4f,.6f,.3f));
            Cube("Supply release pedestal",new Vector3(-21.7f,.4f,46.4f),new Vector3(.35f,.8f,.35f),dark);
            Label("OPTIONAL / CLOCKWORK SUPPLIES",new Vector3(-24,2.8f,47.5f),0,.032f);
            harbor.Room("workwear").purpose="静音作业手册与物资领用单；线索可解开机修车间物资柜。";
            harbor.Room("workshop").purpose="设备遮挡、维修捷径与可选物资柜；解开后获得两枚延时诱敌器。";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Validate();
            Debug.Log("[Waterline] REWARDS GENERATED: optional supply deduction, two decoys and collected journal.");
        }
        public static void Validate()
        {
            var supplies=Object.FindFirstObjectByType<HarborSupplies>();
            if(supplies==null || supplies.annex.Journal==null || supplies.dialLabels.Length!=3 || supplies.cabinetLid==null || supplies.cabinetContents==null || supplies.decoyMaterial==null)throw new InvalidOperationException("Supply references incomplete");
            var items=Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None);
            if(items.Length!=25 || items.Select(i=>i.action).Distinct().Count()!=25)throw new InvalidOperationException("Expected 25 distinct interactions");
        }
        [MenuItem("Waterline/18 Build exploration rewards")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/Rewards");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Rewards/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Rewards build failed");
        }
        public static void Capture()
        {
            Open();var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            Directory.CreateDirectory("TestResults/Rewards");
            Shot(camera,"workshop",new Vector3(-24,1.7f,42),new Vector3(-24,1.6f,47));
            Shot(camera,"manifest",new Vector3(-25,1.7f,8),new Vector3(-26.6f,1.05f,10));
        }
        private static void Shot(Camera camera,string name,Vector3 position,Vector3 look)
        {
            camera.transform.position=position;camera.transform.LookAt(look);camera.fieldOfView=72;
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var result=new Texture2D(1280,720,TextureFormat.RGB24,false);result.ReadPixels(new Rect(0,0,1280,720),0,0);result.Apply();
            File.WriteAllBytes("TestResults/Rewards/"+name+".png",result.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;
            Object.DestroyImmediate(result);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
