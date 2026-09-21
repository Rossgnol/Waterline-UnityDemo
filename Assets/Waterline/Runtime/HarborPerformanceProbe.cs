#if WATERLINE_BENCHMARK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Waterline.Core;

namespace Waterline
{
    // Compiled only into the separate diagnostic player. Never present in the normal demo build.
    public sealed class HarborPerformanceProbe : MonoBehaviour
    {
        [Serializable] public sealed class Segment
        {
            public string name; public int frames, unfocusedFrames, renderedFrames;
            public double meanMs, medianMs, p95Ms, p99Ms, maxMs, over16MsPercent, over33MsPercent;
        }
        [Serializable] public sealed class Report
        {
            public string utc, unity, cpu, gpu, graphicsApi, quality, pipeline, method, invalidReason, scene, buildGuid;
            public int width,height,ramMB,vramMB,vSync,targetFps,guardCount;
            public bool development, batchMode, valid; public double warmupSeconds=8,sampleSeconds=12;
            public List<Segment> segments=new List<Segment>();
        }
        private struct Sample { public double ms;public bool focused; }
        private readonly List<Sample> samples=new List<Sample>(60000);
        private readonly StringBuilder csv=new StringBuilder("segment,frame,elapsed_ms,focused\n");
        private Camera view; private bool measuring;private double previous;
        private string output;private Report report;
        private Vector3 anchor,look;private double sweepStart;
        private int renderedFrames;
        private void OnEnable(){RenderPipelineManager.endFrameRendering+=Rendered;}
        private void OnDestroy(){RenderPipelineManager.endFrameRendering-=Rendered;}
        private void Rendered(ScriptableRenderContext context,Camera[] cameras)
        {if(measuring && Array.IndexOf(cameras,view)>=0)renderedFrames++;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(Environment.GetCommandLineArgs().Contains("--waterline-benchmark"))new GameObject("Performance benchmark").AddComponent<HarborPerformanceProbe>();
        }
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--benchmark-output");
            if(index<0 || index+1>=args.Length){Debug.LogError("Missing benchmark output path");Application.Quit(2);yield break;}
            output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null || Application.isBatchMode){Debug.LogError("Benchmark requires a real graphics player, no batchmode/nographics");Application.Quit(2);yield break;}
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            yield return null;yield return null;
            var player=FindFirstObjectByType<FirstPersonController>();view=player.view;player.enabled=false;
            player.ResetPose(new Vector3(-11,.03f,10),0); // Safe hall avatar; camera independently surveys the level.
            var dock=player.session;
            foreach(var action in new[]{DockAction.TakePin,DockAction.InstallPin,DockAction.UnlockGate,DockAction.DeployBridge})dock.State.TryApply(action,out _);
            var state=dock.annex.State;state.PowerRestored=true;state.HasSignalKey=true;
            dock.annex.Apply();dock.annex.Harbor.Apply();
            report=new Report{utc=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,
                scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,buildGuid=Application.buildGUID,
                graphicsApi=SystemInfo.graphicsDeviceVersion,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],pipeline=GraphicsSettings.currentRenderPipeline.name,
                ramMB=SystemInfo.systemMemorySize,vramMB=SystemInfo.graphicsMemorySize,development=Debug.isDebugBuild,batchMode=Application.isBatchMode,
                width=Screen.width,height=Screen.height,vSync=QualitySettings.vSyncCount,targetFps=Application.targetFrameRate,guardCount=FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None).Length,
                method="1920x1080 windowed diagnostic Development player; uncapped; realtime LateUpdate intervals. Fixed camera stations with slow sweep; avatar in safe hall; four active patrols; gameplay/HUD updates enabled; input controller disabled. Not a player traversal or GPU timing."};
            string[] names={"dock","hall","workshop","east-corridor","tide"};
            Vector3[] positions={new Vector3(-10,1.7f,-7),new Vector3(-11,1.7f,9),new Vector3(-23,1.7f,34),new Vector3(17,1.7f,20),new Vector3(18,1.7f,61)};
            Vector3[] targets={new Vector3(4,-1,-3),new Vector3(0,2.3f,17),new Vector3(-27,1.7f,42),new Vector3(17,1.5f,48),new Vector3(23,1.8f,67)};
            for(int i=0;i<names.Length;i++)
            {
                anchor=positions[i];look=targets[i];sweepStart=Time.realtimeSinceStartupAsDouble;
                Debug.Log("[Benchmark] Warmup "+names[i]);
                yield return new WaitForSecondsRealtime(2);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,names[i]+".png"));
                yield return new WaitForSecondsRealtime(6); // Screenshot IO and first-use shaders excluded.
                samples.Clear();renderedFrames=0;previous=-1;measuring=true;
                yield return new WaitForSecondsRealtime(12);measuring=false;
                Summarize(names[i]);Debug.Log("[Benchmark] Sampled "+names[i]);
            }
            report.width=Screen.width;report.height=Screen.height;
            report.valid=report.width==1920 && report.height==1080 && report.segments.All(s=>s.frames>=60 && s.unfocusedFrames==0 && s.renderedFrames>=s.frames*.95);
            report.invalidReason=report.valid?"":"Resolution, focus, sample count or render callback coverage failed; inspect segments and screenshots.";
            File.WriteAllText(Path.Combine(output,"frames.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));
            Debug.Log("[Benchmark] COMPLETE valid="+report.valid);Application.Quit(report.valid?0:3);
        }
        private void LateUpdate()
        {
            if(view==null)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(measuring){if(previous>=0)samples.Add(new Sample{ms=(now-previous)*1000,focused=Application.isFocused});previous=now;}
            float angle=Mathf.Sin((float)(now-sweepStart)*.45f)*18;
            view.transform.position=anchor;view.transform.rotation=Quaternion.AngleAxis(angle,Vector3.up)*Quaternion.LookRotation(look-anchor);
            view.fieldOfView=72;
        }
        private void Summarize(string name)
        {
            var sorted=samples.Select(s=>s.ms).OrderBy(x=>x).ToArray();if(sorted.Length==0)throw new InvalidOperationException("No measured frames");
            double Percentile(double p){return sorted[Math.Max(0,(int)Math.Ceiling(p*sorted.Length)-1)];}
            report.segments.Add(new Segment{name=name,frames=sorted.Length,renderedFrames=renderedFrames,unfocusedFrames=samples.Count(s=>!s.focused),meanMs=sorted.Average(),medianMs=Percentile(.5),p95Ms=Percentile(.95),p99Ms=Percentile(.99),maxMs=sorted.Last(),over16MsPercent=sorted.Count(v=>v>1000.0/60)*100.0/sorted.Length,over33MsPercent=sorted.Count(v=>v>1000.0/30)*100.0/sorted.Length});
            for(int i=0;i<samples.Count;i++)csv.Append(name).Append(',').Append(i).Append(',').Append(samples[i].ms.ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(samples[i].focused?1:0).Append('\n');
        }
    }
}
#endif
