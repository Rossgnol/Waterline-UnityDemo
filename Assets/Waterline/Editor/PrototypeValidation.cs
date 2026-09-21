using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Waterline.Core;
using Waterline.Checks;

namespace Waterline.Editor
{
    public static class PrototypeValidation
    {
        [MenuItem("Waterline/03 Check scene and rules")]
        public static void Validate()
        {
            int checks = DockStateChecks.RunAll();
            var sessions = UnityEngine.Object.FindObjectsByType<DockSession>(FindObjectsSortMode.None);
            Require(sessions.Length == 1, "Expected exactly one DockSession.");
            var session = sessions[0];
            Require(session.tuning != null, "Missing DockTuning asset.");
            Require(session.water != null && session.boat != null && session.bridge != null && session.serviceDoor != null,
                "DockSession has unassigned scene references.");
            Require(session.fullWaterHeight > session.dryWaterHeight, "Water height range is reversed.");
            var players = UnityEngine.Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None);
            Require(players.Length == 1 && players[0].view != null && players[0].session == session, "Player references are invalid.");
            var actions = new HashSet<DockAction>();
            foreach (var item in UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None))
            {
                Require(item.session == session, "Interaction has the wrong session: " + item.name);
                Require(item.GetComponent<Collider>() != null, "Interaction needs a collider: " + item.name);
                Require(!string.IsNullOrWhiteSpace(item.displayName), "Interaction needs a display name.");
                Require(actions.Add(item.action), "Duplicate action in this exercise: " + item.action);
            }
            Require(actions.Count == Enum.GetValues(typeof(DockAction)).Length, "One or more required actions are missing.");
            Debug.Log("[Waterline] PASS: " + checks + " rule checks, all scene references, six required interactions.");
        }

        [MenuItem("Waterline/04 Select exercise settings")]
        public static void SelectTuning()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<DockSession>();
            Selection.activeObject = session != null ? session.tuning :
                AssetDatabase.LoadAssetAtPath<DockTuning>("Assets/Waterline/Settings/DockTuning.asset");
            if (Selection.activeObject != null) EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Waterline/05 Build Windows demo")]
        public static void BuildWindows()
        {
            if (!File.Exists(PrototypeBuilder.ScenePath)) PrototypeBuilder.Build();
            else PrototypeBuilder.Open();
            Validate();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrototypeBuilder.ScenePath },
                locationPathName = "Builds/Windows/Waterline.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("[Waterline] Windows build ready: Builds/Windows/Waterline.exe");
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException("[Waterline validation] " + message);
        }
    }
}
