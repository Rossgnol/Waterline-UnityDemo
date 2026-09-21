using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Waterline.Editor
{
    // Local procedural surfaces and industrial details; no downloaded assets or packages.
    public static class HarborArt
    {
        private const string Folder="Assets/Waterline/Generated/Harbor";
        private static readonly Dictionary<string,Material> cache=new Dictionary<string,Material>();
        public static void Apply(HarborLayout layout)
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();cache.Clear();
            var concrete=Texture("Concrete grain",false);var tile=Texture("Floor joints",true);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(renderer.GetComponent<TextMesh>()!=null || renderer.sharedMaterial==null)continue;
                string name=renderer.name.ToLowerInvariant();bool wall=name.Contains("wall") || name.Contains("seawall");
                bool floor=name.Contains("floor") || name=="south perimeter" || name.Contains("ceiling");
                if(!wall && !floor)continue;
                var old=renderer.sharedMaterial;Color tint=old.HasProperty("_BaseColor")?old.GetColor("_BaseColor"):Color.gray;
                Vector3 s=renderer.transform.lossyScale;
                float u=wall?Mathf.Max(s.x,s.z)/3:s.x/3,v=wall?s.y/3:s.z/3;
                string key=(wall?"Wall":"Floor")+"_"+Mathf.RoundToInt(u*10)+"_"+Mathf.RoundToInt(v*10)+"_"+ColorUtility.ToHtmlStringRGB(tint);
                if(!cache.TryGetValue(key,out var mat))
                {
                    string path=Folder+"/"+key+".mat";mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(mat==null){mat=new Material(old);AssetDatabase.CreateAsset(mat,path);}
                    mat.SetTexture("_BaseMap",wall?concrete:tile);mat.SetTextureScale("_BaseMap",new Vector2(Mathf.Max(.2f,u),Mathf.Max(.2f,v)));
                    mat.SetFloat("_Smoothness",.15f);cache[key]=mat;
                }
                renderer.sharedMaterial=mat;
            }
            var root=layout.transform;
            var blue=Accent("Blue route",new Color(.06f,.55f,.8f));var gold=Accent("Amber route",new Color(.92f,.57f,.14f));
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name.Contains("route stripe"))renderer.sharedMaterial=renderer.name.Contains("west")?blue:gold;
            foreach(var room in layout.rooms.Where(r=>r.id=="hub" || r.id=="tide"))
            {
                float top=room.id=="hub"?5.4f:4.6f;var b=room.bounds;
                foreach(float x in new[]{b.xMin,b.xMax})Detail(root,"Upper clerestory wall",new Vector3(x,(top+3.6f)/2,b.center.y),new Vector3(.25f,top-3.6f,b.height),AnnexBuilder.wall);
                foreach(float z in new[]{b.yMin,b.yMax})Detail(root,"Upper clerestory wall",new Vector3(b.center.x,(top+3.6f)/2,z),new Vector3(b.width,top-3.6f,.25f),AnnexBuilder.wall);
            }
            foreach(var lamp in root.GetComponentsInChildren<Light>())
            {
                if(lamp.transform.position.x< -14)lamp.color=new Color(.6f,.79f,1);
                if(lamp.transform.position.x>14)lamp.color=new Color(1,.79f,.53f);
            }
            Arrow(root,new Vector3(-11.2f,.04f,-10),Vector3.left,blue);
            Arrow(root,new Vector3(14,.04f,-10),Vector3.left,gold);
            Arrow(root,new Vector3(-11,.04f,14),Vector3.left,blue);
            Arrow(root,new Vector3(-10,.04f,19),Vector3.forward,gold);
            Arrow(root,new Vector3(-17,.04f,24),Vector3.left,blue);
            Arrow(root,new Vector3(17,.04f,32),Vector3.forward,gold);
            Arrow(root,new Vector3(-4,.04f,53),Vector3.forward,blue);
            Arrow(root,new Vector3(3,.04f,62),Vector3.right,gold);
            Arrow(root,new Vector3(17,.04f,12),Vector3.right,blue);
            var pump=GameObject.Find("Suspended pump landmark");if(pump!=null)pump.GetComponent<Renderer>().enabled=false;
            AnnexBuilder.Cylinder("Hoisted pump motor",new Vector3(0,2.1f,15),1.05f,3.2f,AnnexBuilder.blue,Quaternion.Euler(0,0,90));
            AnnexBuilder.Cylinder("Hoisted pump coupling",new Vector3(1.5f,2.1f,15),.68f,.8f,AnnexBuilder.brass,Quaternion.Euler(0,0,90));
            foreach(float x in new[]{-1f,1f})Detail(root,"Hoist suspension cable",new Vector3(x,3.95f,15),new Vector3(.065f,1.7f,.065f),AnnexBuilder.dark);
            for(int i=0;i<7;i++)AnnexBuilder.Cylinder("Motor cooling ring",new Vector3(-1.25f+i*.35f,2.1f,15),1.09f,.045f,AnnexBuilder.dark,Quaternion.Euler(0,0,90));
            // Repeated low wall trim clarifies floor boundaries in the larger rooms.
            foreach(var room in layout.rooms.Where(r=>r.sector>0 && !r.corridor))
            {
                var b=room.bounds;Detail(root,"Room wall trim",new Vector3(b.xMin+.15f,.2f,b.center.y),new Vector3(.12f,.32f,b.height-.5f),AnnexBuilder.dark);
            }
            AssetDatabase.SaveAssets();
        }
        private static void Detail(Transform root,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static void Arrow(Transform root,Vector3 p,Vector3 direction,Material material)
        {
            var go=new GameObject("Painted route arrow");go.transform.SetParent(root);go.transform.position=p;go.transform.rotation=Quaternion.LookRotation(direction);
            Part(new Vector3(0,0,-.15f),new Vector3(.12f,.015f,.8f),0);Part(new Vector3(-.17f,0,.22f),new Vector3(.1f,.015f,.55f),40);Part(new Vector3(.17f,0,.22f),new Vector3(.1f,.015f,.55f),-40);
            void Part(Vector3 local,Vector3 size,float angle)
            {var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name="Arrow paint";box.transform.SetParent(go.transform,false);box.transform.localPosition=local;box.transform.localScale=size;box.transform.localRotation=Quaternion.Euler(0,angle,0);box.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());}
        }
        private static Material Accent(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.45f);return mat;
        }
        private static Texture2D Texture(string name,bool tiled)
        {
            string path=Folder+"/"+name+".png";
            if(!File.Exists(path))
            {
                const int size=256;var image=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float grain=((x*73856093u ^ y*19349663u)%1000)/1000f;
                    float large=Mathf.PerlinNoise(x/27f,y/27f),v=.78f+grain*.12f+large*.1f;
                    if(tiled && (x%128<2 || y%128<2))v=.36f;
                    if(tiled && (x%128==3 || y%128==3))v=.92f;
                    pixels[y*size+x]=new Color(v,v,v);
                }
                image.SetPixels(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
