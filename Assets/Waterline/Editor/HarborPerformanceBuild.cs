using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Waterline.Editor
{
    public static class HarborPerformanceBuild
    {
        [Serializable] private sealed class Identity
        {
            public string scene, sceneSha256, runtimeSha256, executableSha256;
        }
        private static string Hash(string path)
        {
            using(var stream=File.OpenRead(path))using(var algorithm=SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
        }
        public static void Build()
        {
            CirculationBuilder.Open();CirculationBuilder.Validate();
            Directory.CreateDirectory("Builds/Benchmark");
            File.WriteAllText("Builds/Benchmark/render-pipeline.json",EditorJsonUtility.ToJson(GraphicsSettings.currentRenderPipeline,true));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CirculationBuilder.ScenePath},locationPathName="Builds/Benchmark/Waterline.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development,extraScriptingDefines=new[]{"WATERLINE_BENCHMARK"}});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Benchmark build failed");
            File.WriteAllText("Builds/Benchmark/build-identity.json",JsonUtility.ToJson(new Identity{
                scene=CirculationBuilder.ScenePath,sceneSha256=Hash(CirculationBuilder.ScenePath),
                runtimeSha256=Hash("Builds/Benchmark/Waterline_Data/Managed/Waterline.Runtime.dll"),
                executableSha256=Hash("Builds/Benchmark/Waterline.exe")},true));
            Debug.Log("[Waterline] BENCHMARK BUILD PASS");
        }
    }
}
