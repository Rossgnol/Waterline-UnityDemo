using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // Diagnostic staging only: never saves the scene or grants gameplay progression.
    [InitializeOnLoad]
    public static class MapAccessAudit
    {
        private const string Key = "Waterline.MapAccessAudit";
        private static double next;
        [Serializable] private sealed class Approach
        {
            public string target, action, status;
            public Vector3 targetCenter, standingPosition;
        }
        [Serializable] private sealed class FloatCheck
        {
            public string state, status;
            public bool raised;
            public int activeBarriers;
            public float platformY;
        }
        [Serializable] private sealed class Traversal
        {
            public int direction;
            public string status;
            public Vector3 start, end, target;
        }
        [Serializable] private sealed class Report
        {
            public string method = "Saved Day11. Dry active interactables: grounded, clear standing capsule plus actual player interaction ray. Local candidate sampling does not prove a route from spawn. Then stage flooded states and cross prepared float with actual CharacterController. Guards/input disabled; not a full gameplay or performance test.";
            public List<Approach> approaches = new List<Approach>();
            public List<FloatCheck> floatStates = new List<FloatCheck>();
            public List<Traversal> floatCrossings = new List<Traversal>();
        }
        static MapAccessAudit() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run()
        {
            TidalRouteBuilder.Open();
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            next = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Check;
        }
        private static void FreezeGuards()
        {
            foreach (var guard in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))
            {
                guard.enabled = false;
                guard.GetComponent<CharacterController>().enabled = false;
                guard.body.gameObject.SetActive(false);
            }
        }
        private static void Check()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                var report = new Report();
                var h = Object.FindFirstObjectByType<HarborLayout>();
                h.enabled = false; h.dock.Paused = true; h.annex.enabled = false;
                var player = Object.FindFirstObjectByType<FirstPersonController>();
                player.enabled = false;
                var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
                FreezeGuards(); Physics.SyncTransforms();
                var targets = Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).Cast<Component>()
                    .Concat(Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None)).ToArray();
                foreach (var item in targets)
                {
                    var collider = item.GetComponent<Collider>();
                    var center = collider != null ? collider.bounds.center : item.transform.position;
                    var dockItem = item as DockInteractable;
                    var entry = new Approach { target = item.name, action = dockItem != null ? dockItem.action.ToString() : ((AnnexInteractable)item).action.ToString(), targetCenter = center, status = "inspect" };
                    float floor = center.y < -.6f ? -3.5f : 0;
                    foreach (float radius in new[] { .6f, .9f, 1.2f, 1.5f, 1.8f, 2.1f })
                    {
                        for (int angle = 0; angle < 24; angle++)
                        {
                            float radians = angle * Mathf.PI / 12;
                            var sample = new Vector3(center.x + Mathf.Cos(radians) * radius, floor + .4f, center.z + Mathf.Sin(radians) * radius);
                            if (!Physics.Raycast(sample, Vector3.down, out var ground, .9f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            if (ground.normal.y < .8f) continue;
                            var feet = ground.point + Vector3.up * .04f;
                            if (Physics.CheckCapsule(feet + Vector3.up * cc.radius, feet + Vector3.up * (cc.height - cc.radius), cc.radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                            player.transform.position = feet; Physics.SyncTransforms();
                            if (!player.RaycastInteraction((center - player.view.transform.position).normalized, out var hit)) continue;
                            if (hit.collider.GetComponentInParent<DockInteractable>() != item && hit.collider.GetComponentInParent<AnnexInteractable>() != item) continue;
                            entry.status = "pass"; entry.standingPosition = feet; break;
                        }
                        if (entry.status == "pass") break;
                    }
                    report.approaches.Add(entry);
                    if (entry.status != "pass") Debug.Log("[MAP ACCESS INSPECT] " + entry.target + " action=" + entry.action + " at=" + center);
                }
                RecordFloat(h, report, "dry-unprepared", false, -3.65f);
                foreach (var action in new[] { DockAction.TakePin, DockAction.InstallPin, DockAction.UnlockGate, DockAction.DeployBridge, DockAction.StartFlood })
                    if (!h.dock.State.TryApply(action, out var message)) throw new InvalidOperationException(action + ": " + message);
                h.dock.State.Tick(12, 12);
                RecordFloat(h, report, "flooded-unprepared", false, -3.65f);
                h.annex.State.FloatPrepared = true;
                RecordFloat(h, report, "flooded-prepared", true, -.15f);
                foreach (int direction in new[] { -1, 1 })
                {
                    var start = new Vector3(-5 * direction, .04f, 8.65f);
                    var target = new Vector3(5 * direction, .04f, 8.65f);
                    cc.enabled = false; player.transform.position = start; cc.enabled = true; Physics.SyncTransforms();
                    for (int i = 0; i < 180; i++)
                    {
                        var delta = target - player.transform.position; delta.y = 0;
                        if (delta.magnitude < .05f) break;
                        cc.Move(Vector3.ClampMagnitude(delta, .075f) + Vector3.down * .025f);
                    }
                    var end = player.transform.position;
                    report.floatCrossings.Add(new Traversal { direction = direction, start = start, end = end, target = target, status = Vector3.Distance(end, target) < .15f ? "pass" : "inspect" });
                }
                Directory.CreateDirectory("TestResults/MapIntegrity");
                File.WriteAllText("TestResults/MapIntegrity/access.json", JsonUtility.ToJson(report, true));
                int inspect = report.approaches.Count(a => a.status != "pass") + report.floatStates.Count(a => a.status != "pass") + report.floatCrossings.Count(a => a.status != "pass");
                Debug.Log("[Waterline] MAP ACCESS COMPLETE: targets=" + report.approaches.Count + " floatStates=" + report.floatStates.Count + " floatCrossings=" + report.floatCrossings.Count + " inspect=" + inspect + ". Candidate sampling only; inspect failures before changing geometry.");
                Finish(0);
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }
        private static void RecordFloat(HarborLayout h, Report report, string state, bool expectedRaised, float expectedY)
        {
            h.LowerDeck.Apply(); h.Planner.Apply(); h.Apply(); FreezeGuards(); Physics.SyncTransforms();
            var links = h.doors.Where(d => d.rule == HarborGateRule.FloatLink).ToArray();
            int active = links.Count(d => d.blocker.activeInHierarchy);
            bool pass = links.Length == 2 && h.LowerDeck.Raised == expectedRaised && active == (expectedRaised ? 0 : 2) && Mathf.Abs(h.LowerDeck.platform.position.y - expectedY) < .001f;
            report.floatStates.Add(new FloatCheck { state = state, status = pass ? "pass" : "inspect", raised = h.LowerDeck.Raised, activeBarriers = active, platformY = h.LowerDeck.platform.position.y });
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Check; EditorApplication.Exit(code);
        }
    }
}
