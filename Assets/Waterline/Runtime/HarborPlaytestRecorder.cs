using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Waterline.Core;

namespace Waterline
{
    // Adds no scene objects, colliders or input bindings beyond this data-only component.
    public sealed class HarborPlaytestRecorder : MonoBehaviour
    {
        private HarborLayout layout;
        private DockSession dock;
        private PlaytestTrace trace;
        private double started, lastWrite;
        private string reportPath;
        private bool testMode, writeWarning;
        public PlaytestTrace.Report Report { get { return trace == null ? null : trace.Data; } }
        public string ReportPath { get { return reportPath; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (Application.isEditor || Application.isBatchMode ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "--waterline-benchmark") >= 0 ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "--no-playtest-recording") >= 0) return;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool automated = Array.IndexOf(Environment.GetCommandLineArgs(), "--automated-playtest") >= 0;
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PlaytestReports"));
            if (automated) directory = Path.Combine(directory, "Automated");
            foreach (var root in scene.GetRootGameObjects())
            foreach (var link in root.GetComponentsInChildren<HarborReturnLink>())
            {
                if (link.GetComponent<HarborPlaytestRecorder>() == null)
                    link.gameObject.AddComponent<HarborPlaytestRecorder>().Begin(automated ? "automated_test" : "interactive_player", directory);
            }
        }

        private void Begin(string source, string directory)
        {
            layout = GetComponent<HarborLayout>(); dock = layout.dock;
            started = Time.realtimeSinceStartupAsDouble;
            string id = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            reportPath = Path.Combine(directory, id + ".json");
            trace = new PlaytestTrace(new PlaytestTrace.Report { sessionId = id, startedUtc = DateTime.UtcNow.ToString("O"),
                source = source, scene = gameObject.scene.name, buildGuid = Application.buildGUID, unityVersion = Application.unityVersion });
            Capture(); Save();
        }

#if UNITY_EDITOR
        public void BeginAutomatedTest(string directory) { testMode = true; Begin("automated_test", directory); }
#endif

        private void LateUpdate()
        {
            if (trace == null || trace.Finished) return;
            Capture();
            if (dock.State.Phase == DockPhase.Escaped) { End("escaped"); return; }
            if (Time.realtimeSinceStartupAsDouble - lastWrite >= 30) Save();
        }

        private void Capture()
        {
            if (trace == null || trace.Finished || dock == null || dock.State == null || dock.playerTransform == null || dock.threat == null ||
                layout == null || layout.annex == null || layout.annex.State == null) return;
            bool journal = dock.MapVisible && layout.annex.Journal != null && layout.annex.Journal.Showing;
            bool map = dock.MapVisible && !journal;
            string mode = dock.State.Phase == DockPhase.Escaped ? "escaped" : dock.Caught ? "caught" :
                !testMode && !Application.isFocused ? "unfocused" : layout.annex.NoteOpen ? "note" :
                journal ? "journal" : map ? "map" : dock.DebugVisible ? "debug" : dock.Paused ? "paused" : "playing";
            var p = dock.playerTransform.position;
            var room = layout.RoomAt(p);
            trace.Observe(new PlaytestTrace.Sample { seconds = Time.realtimeSinceStartupAsDouble - started, stage = Stage(dock, layout.annex.State),
                room = room == null ? "unmapped" : room.id, mode = mode, attempt = dock.threat.Attempts,
                map = map, journal = journal, debug = dock.DebugVisible, x = p.x, z = p.z });
        }

        private static string Stage(DockSession dock, AnnexState s)
        {
            var d = dock.State;
            if (d.Phase == DockPhase.Escaped) return "escaped";
            if (d.Phase == DockPhase.Flooded) return "board_boat";
            if (d.Phase == DockPhase.Filling) return "wait_water";
            if (dock.threat.FloodRequested) return "wait_evacuation";
            if (!d.HasPin && !d.PinInstalled) return "find_pin";
            if (!d.GateUnlocked) return "unlock_dock";
            if (!d.BridgeDeployed) return "deploy_bridge";
            if (!d.PinInstalled) return "repair_pump";
            if (!s.PowerRestored) return s.HasFuse ? "restore_power" : "find_fuse";
            if (!s.HasSignalKey) return "find_signal_key";
            if (!s.GaugeAligned) return s.HasCalibrationPlate ? "align_gauge" : "find_calibration_plate";
            if (!s.RecordRead) return "read_tide";
            if (!s.PressureBalanced) return "set_pressure";
            return "return_to_dock";
        }

        public void Save()
        {
            if (trace == null) return;
            lastWrite = Time.realtimeSinceStartupAsDouble;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                trace.Data.updatedUtc = DateTime.UtcNow.ToString("O");
                string temp = reportPath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(trace.Data, true), new UTF8Encoding(false));
                if (File.Exists(reportPath)) File.Replace(temp, reportPath, null); else File.Move(temp, reportPath);
                writeWarning = false;
            }
            catch (Exception error)
            {
                if (!writeWarning) Debug.LogWarning("[Waterline] Local playtest report could not be saved: " + error.Message);
                writeWarning = true;
            }
        }

        private void End(string status)
        {
            if (trace == null || trace.Finished) return;
            Capture();
            if (dock != null && dock.State != null && dock.State.Phase == DockPhase.Escaped) status = "escaped";
            trace.Finish(status); Save();
        }
        private void OnApplicationQuit() { End("quit_before_escape"); }
        private void OnDestroy() { End("scene_closed_before_escape"); }
    }
}
