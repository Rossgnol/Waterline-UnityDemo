using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    // Controlled measurements of the shipped behaviour. No scene or tuning asset is saved.
    [InitializeOnLoad]
    public static class ThreatComparison
    {
        private const string Key = "Waterline.Comparison.Active";
        private const float Dt = .02f;
        private const string Output = "Docs/Portfolio/Evidence";
        private static IEnumerator routine;
        private static double deadline;
        private static ThreatEncounter enemy;
        private static ThreatTuning baseline, working;

        [Serializable] public sealed class RecognitionResult
        {
            public float configuredSeconds, firstChaseSeconds;
            public bool chasedWithinPointEightSeconds;
        }
        [Serializable] public sealed class SearchResult
        {
            public float configuredSeconds, returnToPatrolSeconds;
        }
        [Serializable] public sealed class NoiseResult
        {
            public string ground, motion;
            public float sourceDistance, radius;
            public bool heard;
        }
        [Serializable] public sealed class SourceFingerprint { public string path, sha256; }
        [Serializable] public sealed class Report
        {
            public string generatedUtc, unityVersion;
            public float stepSeconds = Dt;
            public string method = "Deterministic Play Mode experiment; manually advance the real enemy behaviour with fixed dt; not human playtest statistics.";
            public string controls = "Runtime tuning clone; retry grace disabled for every case; stationary player; reset before each case; no asset saves.";
            public List<RecognitionResult> recognition = new List<RecognitionResult>();
            public List<SearchResult> search = new List<SearchResult>();
            public List<NoiseResult> noise = new List<NoiseResult>();
            public List<SourceFingerprint> sources = new List<SourceFingerprint>();
        }

        static ThreatComparison() { EditorApplication.playModeStateChanged += OnPlay; }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run Tools/Measure-Threat.ps1 in a separate batch editor.");
            ThreatBuilder.Open(); ThreatBuilder.Validate();
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }

        private static void OnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 90;
            routine = Measure(); EditorApplication.update += Step;
        }

        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Experiment timed out.");
                if (routine.MoveNext()) return;
                Debug.Log("[Waterline] COMPARISON PASS: recognition, search duration and surface hearing measured; defaults unchanged.");
                Finish(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        private static void Finish(int code)
        {
            if (enemy != null && baseline != null) enemy.tuning = baseline;
            if (working != null) UnityEngine.Object.Destroy(working);
            SessionState.SetBool(Key, false); EditorApplication.update -= Step; EditorApplication.Exit(code);
        }

        private static IEnumerator Measure()
        {
            yield return null;
            enemy = UnityEngine.Object.FindFirstObjectByType<ThreatEncounter>();
            enemy.enabled = false; enemy.player.enabled = false;
            baseline = enemy.tuning;
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
            foreach (float seconds in new[] { baseline.recognitionSeconds, .5f })
            {
                Reset(new Vector3(-3, -3.47f, 3.7f)); working.recognitionSeconds = seconds;
                Require(enemy.CanSeePlayer(), "Recognition fixture must have a clear sight line.");
                int ticks = 0;
                while (enemy.Mode != ThreatMode.Chase && ticks < 200) { enemy.Advance(Dt); ticks++; }
                Require(enemy.Mode == ThreatMode.Chase, "Recognition never entered chase.");
                var result = new RecognitionResult { configuredSeconds = seconds, firstChaseSeconds = ticks * Dt };
                Reset(new Vector3(-3, -3.47f, 3.7f)); working.recognitionSeconds = seconds;
                for (int tick = 0; tick < 40; tick++)
                {
                    enemy.Advance(Dt);
                    result.chasedWithinPointEightSeconds |= enemy.Mode == ThreatMode.Chase;
                }
                report.recognition.Add(result);
            }
            Require(!report.recognition[0].chasedWithinPointEightSeconds && report.recognition[1].chasedWithinPointEightSeconds,
                "The .8-second exposure must distinguish the accepted baseline and faster recognition.");

            foreach (float seconds in new[] { baseline.searchSeconds, 8f })
            {
                Reset(new Vector3(-8, -3.47f, 0)); working.searchSeconds = seconds;
                Require(enemy.HearNoise(enemy.transform.position, 2), "Search fixture sound was not received.");
                int ticks = 0;
                while (enemy.Mode != ThreatMode.Patrol && ticks < 600) { enemy.Advance(Dt); ticks++; }
                Require(enemy.Mode == ThreatMode.Patrol, "Search did not finish.");
                report.search.Add(new SearchResult { configuredSeconds = seconds, returnToPatrolSeconds = ticks * Dt });
            }
            Require(report.search[1].returnToPatrolSeconds > report.search[0].returnToPatrolSeconds + 3.9f,
                "Longer search did not increase time before returning to patrol.");

            // Inject the same sound location with the radius produced by each ground/motion rule.
            // This isolates hearing from vision and stride frequency; it is not a player walking trial.
            foreach (GroundKind ground in new[] { GroundKind.Metal, GroundKind.Rubber })
            foreach (MotionKind motion in new[] { MotionKind.Quiet, MotionKind.Walk, MotionKind.Run })
            {
                Reset(new Vector3(-3, -3.47f, 3.7f));
                float radius = NoiseRules.Radius(ground, motion);
                report.noise.Add(new NoiseResult { ground = ground.ToString(), motion = motion.ToString(),
                    sourceDistance = Vector3.Distance(enemy.transform.position, enemy.player.transform.position), radius = radius,
                    heard = enemy.HearNoise(enemy.player.transform.position, radius) });
            }
            Require(!report.noise[0].heard && report.noise[1].heard && report.noise[2].heard &&
                !report.noise[3].heard && !report.noise[4].heard && !report.noise[5].heard, "Unexpected six-metre hearing results.");
            foreach (string path in new[] { "Assets/Waterline/Runtime/ThreatEncounter.cs", "Assets/Waterline/Core/NoiseRules.cs",
                "Assets/Waterline/Settings/ThreatTuning.asset", ThreatBuilder.ScenePath, "Assets/Waterline/Editor/ThreatComparison.cs" })
            {
                using (var sha = SHA256.Create())
                    report.sources.Add(new SourceFingerprint { path = path, sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() });
            }
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/comparison.json", JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            File.WriteAllText(Output + "/Comparison.md", Markdown(report), new UTF8Encoding(false));
        }

        private static void Reset(Vector3 playerPosition)
        {
            if (working != null) UnityEngine.Object.Destroy(working);
            working = UnityEngine.Object.Instantiate(baseline); working.retryGraceSeconds = 0;
            enemy.tuning = working;
            var controller = enemy.player.GetComponent<CharacterController>();
            controller.enabled = false; enemy.player.transform.position = playerPosition; controller.enabled = true;
            enemy.ResetEncounter(); Physics.SyncTransforms();
            Require(!enemy.session.InputBlocked, "Experiment must run in an active session.");
        }

        private static string Number(float value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static string Markdown(Report report)
        {
            var s = new StringBuilder();
            s.AppendLine("# 敌人参数对比：Unity 实测记录\n");
            s.AppendLine("由 `ThreatComparison` 自动生成；原始数据和源码 SHA-256 见 [comparison.json](comparison.json)。");
            s.AppendLine("\n生成时间（UTC）：" + report.generatedUtc + "；Unity：" + report.unityVersion + "；逻辑步长：0.02 秒。");
            s.AppendLine("\n## 方法与控制条件\n");
            s.AppendLine("在第三轮真实场景的 Play Mode 中调用原敌人逻辑，以固定时间步推进，使用真实射线与碰撞。关闭自动 Update 与玩家输入；只修改运行时参数副本，统一关闭出生感知宽限，每个样本前重置。结果是固定条件下的行为测量，不是玩家研究、平均通关数据或难度评分。时间为累计逻辑时间，不是批处理墙钟时间，阈值可有一个时间步的误差。");
            s.AppendLine("\n## 识别窗口\n\n玩家与敌人在下层北侧相距 6 米，正面无遮挡，玩家静止。每个参数分别测量持续暴露到首次追逐的时间，以及独立的 0.8 秒暴露是否触发追逐。\n");
            s.AppendLine("| 配置识别时间 | 首次追逐时间 | 0.8 秒暴露内追逐 |\n|---|---|---|");
            foreach (var r in report.recognition) s.AppendLine("| " + Number(r.configuredSeconds) + " s | " + Number(r.firstChaseSeconds) + " s | " + (r.chasedWithinPointEightSeconds ? "是" : "否") + " |");
            s.AppendLine("\n## 搜索窗口\n\n玩家位于安全工具间，在敌人当前导航节点注入声音，排除前往调查点的路程。记录收到声音后到恢复巡查的时间。\n");
            s.AppendLine("| 配置搜索时长 | 恢复巡查时间 |\n|---|---|");
            foreach (var r in report.search) s.AppendLine("| " + Number(r.configuredSeconds) + " s | " + Number(r.returnToPatrolSeconds) + " s |");
            s.AppendLine("\n## 地面与移动声音\n\n声源固定在下层北侧 6 米无遮挡位置，调用各地面与移动方式的半径规则注入声音，隔离视觉与步频影响。此表不代表在两条实际路线中各完成一次行走。\n");
            s.AppendLine("| 地面 | 移动 | 声音半径 | 敌人收到声音 |\n|---|---|---|---|");
            foreach (var r in report.noise) s.AppendLine("| " + (r.ground == "Metal" ? "金属" : "橡胶") + " | " +
                (r.motion == "Quiet" ? "轻步" : r.motion == "Walk" ? "步行" : "快走") + " | " + Number(r.radius) + " m | " + (r.heard ? "是" : "否") + " |");
            s.AppendLine("\n## 结论边界\n\n缩短识别窗口会让相同的短时暴露触发追逐；延长搜索增加调查点附近的停留时间；相同距离下声音规则会改变调查触发结果。当前保留经过用户试玩的 1 秒识别、4 秒搜索默认配置。尚不能据此断言任何一组参数更好玩、能提高留存，或橡胶路线一定安全。\n\n复现：关闭此工程的 Unity 编辑器后运行 `Tools/Measure-Threat.ps1`。脚本会覆盖本目录测量记录，失败时以日志与非零退出码为准。");
            return s.ToString();
        }
        private static void Require(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("[Comparison] " + message); }
    }
}
