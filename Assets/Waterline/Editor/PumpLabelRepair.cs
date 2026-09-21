using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // Repair the saved scene only; leave historic generators and runtime rules intact.
    public static class PumpLabelRepair
    {
        public static void Apply()
        {
            TidalRouteBuilder.Open();
            var label = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Single(t => t.text == "PUMP PRESSURE HALL");
            var h = Object.FindFirstObjectByType<HarborLayout>();
            var door = h.doors.Single(d => d.from == "pump_hall" && d.to == "archive");
            var originalDoor = EditorJsonUtility.ToJson(h);
            var colliderSignature = Colliders();
            // The title described the room currently occupied, but overprinted its exit sign.
            // Put it on the adjacent north wall; retain the archive/key destination on the lintel.
            label.transform.position = new Vector3(-3.6f, 2.85f, 37.7f);
            var bounds = label.GetComponent<Renderer>().bounds;
            if (bounds.max.x > -1.6f || bounds.min.x < -5.8f)
                throw new InvalidOperationException("Room title does not fit beside the archive portal.");
            if (EditorJsonUtility.ToJson(h) != originalDoor || Colliders() != colliderSignature)
                throw new InvalidOperationException("Door references or collision changed unexpectedly.");
            if (door.sign.text != "06 ARCHIVE / KEY") throw new InvalidOperationException("Archive destination changed.");
            EditorSceneManager.MarkSceneDirty(label.gameObject.scene);
            EditorSceneManager.SaveScene(label.gameObject.scene); AssetDatabase.SaveAssets();
            StairJunctionAudit.CaptureSigns("signs-after");
            var camera = Object.FindFirstObjectByType<FirstPersonController>().view;
            BoatPresentationBuilder.Shot(camera,"TestResults/StairJunction/signs-after/wide.png",new Vector3(0,1.65f,30),new Vector3(-1.8f,2.8f,38));
            Debug.Log("[Waterline] PUMP LABEL REPAIR SAVED: room title moved to adjacent wall; archive destination preserved; original collision and HarborLayout signature unchanged.");
        }
        private static string Colliders() => string.Join("\n",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5")));
    }
}
