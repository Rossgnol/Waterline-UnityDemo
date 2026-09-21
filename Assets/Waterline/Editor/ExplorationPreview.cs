using System.IO;
using UnityEditor;
using UnityEngine;

namespace Waterline.Editor
{
    public static class ExplorationPreview
    {
        // Actual Unity scene renders for geometry and lighting review; no scene edits are saved.
        public static void Capture()
        {
            ExplorationBuilder.Open();
            var camera = Object.FindFirstObjectByType<FirstPersonController>().view;
            Directory.CreateDirectory("TestResults/Exploration");
            Render(camera, "entry", new Vector3(-8, 1.7f, -4), new Vector3(-12.65f, 1.5f, -4));
            Render(camera, "upper", new Vector3(-9, 3.8f, -9), new Vector3(0, -1.2f, 1));
            Render(camera, "west-stairs", new Vector3(-12.65f, 1.5f, -4.4f), new Vector3(-12.65f, -2.2f, 2));
            Render(camera, "workshop", new Vector3(-8, -1.85f, 3.4f), new Vector3(-8, -2.35f, 0));
            Render(camera, "lower-dock", new Vector3(-3.8f, -1.85f, 2.4f), new Vector3(1, -2.4f, 0));
            Render(camera, "east-stairs", new Vector3(12.65f, -1.85f, 4.7f), new Vector3(12.65f, -0.5f, -2));
            var session = Object.FindFirstObjectByType<DockSession>();
            session.water.position = new Vector3(0, session.fullWaterHeight, 0.5f);
            session.boat.position = new Vector3(0, session.fullBoatHeight, 0);
            Render(camera, "flooded", new Vector3(-9, 3.8f, -9), new Vector3(0, -1.2f, 1));
            Debug.Log("[Waterline] Exploration scene previews captured.");
        }

        private static void Render(Camera camera, string name, Vector3 position, Vector3 target)
        {
            camera.transform.position = position; camera.transform.LookAt(target); camera.fieldOfView = 72;
            var rt = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            File.WriteAllBytes("TestResults/Exploration/" + name + ".png", pixels.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(pixels); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
