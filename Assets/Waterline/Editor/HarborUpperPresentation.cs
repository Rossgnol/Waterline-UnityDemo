using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    public static partial class HarborPresentationBuilder
    {
        private const string UpperRoot="Upper workstations presentation";
        public static void ApplyUpperAndCapture(){ApplyUpper();TidalRouteVisualCheck.RunUpperArt();}
        [MenuItem("Waterline/28 Repair upper workstations")]
        public static void ApplyUpper()
        {
            TidalRouteBuilder.Open();var baseline=PhysicsSignature();
            var old=GameObject.Find(UpperRoot);if(old!=null)Object.DestroyImmediate(old);
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Upper pickup dressing").ToArray())Object.DestroyImmediate(t.gameObject);
            root=new GameObject(UpperRoot).transform;
            // Reuse the already validated local palette without changing its materials or older scenes.
            steel=UpperMat("Machined steel");ivory=UpperMat("Gauge face");brass=UpperMat("Drain ochre");blue=UpperMat("Inlet blue");rubber=UpperMat("Recess and rubber");
            var h=Object.FindFirstObjectByType<HarborLayout>();
            var visual=root.gameObject.AddComponent<UpperInstrumentVisuals>();visual.annex=h.annex;
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.type==LightType.Directional);
            var data=sun.GetComponent<UniversalAdditionalLightData>();if(data==null)data=sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            // The global 1/1 biases leaked daylight over thin indoor worktops. Override this scene's light only.
            data.usePipelineSettings=false;sun.shadowBias=.15f;sun.shadowNormalBias=.02f;
            PickupFuse(h.annex.fuse);
            var previous=root;root=new GameObject("Upper pickup dressing").transform;root.SetParent(h.calibrationPlate.transform,true);
            h.calibrationPlate.GetComponent<Renderer>().enabled=false;Plate(new Vector3(-9,1.05f,66));root=previous;
            var frame=UpperItem(AnnexAction.AlignTideGauge);frame.GetComponent<Renderer>().enabled=false;
            Box("Calibration mounting carriage",new Vector3(19,1.12f,65),new Vector3(.8f,.24f,.51f),steel);
            Box("Calibration receiving slot",new Vector3(19,1.245f,65),new Vector3(.72f,.025f,.29f),rubber);
            var mounted=new GameObject("Installed measuring plate");mounted.transform.SetParent(root);visual.installedPlate=mounted;
            previous=root;root=mounted.transform;Plate(new Vector3(19,1.29f,65));root=previous;mounted.SetActive(false);
            SignalWheels(h.annex,visual);
            DockControlFaces();
            // A real service mat has thickness and edge clearance; it does not coincide with the tabletop.
            foreach(string name in new[]{"Fuse bench service mat","Calibration bench mat"})
            {
                var mat=GameObject.Find(name);var p=mat.transform.position;p.y=1.009f;mat.transform.position=p;
                var size=mat.transform.localScale;size.y=.014f;mat.transform.localScale=size;
            }
            if(PhysicsSignature()!=baseline)throw new InvalidOperationException("Upper art changed collision or interactions");
            if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Upper decor contains collision");
            TidalRouteBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),TidalRouteBuilder.ScenePath);
            Capture("day11-upper-after");CaptureWings("day11-upper-after");
            Debug.Log("[Waterline] UPPER PRESENTATION PASS: original collision/interaction transforms unchanged; local shadow bias, fuse, measuring plate, signal handwheels and dock controls saved.");
        }
        private static Material UpperMat(string name)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waterline/Generated/TidalPresentation/"+name+".mat");
            if(mat==null)throw new InvalidOperationException("Missing local palette "+name);return mat;
        }
        private static AnnexInteractable UpperItem(AnnexAction action)
        {return Object.FindObjectsByType<AnnexInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(i=>i.action==action);}
        private static void PickupFuse(GameObject pickup)
        {
            pickup.GetComponent<Renderer>().enabled=false;
            var saved=root;root=new GameObject("Upper pickup dressing").transform;root.SetParent(pickup.transform,true);
            var p=pickup.transform.position;p.y=1.01f;
            Pipe("Ceramic fuse barrel",p+Vector3.up*.04f,p+Vector3.up*.24f,.075f,ivory);
            Pipe("Fuse lower contact",p,p+Vector3.up*.05f,.086f,brass);
            Pipe("Fuse upper contact",p+Vector3.up*.23f,p+Vector3.up*.28f,.086f,brass);
            Box("Fuse identification band",p+new Vector3(0,.15f,-.077f),new Vector3(.075f,.075f,.015f),blue);
            root=saved;
        }
        private static void Plate(Vector3 p)
        {
            Box("Enamel measuring plate",p,new Vector3(.78f,.055f,.27f),ivory);
            foreach(float x in new[]{-.33f,.33f})Box("Plate support foot",p+new Vector3(x,-.035f,0),new Vector3(.07f,.04f,.2f),rubber);
            for(int i=0;i<15;i++)Box("Measuring graduation",p+new Vector3(-.35f+i*.05f,.031f,.05f),new Vector3(.014f,.006f,i%5==0?.14f:.075f),rubber);
            foreach(float x in new[]{-.35f,.35f})Pipe("Plate registration pin",p+new Vector3(x,.028f,-.09f),p+new Vector3(x,.05f,-.09f),.022f,brass);
        }
        private static void SignalWheels(AnnexSession annex,UpperInstrumentVisuals visual)
        {
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Signal hand wheel"))r.enabled=false;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Waterline/Generated/TidalPresentation/Handwheel.asset");
            if(mesh==null)throw new InvalidOperationException("Missing handwheel mesh");
            visual.signalWheels=new Transform[3];
            for(int i=0;i<3;i++)
            {
                float x=23.7f+i*1.25f;var color=i==1?blue:brass;
                var item=UpperItem((AnnexAction)((int)AnnexAction.DialHarbor+i));item.GetComponent<Renderer>().sharedMaterial=steel;
                Pipe("Signal spindle",new Vector3(x,1.3f,15),new Vector3(x,1.41f,15),.055f,ivory);
                var saved=root;var rotor=new GameObject("Signal wheel rotor").transform;rotor.SetParent(root);rotor.position=new Vector3(x,1.4f,15);root=rotor;visual.signalWheels[i]=rotor;
                var rim=new GameObject("Signal wheel rim",typeof(MeshFilter),typeof(MeshRenderer));rim.transform.SetParent(root,false);rim.transform.localRotation=Quaternion.Euler(90,0,0);rim.GetComponent<MeshFilter>().sharedMesh=mesh;rim.GetComponent<Renderer>().sharedMaterial=color;
                for(int j=0;j<3;j++){float a=j*Mathf.PI*2/3;Pipe("Signal wheel spoke",rotor.position,rotor.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.24f,.022f,color);}
                Box("Signal wheel index",rotor.position+Vector3.back*.26f,new Vector3(.07f,.055f,.075f),ivory);
                root=saved;
                var p=annex.dialLabels[i].transform.position;
                Box("Signal readout backing",p+Vector3.forward*.04f,new Vector3(1.03f,.32f,.055f),rubber);
                foreach(float dx in new[]{-.44f,.44f})Pipe("Readout support",new Vector3(x+dx,1.01f,15.55f),new Vector3(x+dx,1.9f,15.55f),.019f,steel);
            }
        }
        private static void DockControlFaces()
        {
            foreach(var item in Object.FindObjectsByType<DockInteractable>(FindObjectsSortMode.None).Where(i=>i.action!=DockAction.TakePin))
            {
                var b=item.GetComponent<Renderer>().bounds;
                Box("Dock control fascia",new Vector3(b.center.x,b.center.y,b.min.z-.014f),new Vector3(b.size.x*.83f,b.size.y*.76f,.024f),steel);
                var button=new Vector3(b.center.x+b.size.x*.22f,b.center.y,b.min.z-.035f);
                Pipe("Control bezel",button,button+Vector3.back*.035f,.066f,rubber);
                Pipe("Control push button",button+Vector3.back*.036f,button+Vector3.back*.052f,.047f,item.action==DockAction.StartFlood?brass:blue);
                for(int i=0;i<3;i++)Box("Control indicator slot",new Vector3(b.center.x-b.size.x*.22f+i*.055f,b.center.y,b.min.z-.03f),new Vector3(.025f,.048f,.012f),ivory);
            }
        }
    }
}
