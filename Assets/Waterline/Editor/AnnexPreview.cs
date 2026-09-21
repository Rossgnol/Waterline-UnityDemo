using System.IO;
using UnityEngine;
namespace Waterline.Editor
{
    public static class AnnexPreview
    {
        public static void Capture()
        {
            AnnexBuilder.Open();var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            Directory.CreateDirectory("TestResults/Annex");
            Render(camera,"entrance",new Vector3(-8,1.65f,4),new Vector3(-8,1.4f,9));
            Render(camera,"spares",new Vector3(-8,1.65f,12),new Vector3(-9.5f,1.1f,16.7f));
            Render(camera,"power",new Vector3(-2,1.65f,10),new Vector3(-2,1,6.8f));
            Render(camera,"archive",new Vector3(-2,1.65f,13),new Vector3(-2,1.1f,16.5f));
            Render(camera,"signal",new Vector3(8,1.65f,7.5f),new Vector3(8,1.4f,10.4f));
            Render(camera,"gallery",new Vector3(-9,1.65f,19.5f),new Vector3(8,1.3f,19.5f));
            Debug.Log("[Waterline] Annex six-view previews captured.");
        }
        private static void Render(Camera camera,string name,Vector3 p,Vector3 target)
        {
            camera.transform.position=p;camera.transform.LookAt(target);camera.fieldOfView=72;
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("TestResults/Annex/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;
            Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void CaptureConnected()
        {
            ConnectedBuilder.Open();var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            Directory.CreateDirectory("TestResults/Annex");
            Render(camera,"connected-workshop",new Vector3(-12.5f,1.65f,10),new Vector3(-18,1.3f,12));
            Render(camera,"connected-packing",new Vector3(-16,1.65f,19),new Vector3(-15,1.3f,27));
            Render(camera,"connected-dispatch",new Vector3(-8,1.65f,31),new Vector3(-2,1.3f,35));
            Render(camera,"connected-tide",new Vector3(8,1.65f,32),new Vector3(8,1.3f,37));
            Render(camera,"connected-signal",new Vector3(8,1.65f,8),new Vector3(8,1.4f,13));
            Render(camera,"connected-gallery",new Vector3(-9,1.65f,27.5f),new Vector3(8,1.3f,27.5f));
        }
    }
}
