using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;

namespace Waterline.Editor
{
    // A batch-only integration check: real Play Mode, colliders and presentation.
    [InitializeOnLoad]
    public static class PrototypeSmokeTest
    {
        private const string ActiveKey = "Waterline.SmokeTest.Active";
        private static IEnumerator routine;
        private static double deadline;

        static PrototypeSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this check from a separate batch editor.");
            PrototypeBuilder.Open();
            PrototypeValidation.Validate();
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
            deadline = EditorApplication.timeSinceStartup + 45;
            routine = CheckRuntime();
            EditorApplication.update += Step;
        }

        private static void Step()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Runtime check timed out.");
                if (routine.MoveNext()) return;
                Debug.Log("[Waterline] RUNTIME PASS: interlocks, physical gate, bridge crossing, pause, water and boat, escape.");
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Step;
            EditorApplication.Exit(code);
        }

        private static IEnumerator CheckRuntime()
        {
            yield return null;
            var session = UnityEngine.Object.FindFirstObjectByType<DockSession>();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            var items = UnityEngine.Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None)
                .ToDictionary(item => item.action);
            Require(!items[DockAction.StartFlood].Use(), "Flood started before prerequisites.");
            Place(controller, new Vector3(2, 0.08f, -6));
            controller.Move(Vector3.right * 6);
            Require(player.transform.position.x < 3.8f, "Closed service gate does not block passage.");
            Place(controller, new Vector3(-6, 0.08f, 3.5f));
            controller.Move(Vector3.right * 12);
            Require(player.transform.position.x < -4.8f, "Retracted bridge has no safety barrier.");

            Require(items[DockAction.TakePin].Use(), "Cannot collect repair pin.");
            Require(!items[DockAction.TakePin].gameObject.activeSelf, "Collected pin is still visible.");
            Require(items[DockAction.InstallPin].Use(), "Cannot repair pump.");
            double until = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Place(controller, new Vector3(2, 0.08f, -6));
            controller.Move(Vector3.right * 6);
            Require(player.transform.position.x > 7, "Repaired service gate still blocks passage.");
            Require(items[DockAction.UnlockGate].Use(), "Cannot release dock lock.");
            Require(items[DockAction.DeployBridge].Use(), "Cannot deploy bridge.");
            until = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Physics.SyncTransforms();
            Require(session.bridge.GetComponent<Collider>().enabled, "Deployed bridge has no collision.");
            Place(controller, new Vector3(-6, 0.08f, 3.5f));
            controller.Move(Vector3.right * 12);
            Require(player.transform.position.x > 5.5f, "Deployed bridge cannot be crossed.");
            Require(Physics.Raycast(new Vector3(0, 1, 3.5f), Vector3.down, out var floorHit, 2) &&
                floorHit.collider == session.bridge.GetComponent<Collider>(), "Bridge deck does not support a player.");

            Require(items[DockAction.StartFlood].Use(), "Valid flood request failed.");
            Require(!items[DockAction.BoardBoat].Use(), "Escape allowed before water rises.");
            session.Paused = true;
            float progress = session.State.WaterProgress;
            until = EditorApplication.timeSinceStartup + 0.5;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Require(Mathf.Approximately(progress, session.State.WaterProgress), "Pause does not stop water.");
            session.Paused = false;
            while (session.State.Phase != DockPhase.Flooded) yield return null;
            Require(Mathf.Abs(session.water.position.y - session.fullWaterHeight) < 0.01f, "Water presentation is out of sync.");
            Require(Mathf.Abs(session.boat.position.y - session.fullBoatHeight) < 0.01f, "Boat presentation is out of sync.");
            Require(items[DockAction.BoardBoat].Use() && session.InputBlocked, "Escape did not finish the session.");
        }

        private static void Place(CharacterController controller, Vector3 position)
        {
            controller.enabled = false;
            controller.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException("[Waterline runtime] " + message);
        }
    }
}
