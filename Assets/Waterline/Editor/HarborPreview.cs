using System.IO;
using UnityEngine;
namespace Waterline.Editor
{
    public static class HarborPreview
    {
        public static void Capture()
        {
            HarborBuilder.Open();var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            Directory.CreateDirectory("TestResults/Harbor");
            Shot(camera,"dock",new Vector3(-10,1.7f,-7),new Vector3(4,-1,-3));
            Shot(camera,"hall",new Vector3(-11,1.7f,9),new Vector3(0,2.3f,17));
            Shot(camera,"west-service",new Vector3(-17,1.7f,15),new Vector3(-17,1.5f,30));
            Shot(camera,"power",new Vector3(-10,1.7f,31),new Vector3(-10,1.3f,24));
            Shot(camera,"archive",new Vector3(0,1.7f,40),new Vector3(0,1.3f,46));
            Shot(camera,"east-service",new Vector3(17,1.7f,20),new Vector3(17,1.5f,48));
            Shot(camera,"tide",new Vector3(14,1.7f,61),new Vector3(23,1.5f,67));
            Shot(camera,"signal",new Vector3(25,1.7f,10),new Vector3(25,1.5f,15));
        }
        private static void Shot(Camera camera,string name,Vector3 position,Vector3 look)
        {
            camera.transform.position=position;camera.transform.LookAt(look);camera.fieldOfView=72;
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var result=new Texture2D(1280,720,TextureFormat.RGB24,false);result.ReadPixels(new Rect(0,0,1280,720),0,0);result.Apply();
            File.WriteAllBytes("TestResults/Harbor/"+name+".png",result.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;
            Object.DestroyImmediate(result);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void GenerateAndCapture(){HarborBuilder.Generate();Capture();}
        public static void FinalBuild(){GenerateAndCapture();HarborBuilder.BuildWindows();}
    }
}
