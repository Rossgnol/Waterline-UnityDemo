using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    // Only replaces render geometry and animation pivots in the current Day11 scene.
    public static class WorkerPresentationBuilder
    {
        private const string Folder="Assets/Waterline/Generated/Workers";
        private const string RootName="Rainwear presentation";
        private static Material rubber,metal,tape,lens;
        private static Mesh coat,hood,sleeve,leg,boot,glove,pack,oval,link;

        // Use a fresh editor process for delivery captures after asset regeneration: it avoids stale imported GPU buffers.
        public static void CaptureSaved()
        {
            TidalRouteBuilder.Open();var enemies=Object.FindObjectsByType<ThreatEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(e=>e.displayName).ToArray();
            Validate(enemies);Capture(enemies,"after");Debug.Log("[Waterline] WORKER SAVED CAPTURE PASS: persisted scene and meshes, no scene save.");
        }

        [MenuItem("Waterline/29 Refine patrol worker models")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();var signature=Signature();
            var enemies=Object.FindObjectsByType<ThreatEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(e=>e.displayName).ToArray();
            if(enemies.Length!=4)throw new InvalidOperationException("Expected four current harbor workers");
            if(enemies.All(e=>e.body.Find(RootName)==null))Capture(enemies,"before");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();LoadParts();
            for(int i=0;i<enemies.Length;i++)Dress(enemies[i],i);
            var updated=Signature();
            if(signature!=updated)
            {
                File.WriteAllText("TestResults/Workers/signature-before.txt",signature);File.WriteAllText("TestResults/Workers/signature-after.txt",updated);
                throw new InvalidOperationException("Worker art changed physics, interaction or AI configuration; inspect saved signatures");
            }
            Validate(enemies);TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            TidalRouteBuilder.Open();
            Capture(Object.FindObjectsByType<ThreatEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(e=>e.displayName).ToArray(),"after");
            Debug.Log("[Waterline] WORKER PRESENTATION PASS: four rainwear models; shoulder/hip pivots, opposite arm swing; collider, interaction and AI configuration unchanged.");
        }

        private static void LoadParts()
        {
            rubber=Mat("Rubber and fabric",new Color(.035f,.045f,.047f),0,.2f);
            metal=Mat("Weathered fittings",new Color(.28f,.32f,.31f),.65f,.32f);
            tape=Mat("Worn reflective tape",new Color(.64f,.68f,.57f),.15f,.28f);
            lens=Mat("Smoked goggles",new Color(.025f,.075f,.085f),.5f,.65f);
            coat=Rings("Raincoat",new[]{R(.58f,.345f,.24f),R(.69f,.35f,.245f),R(.95f,.285f,.215f),R(1.22f,.295f,.22f),R(1.4f,.31f,.23f),R(1.51f,.19f,.16f),R(1.53f,.12f,.115f)});
            hood=Rings("Hood",new[]{R(1.47f,.115f,.12f),R(1.55f,.21f,.19f),R(1.73f,.23f,.21f),R(1.85f,.155f,.15f),R(1.91f,.045f,.055f)});
            sleeve=Rings("Sleeve",new[]{R(-.60f,.073f,.085f),R(-.47f,.09f,.1f),R(-.30f,.10f,.12f),R(-.12f,.12f,.13f),R(.015f,.11f,.12f),R(.055f,.06f,.07f)});
            leg=Rings("Trouser",new[]{R(-.51f,.075f,.087f),R(-.28f,.092f,.11f),R(0,.117f,.125f),R(.075f,.10f,.11f)});
            boot=Rings("Boot",new[]{R(-.65f,.093f,.145f),R(-.58f,.10f,.16f),R(-.5f,.088f,.12f),R(-.34f,.079f,.09f)});
            glove=Rings("Glove",new[]{R(-.77f,.035f,.055f),R(-.74f,.073f,.087f),R(-.65f,.078f,.086f),R(-.58f,.065f,.074f)});
            pack=Rings("Tool satchel",new[]{R(.89f,.13f,.075f),R(.96f,.2f,.095f),R(1.32f,.20f,.095f),R(1.4f,.13f,.07f)});
            oval=Rings("Rounded fitting",new[]{R(-.5f,.22f,.22f),R(-.35f,.45f,.45f),R(.35f,.45f,.45f),R(.5f,.22f,.22f)},16);
            link=ChainLink();
        }

        private static void Dress(ThreatEncounter enemy,int index)
        {
            var body=enemy.body;var old=body.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            // These original transforms remain for safe reapplication and material/role lookup.
            var color=body.Find("Raincoat").GetComponent<Renderer>().sharedMaterial;
            foreach(var r in body.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var root=new GameObject(RootName).transform;root.SetParent(body,false);
            var torso=new GameObject("Coat and equipment").transform;torso.SetParent(root,false);
            MeshPart(torso,"Flared raincoat",coat,color,Vector3.zero,Vector3.one);
            MeshPart(torso,"Rounded hood",hood,color,new Vector3(0,0,-.025f),Vector3.one);
            MeshPart(torso,"Recessed face",oval,rubber,new Vector3(0,1.672f,.181f),new Vector3(.355f,.31f,.18f));
            MeshPart(torso,"Goggle seal",oval,metal,new Vector3(0,1.73f,.246f),new Vector3(.31f,.11f,.065f));
            MeshPart(torso,"Smoked goggle lens",oval,lens,new Vector3(0,1.731f,.271f),new Vector3(.265f,.073f,.03f));
            MeshPart(torso,"Respirator",oval,rubber,new Vector3(0,1.604f,.253f),new Vector3(.16f,.115f,.12f));
            for(int j=-1;j<=1;j++)BoxPart(torso,"Mask filter grille",new Vector3(j*.032f,1.604f,.308f),new Vector3(.012f,.055f,.012f),metal);
            // Flat pieces follow the curved chest rather than clipping into it.
            BoxPart(torso,"Zip storm flap",new Vector3(0,1.05f,.228f),new Vector3(.033f,.69f,.016f),rubber);
            var lamp=BoxPart(torso,"Chest lamp housing",new Vector3(.145f,1.40f,.205f),new Vector3(.075f,.092f,.035f),metal);lamp.localRotation=Quaternion.Euler(0,34,0);
            lamp=BoxPart(torso,"Chest lamp lens",new Vector3(.158f,1.40f,.222f),new Vector3(.052f,.059f,.017f),body.Find("Work lamp").GetComponent<Renderer>().sharedMaterial);lamp.localRotation=Quaternion.Euler(0,34,0);
            foreach(float side in new[]{-1f,1f})
            {
                var p=BoxPart(torso,"Chest reflector",new Vector3(side*.18f,1.28f,.184f),new Vector3(.13f,.045f,.016f),tape);p.localRotation=Quaternion.Euler(0,side*38,0);
                p=BoxPart(torso,"Pocket flap",new Vector3(side*.18f,.99f,.167f),new Vector3(.17f,.1f,.027f),color);p.localRotation=Quaternion.Euler(0,side*36,0);
                p=BoxPart(torso,"Satchel strap",new Vector3(side*.145f,1.22f,-.204f),new Vector3(.034f,.46f,.025f),rubber);p.localRotation=Quaternion.Euler(0,side*-25,0);
            }
            MeshPart(torso,"Canvas tool satchel",pack,rubber,new Vector3(0,0,-.245f),Vector3.one);
            BoxPart(torso,"Rear safety strip",new Vector3(0,1.19f,-.344f),new Vector3(.28f,.055f,.018f),tape);
            BoxPart(torso,"Satchel buckle",new Vector3(0,1.02f,-.346f),new Vector3(.06f,.07f,.019f),metal);
            // The belt chain has interlocking links and an actual attachment instead of floating cubes.
            for(int j=0;j<11;j++)
            {
                Vector3 p=j<6?new Vector3(.30f+j*.012f,.67f-j*.108f,-.15f):new Vector3(.36f,.046f,-.20f-(j-6)*.105f);
                var t=MeshPart(torso,"Chain link",link,metal,p,Vector3.one);
                t.localRotation=j<6?Quaternion.Euler(0,j%2*90,0):j%2==0?Quaternion.Euler(90,0,0):Quaternion.Euler(0,90,90);
            }
            var pivots=new Transform[4];
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                var hip=new GameObject(side==0?"Left hip":"Right hip").transform;hip.SetParent(root,false);hip.localPosition=new Vector3(sign*.16f,.66f,0);
                MeshPart(hip,"Waterproof trouser",leg,rubber,Vector3.zero,Vector3.one);
                MeshPart(hip,"Rubber work boot",boot,rubber,new Vector3(0,0,.025f),Vector3.one);
                pivots[side]=hip;
                var shoulder=new GameObject(side==0?"Left shoulder":"Right shoulder").transform;shoulder.SetParent(root,false);shoulder.localPosition=new Vector3(sign*.335f,1.40f,0);
                MeshPart(shoulder,"Rain sleeve",sleeve,color,Vector3.zero,Vector3.one);
                MeshPart(shoulder,"Work glove",glove,rubber,Vector3.zero,Vector3.one);
                BoxPart(shoulder,"Sleeve reflector",new Vector3(0,-.23f,.118f),new Vector3(.14f,.045f,.018f),tape);
                // Existing gait alternates even/odd entries. Opposite arm belongs to each leg.
                pivots[side==0?3:2]=shoulder;
                Combine(hip,"Worker "+index+" hip "+side);Combine(shoulder,"Worker "+index+" shoulder "+side);
            }
            enemy.limbs=pivots;Combine(torso,"Worker "+index+" torso");
        }

        private static void Validate(ThreatEncounter[] enemies)
        {
            foreach(var e in enemies)
            {
                var root=e.body.Find(RootName);
                if(root==null || root.GetComponentsInChildren<Collider>(true).Length!=0 || root.GetComponentsInChildren<Light>(true).Length!=0)throw new InvalidOperationException("Unexpected worker collision/light");
                if(e.limbs.Length!=4 || e.limbs.Any(t=>t==null || !t.IsChildOf(root)))throw new InvalidOperationException("Worker pivot references missing");
                if(e.limbs[0].name!="Left hip" || e.limbs[2].name!="Right shoulder")throw new InvalidOperationException("Contralateral swing binding wrong");
                int triangles=0;
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                {
                    var mesh=r.GetComponent<MeshFilter>().sharedMesh;triangles+=mesh.triangles.Length/3;
                    if(mesh.uv.Length!=mesh.vertexCount || r.sharedMaterial==null || r.sharedMaterial.shader==null || r.sharedMaterial.shader.name.Contains("InternalError"))throw new InvalidOperationException("Broken worker surface");
                }
                Debug.Log("[Waterline] Worker geometry "+e.displayName+": "+triangles+" triangles / "+root.GetComponentsInChildren<MeshRenderer>().Length+" renderers");
            }
        }

        private static string Signature()
        {
            var physics=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F6"));
            var ai=Object.FindObjectsByType<ThreatEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(e=>e.GetInstanceID()).Select(e=>e.GetInstanceID()+"/"+e.graph.GetInstanceID()+EditorJsonUtility.ToJson(e.graph)+EditorJsonUtility.ToJson(e.tuning)+e.areaLimited+e.areaMin+e.areaMax+e.managesDockFlood+e.Available);
            var interactions=Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(t=>t.GetInstanceID()).Select(t=>EditorJsonUtility.ToJson(t)+t.transform.localToWorldMatrix.ToString("F6")).Concat(Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(t=>t.GetInstanceID()).Select(t=>EditorJsonUtility.ToJson(t)+t.transform.localToWorldMatrix.ToString("F6")));
            return string.Join("\n",physics.Concat(ai).Concat(interactions));
        }

        private static Material Mat(string name,Color color,float metallic,float smooth)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        private static Vector3 R(float y,float rx,float rz){return new Vector3(rx,y,rz);}
        private static Mesh Rings(string name,Vector3[] rings,int segments=24)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int j=0;j<rings.Length;j++)for(int i=0;i<=segments;i++)
            {
                float a=i*2*Mathf.PI/segments;var r=rings[j];v.Add(new Vector3(Mathf.Sin(a)*r.x,r.y,Mathf.Cos(a)*r.z));uv.Add(new Vector2((float)i/segments,(r.y-rings[0].y)/(rings[rings.Length-1].y-rings[0].y)));
                if(j<rings.Length-1 && i<segments){int a0=j*(segments+1)+i,b=a0+segments+1;tris.AddRange(new[]{a0,b+1,b,a0,a0+1,b+1});}
            }
            // Separate cap vertices keep cap normals from rounding over the rim.
            for(int cap=0;cap<2;cap++)
            {
                var r=rings[cap==0?0:rings.Length-1];int center=v.Count;v.Add(new Vector3(0,r.y,0));uv.Add(new Vector2(.5f,.5f));
                for(int i=0;i<=segments;i++){float a=i*2*Mathf.PI/segments;v.Add(new Vector3(Mathf.Sin(a)*r.x,r.y,Mathf.Cos(a)*r.z));uv.Add(new Vector2(.5f+.5f*Mathf.Sin(a),.5f+.5f*Mathf.Cos(a)));}
                for(int i=0;i<segments;i++)tris.AddRange(cap==0?new[]{center,center+i+2,center+i+1}:new[]{center,center+i+1,center+i+2});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
            // Smooth the cylindrical UV seam without joining the independently shaded caps.
            var normals=mesh.normals;for(int j=0;j<rings.Length;j++){int a=j*(segments+1),b=a+segments;var n=(normals[a]+normals[b]).normalized;normals[a]=n;normals[b]=n;}mesh.normals=normals;
            mesh.RecalculateBounds();return SaveMesh(name,mesh);
        }
        private static Mesh ChainLink()
        {
            const int around=16,tube=6;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int i=0;i<=around;i++)for(int j=0;j<=tube;j++)
            {
                float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube;v.Add(new Vector3(Mathf.Cos(a)*(.035f+.009f*Mathf.Cos(b)),Mathf.Sin(a)*(.065f+.009f*Mathf.Cos(b)),.009f*Mathf.Sin(b)));uv.Add(new Vector2((float)i/around,(float)j/tube));
                if(i<around && j<tube){int x=i*(tube+1)+j,y=x+tube+1;tris.AddRange(new[]{x,y,x+1,x+1,y,y+1});}
            }
            var m=new Mesh{name="Chain link"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();return SaveMesh("Chain link",m);
        }
        private static Mesh SaveMesh(string name,Mesh mesh)
        {
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);saved.UploadMeshData(false);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }
        private static Transform MeshPart(Transform parent,string name,Mesh mesh,Material material,Vector3 p,Vector3 size)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        private static Transform BoxPart(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        private static void Combine(Transform parent,string name)
        {
            var renderers=parent.GetComponentsInChildren<MeshRenderer>();int index=0;
            foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
            {
                var combines=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{name=name};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
                MeshPart(parent,"Combined "+group.Key.name,SaveMesh(name+" "+index++,mesh),group.Key,Vector3.zero,Vector3.one);
            }
            foreach(var r in renderers)Object.DestroyImmediate(r.gameObject);
        }

        private static void Capture(ThreatEncounter[] enemies,string phase)
        {
            string folder="TestResults/Workers/"+phase;Directory.CreateDirectory(folder);
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;int mask=camera.cullingMask;
            var stage=new GameObject("Temporary worker inspection stage");stage.transform.position=new Vector3(1000,10,1000);
            try
            {
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(stage.transform,false);floor.transform.localPosition=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(8,.2f,8);floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/Machined steel.mat");floor.layer=30;
                camera.cullingMask=1<<30;camera.fieldOfView=38;
                for(int i=0;i<enemies.Length;i++)
                {
                    var clone=Object.Instantiate(enemies[i].body.gameObject,stage.transform);clone.SetActive(true);clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=Vector3.one;
                    foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                    Shot(i+"-front",new Vector3(1.9f,1.5f,3.3f),new Vector3(0,1,0));
                    if(i==0)
                    {
                        Shot("back",new Vector3(-1.9f,1.5f,-3.3f),new Vector3(0,1,0));
                        Shot("face",new Vector3(.8f,1.72f,1.7f),new Vector3(0,1.63f,0));
                        if(phase=="after")
                        {
                            var root=clone.transform.Find(RootName);root.Find("Left hip").localRotation=Quaternion.Euler(18,0,0);root.Find("Right hip").localRotation=Quaternion.Euler(-18,0,0);root.Find("Left shoulder").localRotation=Quaternion.Euler(-18,0,0);root.Find("Right shoulder").localRotation=Quaternion.Euler(18,0,0);
                            Shot("stride",new Vector3(2.8f,1.4f,2.7f),new Vector3(0,.9f,0));
                        }
                    }
                    Object.DestroyImmediate(clone);
                }
            }
            finally{Object.DestroyImmediate(stage);camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;camera.cullingMask=mask;}
            void Shot(string name,Vector3 p,Vector3 target)
            {
                camera.transform.position=stage.transform.position+p;camera.transform.LookAt(stage.transform.position+target);
                var texture=new RenderTexture(1000,1000,24);var old=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
                try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;image=new Texture2D(1000,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());}
                finally{camera.targetTexture=old;RenderTexture.active=active;if(image!=null)Object.DestroyImmediate(image);texture.Release();Object.DestroyImmediate(texture);}
            }
        }
    }
}
