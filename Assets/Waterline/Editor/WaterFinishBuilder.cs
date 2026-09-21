using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class WaterFinishBuilder
    {
        [MenuItem("Waterline/31 Refine harbor water")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();var layout=Object.FindFirstObjectByType<HarborLayout>();
            var before=Signature();var shader=Shader.Find("Waterline/Harbor Water");
            if(shader==null || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Water shader missing or invalid");
            const string folder="Assets/Waterline/Generated/WaterFinish";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string path=folder+"/Harbor water.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;
            material.SetColor("_BaseColor",new Color(.025f,.13f,.145f));material.SetFloat("_Smoothness",.73f);material.SetFloat("_RippleStrength",.07f);material.SetFloat("_RippleTime",-1);EditorUtility.SetDirty(material);
            foreach(var t in new[]{layout.dock.water,layout.LowerDeck.water})t.GetComponent<Renderer>().sharedMaterial=material;
            if(before!=Signature())throw new InvalidOperationException("Water art changed physics, light settings or gameplay bindings");
            TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] WATER FINISH PASS: two water renderers share local dielectric ripple material; original physics, lights and session bindings unchanged.");
        }
        private static string Signature()
        {
            var a=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5"));
            var b=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5"));
            var d=Object.FindFirstObjectByType<DockSession>();var h=Object.FindFirstObjectByType<HarborLayout>();
            return string.Join("\n",a.Concat(b))+EditorJsonUtility.ToJson(d)+EditorJsonUtility.ToJson(h.LowerDeck)+EditorJsonUtility.ToJson(h.Planner);
        }
    }

    [InitializeOnLoad] public static class WaterVisualCheck
    {
        private const string Key="Waterline.WaterVisual";
        private static HarborLayout layout;
        private static FirstPersonController player;
        private static Material preview;
        private static int step;
        private static double next,deadline;
        private static string Folder=>"TestResults/WaterFinish/"+SessionState.GetString(Key+"Phase","after");
        static WaterVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Before(){Start("before");}
        public static void After(){Start("after");}
        internal static void Start(string phase){TidalRouteBuilder.Open();SessionState.SetString(Key+"Phase",phase);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            step=0;next=EditorApplication.timeSinceStartup+2;deadline=next+40;EditorApplication.update+=Advance;
        }
        private static void Advance()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"Water capture timeout");if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
                switch(step++)
                {
                    case 0:
                        Directory.CreateDirectory(Folder);layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
                        player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;layout.dock.Paused=true;
                        foreach(var e in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){e.enabled=false;e.body.gameObject.SetActive(false);}
                        var source=layout.dock.water.GetComponent<Renderer>().sharedMaterial;
                        if(Folder.EndsWith("after"))Require(source.shader.name=="Waterline/Harbor Water" && !ShaderUtil.ShaderHasError(source.shader),"Water shader failed");
                        preview=new Material(source);if(preview.HasProperty("_RippleTime"))preview.SetFloat("_RippleTime",8);
                        foreach(var t in new[]{layout.dock.water,layout.LowerDeck.water})t.GetComponent<Renderer>().sharedMaterial=preview;
                        Shots("dry");
                        foreach(var action in new[]{Waterline.Core.DockAction.TakePin,Waterline.Core.DockAction.InstallPin,Waterline.Core.DockAction.UnlockGate,Waterline.Core.DockAction.DeployBridge,Waterline.Core.DockAction.StartFlood})Require(layout.dock.State.TryApply(action,out _),"Cannot stage flood");
                        layout.annex.State.FloatIntakeOpen=true;layout.annex.State.FloatDrainClosed=true;layout.annex.State.TryApply(Waterline.Core.AnnexAction.TestFloatCircuit,out _);layout.dock.State.Tick(6,12);layout.LowerDeck.Apply();layout.Planner.Apply();break;
                    case 1:CheckHeight(.5f);Shots("rising");layout.dock.State.Tick(6,12);layout.LowerDeck.Apply();layout.Planner.Apply();break;
                    case 2:CheckHeight(1);Require(layout.LowerDeck.Raised && layout.Planner.boardingGangway.activeSelf,"Flood presentation missing");Shots("flooded");
                        BoatPresentationBuilder.Shot(player.view,Folder+"/ripples-t0.png",new Vector3(5,1,-2),new Vector3(4,-.6f,-4));
                        if(preview.HasProperty("_RippleTime"))preview.SetFloat("_RippleTime",10);break;
                    case 3:BoatPresentationBuilder.Shot(player.view,Folder+"/ripples-t1.png",new Vector3(5,1,-2),new Vector3(4,-.6f,-4));break;
                    default:
                        Require(!ShaderUtil.ShaderHasError(preview.shader),"Water shader compile error after render");
                        if(preview.HasProperty("_RippleTime"))Require(!File.ReadAllBytes(Folder+"/ripples-t0.png").SequenceEqual(File.ReadAllBytes(Folder+"/ripples-t1.png")),"Ripples did not animate");
                        Debug.Log("[Waterline] WATER VISUAL PASS: "+Folder+"; dry/rising/flooded surfaces, unchanged heights, prepared float and boarding bridge; inspect captures.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void CheckHeight(float progress)
        {
            float y=Mathf.Lerp(layout.dock.dryWaterHeight,layout.dock.fullWaterHeight,progress);
            Require(Mathf.Abs(layout.dock.water.position.y-y)<.01f && Mathf.Abs(layout.LowerDeck.water.position.y-y)<.01f,"Water height changed");
        }
        private static void Shots(string state)
        {
            BoatPresentationBuilder.Shot(player.view,Folder+"/"+state+"-dock.png",new Vector3(4,-1.1f+layout.dock.State.WaterProgress*2.45f,-7),new Vector3(0,-1.8f+layout.dock.State.WaterProgress*2.45f,-1));
            BoatPresentationBuilder.Shot(player.view,Folder+"/"+state+"-hall.png",new Vector3(-8,1.7f,8.65f),new Vector3(0,-1.3f,8.65f));
            if(state=="flooded")BoatPresentationBuilder.Shot(player.view,Folder+"/flooded-maintenance.png",new Vector3(-10,-.3f,8),new Vector3(-10,-1,16));
        }
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Advance;if(preview!=null)Object.DestroyImmediate(preview);EditorApplication.Exit(code);}
    }
}
