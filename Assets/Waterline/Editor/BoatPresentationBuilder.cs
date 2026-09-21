using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static class BoatPresentationBuilder
    {
        public const string BoatRoot="Boat finish presentation";
        public const string BridgeRoot="Boarding finish presentation";
        private const string Folder="Assets/Waterline/Generated/BoatFinish";
        private static Transform root;
        private static Material steel,enamel,rubber,glass,ochre,ivory;
        public static void Audit()
        {
            TidalRouteBuilder.Open();var d=Object.FindFirstObjectByType<DockSession>();
            foreach(var r in d.boat.GetComponentsInChildren<Renderer>())Debug.Log("BOAT AUDIT "+r.name+" bounds="+r.bounds+" scale="+r.transform.lossyScale);
            CaptureSaved("before");
        }
        public static void CaptureSaved(){TidalRouteBuilder.Open();CaptureSaved("after");}
        [MenuItem("Waterline/30 Refine inspection boat and boarding bridge")]
        public static void Apply()
        {
            TidalRouteBuilder.Open();string baseline=Signature();var dock=Object.FindFirstObjectByType<DockSession>();
            var h=Object.FindFirstObjectByType<HarborLayout>();
            foreach(var name in new[]{BoatRoot,BridgeRoot})foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name==name).ToArray())Object.DestroyImmediate(t.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            steel=Material("Graphite steel");enamel=Material("Aged enamel");rubber=Material("Fender rubber");glass=Material("Smoked marine glass");ochre=Material("Safety ochre");ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/Gauge face.mat");
            var old=dock.boat.Find("Inspection boat detailing");foreach(var r in old.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            dock.boat.Find("Cabin window").GetComponent<Renderer>().enabled=false;
            var hull=dock.boat.Find("Hull").GetComponent<MeshFilter>();hull.sharedMesh=ClosedHull();
            root=Child(BoatRoot,dock.boat);
            var cabin=dock.boat.Find("Cabin").GetComponent<Renderer>().bounds;
            var deck=dock.boat.Find("Deck").GetComponent<Renderer>().bounds;
            var hullBounds=hull.GetComponent<Renderer>().bounds;
            Wheelhouse(cabin);Deck(deck,cabin,hullBounds);Combine(root,"Boat");Gangway(h.Planner.boardingGangway);Combine(root,"Gangway");
            if(baseline!=Signature())throw new InvalidOperationException("Boat finish changed original collision or interactions");
            foreach(string name in new[]{BoatRoot,BridgeRoot})
            {
                var t=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name==name);
                if(t.GetComponentsInChildren<Collider>(true).Length!=0 || t.GetComponentsInChildren<Light>(true).Length!=0)throw new InvalidOperationException("Decor contains physics or lighting");
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))if(!float.IsFinite(r.bounds.center.sqrMagnitude) || r.sharedMaterial==null)throw new InvalidOperationException("Invalid boat decoration: "+r.name);
                Debug.Log("[Waterline] "+name+": "+t.GetComponentsInChildren<Renderer>(true).Length+" renderers");
            }
            TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Debug.Log("[Waterline] BOAT FINISH PASS: watertight visual hull and UVs, wheelhouse, suspended fenders, berth opening and boarding rails. Original collision/interactions unchanged.");
        }
        private static Material Material(string name){return AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/Presentation/"+name+".mat");}
        private static Transform Child(string name,Transform parent)
        {var t=new GameObject(name).transform;t.SetParent(parent,true);return t;}
        private static void Wheelhouse(Bounds c)
        {
            Box("Roof edge",new Vector3(c.center.x,c.max.y+.055f,c.center.z),new Vector3(c.size.x+.12f,.11f,c.size.z+.12f),steel);
            Box("Roof enamel cap",new Vector3(c.center.x,c.max.y+.12f,c.center.z),new Vector3(c.size.x+.07f,.035f,c.size.z+.07f),enamel);
            foreach(float side in new[]{-1f,1f})for(int i=0;i<2;i++)
            {
                var p=new Vector3(c.center.x+side*(c.extents.x+.018f),c.center.y+.17f,c.center.z-.5f+i*.98f);
                Window(p,new Vector3(.035f,.52f,.81f),side*Vector3.right);
            }
            for(int i=0;i<2;i++)
            {
                var p=new Vector3(c.center.x-.56f+i*1.12f,c.center.y+.19f,c.max.z+.02f);
                Window(p,new Vector3(.99f,.54f,.036f),Vector3.forward);
                Pipe("Windshield wiper",p+new Vector3(-.35f,-.23f,.05f),p+new Vector3(.22f,.06f,.05f),.009f,rubber);
            }
            float z=c.min.z-.02f;
            Box("Rear access door frame",new Vector3(c.center.x,c.center.y-.06f,z),new Vector3(.8f,1.08f,.035f),steel);
            Box("Rear access door leaf",new Vector3(c.center.x,c.center.y-.06f,z-.024f),new Vector3(.72f,1,.026f),enamel);
            Window(new Vector3(c.center.x,c.center.y+.19f,z-.045f),new Vector3(.48f,.35f,.024f),Vector3.back);
            Pipe("Door grab handle",new Vector3(c.center.x+.26f,c.center.y-.21f,z-.075f),new Vector3(c.center.x+.26f,c.center.y-.04f,z-.075f),.017f,steel);
            for(int i=0;i<3;i++)Box("Rear door hinge",new Vector3(c.center.x-.37f,c.center.y-.40f+i*.32f,z-.04f),new Vector3(.07f,.06f,.055f),steel);
            foreach(float side in new[]{-1f,1f})
            {
                Box("Rear wheelhouse vent backing",new Vector3(c.center.x+side*.93f,c.center.y-.05f,z),new Vector3(.45f,.62f,.03f),steel);
                for(int i=0;i<5;i++)Box("Rear wheelhouse vent slat",new Vector3(c.center.x+side*.93f,c.center.y-.28f+i*.11f,z-.025f),new Vector3(.39f,.035f,.023f),enamel);
                Pipe("Roof handhold",new Vector3(c.center.x+side*(c.extents.x-.17f),c.max.y+.24f,c.center.z-.76f),new Vector3(c.center.x+side*(c.extents.x-.17f),c.max.y+.24f,c.center.z+.76f),.025f,steel);
                foreach(float end in new[]{-.76f,.76f})Pipe("Roof handhold mount",new Vector3(c.center.x+side*(c.extents.x-.17f),c.max.y+.13f,c.center.z+end),new Vector3(c.center.x+side*(c.extents.x-.17f),c.max.y+.24f,c.center.z+end),.022f,steel);
            }
            var mast=new Vector3(c.center.x,c.max.y+.13f,c.center.z);Pipe("Navigation mast",mast,mast+Vector3.up*.65f,.035f,steel);
            Cylinder("Navigation light housing",mast+Vector3.up*.66f,.095f,.10f,steel);
            Cylinder("Navigation lens",mast+Vector3.up*.76f,.072f,.13f,ivory);
            Cylinder("Navigation cap",mast+Vector3.up*.845f,.10f,.03f,steel);
        }
        private static void Window(Vector3 p,Vector3 size,Vector3 outward)
        {
            Box("Window gasket",p,size,rubber);
            var inner=size;if(size.x<.1f){inner.y-=.065f;inner.z-=.065f;inner.x=.016f;}else{inner.x-=.065f;inner.y-=.065f;inner.z=.016f;}
            Box("Marine glass",p+outward*.025f,inner,glass);
        }
        private static void Deck(Bounds d,Bounds c,Bounds h)
        {
            foreach(float side in new[]{-1f,1f})
            {
                float x=d.center.x+side*(d.extents.x-.13f);
                // The starboard gap aligns with the existing forward gangway at world z=0.
                if(side>0){Rail(x,d.max.y,d.min.z+.34f,-.66f);Rail(x,d.max.y,.66f,d.max.z-.34f);}else Rail(x,d.max.y,d.min.z+.34f,d.max.z-.34f);
                foreach(float z in new[]{d.center.z-2.9f,d.center.z-1.65f,d.center.z+1.65f,d.center.z+2.9f})
                {
                    var f=new Vector3(h.center.x+side*(h.extents.x+.035f),h.max.y-.28f,z);
                    Primitive(PrimitiveType.Capsule,"Rounded rubber fender",f,new Vector3(.21f,.30f,.21f),Quaternion.identity,rubber);
                    Pipe("Fender hanging rope",new Vector3(x,d.max.y+.12f,z),f+Vector3.up*.27f,.016f,ivory);
                    Cylinder("Fender tie mount",new Vector3(x,d.max.y+.09f,z),.05f,.16f,steel);
                }
                foreach(float z in new[]{d.min.z+.5f,Mathf.Min(d.max.z-.5f,-1.05f)})
                {
                    Box("Mooring cleat shoe",new Vector3(x,d.max.y+.025f,z),new Vector3(.25f,.05f,.36f),steel);
                    foreach(float dz in new[]{-.075f,.075f})Pipe("Cleat neck",new Vector3(x,d.max.y+.05f,z+dz),new Vector3(x,d.max.y+.15f,z+dz),.025f,steel);
                    Pipe("Mooring cleat bar",new Vector3(x,d.max.y+.15f,z-.21f),new Vector3(x,d.max.y+.15f,z+.21f),.035f,steel);
                }
                // Rub strip follows the broader upper hull, including its chamfered ends.
                float edge=h.center.x+side*h.extents.x;
                Pipe("Hull rubbing strake",new Vector3(edge,h.max.y-.10f,h.center.z-h.extents.z*.82f),new Vector3(edge,h.max.y-.10f,h.center.z+h.extents.z*.82f),.045f,rubber);
            }
            foreach(float z in new[]{c.min.z-.54f,c.max.z+1.75f})
            {
                Box("Non-slip working deck",new Vector3(d.center.x,d.max.y+.009f,z),new Vector3(2.42f,.016f,1.02f),steel);
                for(int i=0;i<6;i++)Box("Deck grip rib",new Vector3(d.center.x,d.max.y+.021f,z-.4f+i*.16f),new Vector3(2.28f,.009f,.024f),rubber);
            }
            var hatch=new Vector3(d.center.x,d.max.y+.055f,d.max.z-.65f);
            Box("Foredeck hatch frame",hatch,new Vector3(1.05f,.10f,.83f),steel);
            Box("Foredeck hatch lid",hatch+Vector3.up*.057f,new Vector3(.96f,.025f,.74f),enamel);
            Pipe("Hatch recessed pull",hatch+new Vector3(-.16f,.08f,0),hatch+new Vector3(.16f,.08f,0),.02f,steel);
            // Surface paint identifies the berth without adding a second interaction or a false route.
            foreach(float z in new[]{-.58f,Mathf.Min(.58f,d.max.z-.06f)})Box("Boat boarding threshold paint",new Vector3(d.max.x-.28f,d.max.y+.012f,z),new Vector3(.54f,.012f,.045f),ochre);
            var sign=new Vector3(c.center.x,c.min.y+.21f,c.max.z+.037f);Box("Registration plaque",sign,new Vector3(1.35f,.27f,.04f),steel);Label("WL - 07",sign+Vector3.forward*.024f,180,.04f);
        }
        private static void Rail(float x,float y,float a,float b)
        {
            if(b-a<.15f)return; // The existing berth opening reaches the bow; do not create a negative-length segment.
            int count=Mathf.CeilToInt((b-a)/1.2f);
            for(int i=0;i<=count;i++){float z=Mathf.Lerp(a,b,(float)i/count);Pipe("Deck rail stanchion",new Vector3(x,y,z),new Vector3(x,y+.68f,z),.025f,steel);}
            foreach(float height in new[]{.35f,.68f})Pipe("Deck handrail",new Vector3(x,y+height,a),new Vector3(x,y+height,b),.027f,steel);
        }
        private static void Gangway(GameObject bridge)
        {
            root=Child(BridgeRoot,bridge.transform);var b=bridge.GetComponent<Renderer>().bounds;
            foreach(float side in new[]{-1f,1f})
            {
                float z=b.center.z+side*(b.extents.z-.055f);
                for(int i=0;i<=5;i++){float x=Mathf.Lerp(b.min.x+.08f,b.max.x-.08f,i/5f);Pipe("Boarding rail upright",new Vector3(x,b.max.y,z),new Vector3(x,b.max.y+.85f,z),.027f,steel);}
                foreach(float height in new[]{.42f,.85f})Pipe("Boarding bridge rail",new Vector3(b.min.x+.08f,b.max.y+height,z),new Vector3(b.max.x-.08f,b.max.y+height,z),.032f,ochre);
                Box("Bridge edge stripe",new Vector3(b.center.x,b.max.y+.009f,z),new Vector3(b.size.x-.04f,.015f,.045f),ochre);
            }
            for(int i=0;i<17;i++)Box("Boarding anti-slip tread",new Vector3(b.min.x+.10f+i*(b.size.x-.20f)/16,b.max.y+.010f,b.center.z),new Vector3(.035f,.016f,.76f),rubber);
        }
        private static Mesh ClosedHull()
        {
            // The upper shoulders now support the rectangular deck; all vertices stay inside the original box collider.
            var ring=new[]{new Vector2(-.43f,-.5f),new Vector2(.43f,-.5f),new Vector2(.5f,-.41f),new Vector2(.5f,.41f),new Vector2(.43f,.5f),new Vector2(-.43f,.5f),new Vector2(-.5f,.41f),new Vector2(-.5f,-.41f)};
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});tri.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];var at=new Vector3(a.x,.5f,a.y);var bt=new Vector3(b.x,.5f,b.y);var ab=new Vector3(a.x*.8f,-.5f,a.y*.88f);var bb=new Vector3(b.x*.8f,-.5f,b.y*.88f);
                Face(at,bt,bb,ab);Cap(Vector3.up*.5f,bt,at);Cap(Vector3.down*.5f,ab,bb);
            }
            var m=new Mesh{name="Closed inspection hull"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();
            return SaveMesh("Closed hull",m);
            void Cap(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.AddRange(new[]{a,b,c});foreach(var p in new[]{a,b,c})uv.Add(new Vector2(p.x+.5f,p.z+.5f));tri.AddRange(new[]{n,n+1,n+2});}
        }
        private static Mesh SaveMesh(string name,Mesh mesh)
        {
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }
        private static void Combine(Transform parent,string name)
        {
            var parts=parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()!=null).ToArray();
            foreach(var group in parts.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh{name=name+" "+group.Key.name};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
                var go=new GameObject("Combined "+group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh.name,mesh);go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var r in parts)Object.DestroyImmediate(r.gameObject);
        }
        private static string Signature()
        {
            var c=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>x.GetInstanceID()+EditorJsonUtility.ToJson(x)+x.gameObject.activeSelf+x.transform.localToWorldMatrix.ToString("F5"));
            var a=Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.transform.localToWorldMatrix.ToString("F5"));
            var d=Object.FindObjectsByType<DockInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.GetInstanceID()).Select(x=>EditorJsonUtility.ToJson(x)+x.transform.localToWorldMatrix.ToString("F5"));return string.Join("\n",c.Concat(a).Concat(d));
        }
        private static GameObject Box(string n,Vector3 p,Vector3 s,Material m){return Primitive(PrimitiveType.Cube,n,p,s,Quaternion.identity,m);}
        private static void Pipe(string n,Vector3 a,Vector3 b,float r,Material m){Primitive(PrimitiveType.Cylinder,n,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),Quaternion.FromToRotation(Vector3.up,b-a),m);}
        private static void Cylinder(string n,Vector3 p,float r,float height,Material m){Primitive(PrimitiveType.Cylinder,n,p,new Vector3(r*2,height*.5f,r*2),Quaternion.identity,m);}
        private static GameObject Primitive(PrimitiveType type,string name,Vector3 p,Vector3 s,Quaternion q,Material m)
        {var go=GameObject.CreatePrimitive(type);Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetPositionAndRotation(p,q);go.transform.localScale=s;go.transform.SetParent(root,true);go.GetComponent<Renderer>().sharedMaterial=m;return go;}
        private static void Label(string value,Vector3 p,float yaw,float size)
        {
            var previous=AnnexBuilder.root;AnnexBuilder.root=root;AnnexBuilder.Label(value,p,yaw,size);AnnexBuilder.root=previous;
        }
        private static void CaptureSaved(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;string folder="TestResults/BoatFinish/"+phase;Directory.CreateDirectory(folder);
            Shot(camera,folder+"/stern.png",new Vector3(4,-1.1f,-7),new Vector3(0,-1.8f,-1));
            Shot(camera,folder+"/bow.png",new Vector3(4,-1.1f,3.8f),new Vector3(0,-1.7f,-.5f));
            Shot(camera,folder+"/upper-berth.png",new Vector3(9,1.7f,3),new Vector3(1,-.9f,-.2f));
            Debug.Log("[Waterline] BOAT CAPTURE "+phase+" complete; no scene save.");
        }
        internal static void Shot(Camera camera,string path,Vector3 position,Vector3 target)
        {
            var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);Texture2D image=null;
            try{camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=68;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(image!=null)Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
