using System.IO;
using UnityEditor;
using UnityEngine;

namespace Waterline.Editor
{
    public static class ThreatPreview
    {
        public static void Capture()
        {
            ThreatBuilder.Open();
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            var enemy=Object.FindFirstObjectByType<ThreatEncounter>();
            Directory.CreateDirectory("TestResults/Threat");
            Render(camera,"lower-encounter",new Vector3(-2.6f,-1.85f,3.7f),new Vector3(3,-2.2f,3.7f));
            enemy.transform.rotation=Quaternion.Euler(0,-90,0);
            Render(camera,"worker",new Vector3(.6f,-1.85f,3.7f),new Vector3(3,-2.3f,3.7f));
            Render(camera,"quiet-route",new Vector3(-7,1.65f,-6.6f),new Vector3(4,.9f,-6));
            Render(camera,"overview",new Vector3(-9,7,-13),new Vector3(0,-1,0));
            Debug.Log("[Waterline] Threat previews captured.");
        }
        private static void Render(Camera camera,string name,Vector3 position,Vector3 target)
        {
            camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=72;
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            File.WriteAllBytes("TestResults/Threat/"+name+".png",pixels.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
