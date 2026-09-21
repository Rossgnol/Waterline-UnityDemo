using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    [InitializeOnLoad]
    public static class ExplorationSmokeTest
    {
        private const string Key = "Waterline.ExplorationCheck.Active";
        private static IEnumerator routine;
        private static double deadline;
        private static CharacterController controller;
        static ExplorationSmokeTest() { EditorApplication.playModeStateChanged += OnPlay; }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch editor for this check.");
            ExplorationBuilder.Open(); ExplorationBuilder.Validate();
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }

        private static void OnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 240;
            routine = Check(); EditorApplication.update += Step;
        }

        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Timed out.");
                if (routine.MoveNext()) return;
                Debug.Log("[Waterline] EXPLORATION PASS: lower loop, both stairs, reachable interactions, map pause, flood safety, two exit routes, escape.");
                Finish(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        private static void Finish(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Step; EditorApplication.Exit(code); }

        private static IEnumerator Check()
        {
            yield return null;
            var session = UnityEngine.Object.FindFirstObjectByType<DockSession>();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            var routes = UnityEngine.Object.FindFirstObjectByType<DockRoutes>();
            player.enabled = false; controller = player.GetComponent<CharacterController>();
            var items = UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).ToDictionary(i => i.action);
            Require(session.water.position.y < -3.5f, "Dry lower floor is underwater.");
            Place(new Vector3(2, 0.08f, -6)); controller.Move(Vector3.right * 6);
            Require(controller.transform.position.x < 3.8f, "South gate can be bypassed before east unlock.");
            Place(new Vector3(-8, 0.08f, -3.3f));
            foreach (var point in new[] { V(-8,-4), V(-12.65f,-4), V(-12.65f,4.8f), V(-8,4.8f), V(-8,1.35f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            Require(controller.transform.position.y < -3.3f, "West stairs did not reach lower deck.");
            UseVisible(player.view, items[DockAction.TakePin]);
            Require(!items[DockAction.TakePin].gameObject.activeSelf, "Collected pin remains visible.");
            foreach (var point in new[] { V(-3,1.35f), V(-3,3.7f), V(3,3.7f), V(8,3.7f), V(8,4.8f), V(12.65f,4.8f), V(12.65f,-4), V(8.5f,-4), V(8.5f,-0.2f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            Require(controller.transform.position.y > -0.2f, "East stairs did not reach upper deck.");
            UseVisible(player.view, items[DockAction.UnlockGate]);
            foreach (var point in new[] { V(10,-0.2f), V(10,2.4f), V(8.4f,2.4f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            UseVisible(player.view, items[DockAction.DeployBridge]);
            var wait = Wait(1.5); while (wait.MoveNext()) yield return null;
            foreach (var point in new[] { V(8.4f,3.5f), V(-8,3.5f), V(-8,0.8f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            UseVisible(player.view, items[DockAction.InstallPin]);
            Place(new Vector3(-8, -3.42f, 2.3f));
            Require(!session.Perform(DockAction.StartFlood) && session.State.Phase == DockPhase.Dry, "Flood can strand a player below deck.");
            Place(new Vector3(-8, 0.08f, 2.3f));
            UseVisible(player.view, items[DockAction.StartFlood]);
            yield return null;
            Require(!routes.LowerOpen && routes.westFloodGate.activeSelf && routes.eastFloodGate.activeSelf, "Stair flood gates did not close.");
            session.MapVisible = true;
            float progress = session.State.WaterProgress;
            wait = Wait(0.4); while (wait.MoveNext()) yield return null;
            Require(Mathf.Approximately(progress, session.State.WaterProgress), "Map does not pause flooding.");
            session.MapVisible = false;
            while (session.State.Phase != DockPhase.Flooded) yield return null;
            foreach (float x in new[] { -12.65f, 12.65f })
            {
                Place(new Vector3(x, 0.08f, -4)); controller.Move(Vector3.forward * 3);
                Require(controller.transform.position.z < -3.3f, "Flood gate permits descent: " + x);
            }
            Place(new Vector3(-8, 0.08f, 2.3f));
            foreach (var point in new[] { V(-8,3.5f), V(10,3.5f), V(10,-3.2f), V(5.8f,-2.4f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            RequireVisible(player.view, items[DockAction.BoardBoat]);
            Place(new Vector3(-8, 0.08f, 2.3f));
            foreach (var point in new[] { V(-8,-6), V(8,-6), V(8,-3.2f), V(5.8f,-2.4f) })
            { var move = Walk(point); while (move.MoveNext()) yield return null; }
            UseVisible(player.view, items[DockAction.BoardBoat]);
            Require(session.State.Phase == DockPhase.Escaped && routes.LowerVisited && routes.BridgeUsedAfterFlood && routes.SouthUsedAfterFlood,
                "One or more required route segments were not traversed.");
        }

        private static Vector2 V(float x, float z) { return new Vector2(x,z); }
        private static IEnumerator Walk(Vector2 target)
        {
            int steps = 0;
            while (Vector2.Distance(new Vector2(controller.transform.position.x, controller.transform.position.z), target) > 0.09f)
            {
                Require(++steps < 450, "Walk blocked toward " + target + " at " + controller.transform.position);
                var delta = Vector2.ClampMagnitude(target - new Vector2(controller.transform.position.x, controller.transform.position.z), 0.12f);
                controller.Move(new Vector3(delta.x, -0.09f, delta.y));
                Require(controller.transform.position.y > -4, "Fell through lower deck.");
                yield return null;
            }
            Debug.Log("[Waterline] Route waypoint reached " + target + " at height " + controller.transform.position.y);
        }

        private static void RequireVisible(Camera view, DockInteractable item)
        {
            Physics.SyncTransforms();
            Vector3 delta = item.transform.position - view.transform.position;
            Require(delta.magnitude <= item.session.tuning.interactionDistance, "Interaction out of reach: " + item.name);
            Require(view.GetComponentInParent<FirstPersonController>().RaycastInteraction(delta.normalized, out var hit) &&
                hit.collider.GetComponentInParent<DockInteractable>() == item, "Interaction is occluded: " + item.name +
                " from " + view.transform.position + " hit " + (hit.collider==null?"nothing":hit.collider.name) + " at " + hit.point);
        }

        private static void UseVisible(Camera view, DockInteractable item)
        { RequireVisible(view,item); Require(item.Use(), "Interaction failed: " + item.name); }

        private static IEnumerator Wait(double seconds)
        { double until = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < until) yield return null; }

        private static void Place(Vector3 position)
        { controller.enabled = false; controller.transform.position = position; controller.enabled = true; Physics.SyncTransforms(); }

        private static void Require(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("[Exploration check] " + message); }
    }
}
