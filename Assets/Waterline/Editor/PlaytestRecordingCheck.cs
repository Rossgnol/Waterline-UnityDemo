using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class PlaytestRecordingCheck
    {
        private const string Key = "Waterline.PlaytestRecordingCheck";
        private static IEnumerator routine;
        private static double deadline;
        static PlaytestRecordingCheck() { EditorApplication.playModeStateChanged += OnPlay; }
        public static void Run()
        {
            CheckAccounting(); CirculationBuilder.Open(); CirculationBuilder.Validate();
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void OnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            routine = CheckScene(); deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Step;
        }
        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Timeout");
                if (routine.MoveNext()) return;
                Debug.Log("[Waterline] PLAYTEST RECORDING PASS: accounting, real UI state, retry teleport, stage sampling, atomic replacement, escape persistence and scene close. Automated data only.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Finish(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Step; EditorApplication.Exit(code); }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Near(double actual, double expected, string message) { Require(Math.Abs(actual - expected) < .0001, message + ": " + actual); }

        private static void CheckAccounting()
        {
            var trace = new PlaytestTrace(new PlaytestTrace.Report { source = "automated_test" });
            var s = new PlaytestTrace.Sample { stage = "find", room = "a", mode = "playing", attempt = 1 };
            trace.Observe(s);
            s.seconds = 1; s.x = 1; trace.Observe(s);
            s.seconds = 2; s.mode = "map"; s.map = true; trace.Observe(s);
            s.seconds = 20; trace.Observe(s);
            s.seconds = 21; s.mode = "journal"; s.map = false; s.journal = true; trace.Observe(s);
            s.seconds = 22; s.mode = "map"; s.map = true; s.journal = false; trace.Observe(s);
            s.seconds = 23; s.mode = "unfocused"; trace.Observe(s);
            s.seconds = 24; s.mode = "map"; trace.Observe(s);
            s.seconds = 25; s.mode = "playing"; s.map = false; trace.Observe(s);
            s.seconds = 27; s.x = 3; trace.Observe(s);
            s.seconds = 28; s.mode = "paused"; trace.Observe(s);
            s.seconds = 30; trace.Observe(s);
            s.seconds = 31; s.mode = "playing"; trace.Observe(s);
            s.seconds = 32; s.x = 4; trace.Observe(s);
            s.seconds = 33; s.mode = "caught"; trace.Observe(s);
            s.seconds = 38; trace.Observe(s);
            s.seconds = 39; s.mode = "playing"; s.room = "b"; s.attempt = 2; s.x = 500; trace.Observe(s);
            s.seconds = 40; s.x = 501; trace.Observe(s);
            s.seconds = 41; s.stage = "next"; s.x = 502; trace.Observe(s);
            s.seconds = 42; s.x = 503; trace.Observe(s);
            s.seconds = 43; s.x = 900; trace.Observe(s);
            s.seconds = 44; s.mode = "debug"; s.debug = true; trace.Observe(s);
            s.seconds = 45; s.mode = "playing"; s.debug = false; trace.Observe(s);
            s.seconds = 46; s.x = 901; trace.Observe(s);
            s.seconds = 47; s.mode = "escaped"; trace.Observe(s);
            trace.Finish("escaped"); s.seconds = 48; trace.Observe(s); trace.Finish("quit_before_escape");
            var r = trace.Data;
            Near(r.elapsedSeconds, 47, "Finished trace changed"); Near(r.activeSeconds, 9, "Paused/focus/capture time included");
            Near(r.horizontalMetres, 8, "Teleport or discontinuity distance included");
            Near(r.stages.Single(v => v.id == "find").activeSeconds, 6, "Previous stage attribution");
            Near(r.stages.Single(v => v.id == "next").activeSeconds, 3, "Next stage attribution");
            Near(r.rooms.Single(v => v.id == "b").activeSeconds, 5, "Room attribution");
            Require(r.mapOpens == 2 && r.journalOpens == 1, "Map and journal/focus counts conflated");
            Require(r.retries == 1 && r.captures == 1 && r.movementDiscontinuities == 1, "Retry/capture/gap counts");
            Require(r.rooms.Single(v => v.id == "b").retryArrivals == 1 && r.rooms.Single(v => v.id == "b").entries == 0, "Retry marked as walked entry");
            Require(r.debugUsed && r.status == "escaped", "Debug taint or escape status lost");
            Require(r.events.Any(v => v.kind == "room_exit" && v.reason == "retry"), "Missing retry exit reason");
            var bounded = new PlaytestTrace(new PlaytestTrace.Report());
            for (int i = 0; i < 11000; i++)
                bounded.Observe(new PlaytestTrace.Sample { seconds = i, stage = "x", room = "x", mode = i % 2 == 0 ? "map" : "playing", attempt = 1 });
            Require(bounded.Data.events.Count == 10000 && bounded.Data.droppedEvents > 0, "Event storage unbounded");
            bool rejected = false;
            try { bounded.Observe(new PlaytestTrace.Sample { seconds = double.NaN }); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "Invalid time accepted");
            Debug.Log("[Waterline] PLAYTEST ACCOUNTING PASS: deterministic boundary and storage checks.");
        }

        private static IEnumerator Wait(double seconds)
        {
            double until = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < until) yield return null;
        }
        private static IEnumerator CheckScene()
        {
            yield return null; yield return null;
            Require(Object.FindFirstObjectByType<HarborPlaytestRecorder>() == null, "Editor auto-recording must be off");
            var h = Object.FindFirstObjectByType<HarborLayout>(); var dock = h.dock; var annex = h.annex;
            h.enabled = false;
            var player = Object.FindFirstObjectByType<FirstPersonController>(); player.enabled = false;
            foreach (var guard in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)) guard.enabled = false;
            var recorder = h.gameObject.AddComponent<HarborPlaytestRecorder>();
            string folder = Path.GetFullPath("TestResults/PlaytestRecording/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            recorder.BeginAutomatedTest(folder); string file = recorder.ReportPath;
            Require(File.Exists(file), "Initial report not written");
            var wait = Wait(.15); while (wait.MoveNext()) yield return null;
            Require(recorder.Report.activeSeconds > 0 && recorder.Report.stages[0].id == "find_pin", "Initial live samples missing");
            annex.Journal.Toggle(); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            double paused = recorder.Report.activeSeconds;
            wait = Wait(.15); while (wait.MoveNext()) yield return null;
            Near(recorder.Report.activeSeconds, paused, "Journal time included");
            Require(recorder.Report.journalOpens == 1 && recorder.Report.mapOpens == 0, "Live journal counted as map");
            annex.Journal.ShowMap(); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Require(recorder.Report.mapOpens == 1, "Live journal-to-map transition missed");
            Near(recorder.Report.activeSeconds, paused, "Map time included");
            annex.Journal.Close(); annex.NoteOpen = true;
            wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Near(recorder.Report.activeSeconds, paused, "Note time included");
            annex.NoteOpen = false; dock.Paused = true;
            wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Near(recorder.Report.activeSeconds, paused, "Pause time included"); dock.Paused = false;
            dock.State.TryApply(DockAction.TakePin, out _);
            wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Require(recorder.Report.stages.Any(v => v.id == "unlock_dock"), "Progress stage not sampled");
            dock.SaveCheckpoint(new Vector3(-10, .08f, 44), false);
            dock.CapturePlayer(); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            double beforeRetry = recorder.Report.horizontalMetres;
            dock.RetryCheckpoint(); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Near(recorder.Report.horizontalMetres, beforeRetry, "Live checkpoint teleport counted as movement");
            Require(recorder.Report.retries == 1 && recorder.Report.captures == 1, "Live retry/capture missed");
            Require(recorder.Report.rooms.Any(v => v.id == "rest" && v.retryArrivals == 1), "Checkpoint arrival not marked");
            recorder.Save();
            var checkpoint = JsonUtility.FromJson<PlaytestTrace.Report>(File.ReadAllText(file));
            Require(checkpoint.status == "in_progress" && checkpoint.source == "automated_test" && checkpoint.retries == 1, "Snapshot replacement failed or source mislabeled");
            Require(!File.Exists(file + ".tmp"), "Temporary snapshot was not committed");
            foreach (var action in new[] { DockAction.InstallPin, DockAction.UnlockGate, DockAction.DeployBridge, DockAction.StartFlood })
                Require(dock.State.TryApply(action, out _), "Fixture could not advance " + action);
            dock.State.Tick(10, 1); Require(dock.State.TryApply(DockAction.BoardBoat, out _), "Fixture could not escape");
            wait = Wait(.15); while (wait.MoveNext()) yield return null;
            string finished = File.ReadAllText(file);
            var report = JsonUtility.FromJson<PlaytestTrace.Report>(finished);
            Require(report.status == "escaped" && report.events.Last().kind == "session_end", "Escape did not persist");
            Object.Destroy(recorder); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Require(File.ReadAllText(file) == finished, "Destroy overwrote completed report");
            // A separate unfinished session should be sealed on scene/component destruction.
            dock.RetryCheckpoint();
            recorder = h.gameObject.AddComponent<HarborPlaytestRecorder>(); recorder.BeginAutomatedTest(folder);
            string unfinished = recorder.ReportPath;
            Require(unfinished != file, "Session report collision");
            Object.Destroy(recorder); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Require(JsonUtility.FromJson<PlaytestTrace.Report>(File.ReadAllText(unfinished)).status == "scene_closed_before_escape", "Unfinished session not sealed");
            recorder = h.gameObject.AddComponent<HarborPlaytestRecorder>(); recorder.BeginAutomatedTest(folder);
            string quit = recorder.ReportPath;
            recorder.SendMessage("OnApplicationQuit");
            Require(JsonUtility.FromJson<PlaytestTrace.Report>(File.ReadAllText(quit)).status == "quit_before_escape", "Quit callback did not save");
            Object.Destroy(recorder); wait = Wait(.1); while (wait.MoveNext()) yield return null;
            Require(JsonUtility.FromJson<PlaytestTrace.Report>(File.ReadAllText(quit)).status == "quit_before_escape", "Destroy overwrote quit status");
            Debug.Log("[Waterline] Automated report evidence: " + folder);
        }
    }
}
