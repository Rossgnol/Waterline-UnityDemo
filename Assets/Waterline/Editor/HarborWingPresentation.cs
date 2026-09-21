using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static partial class HarborPresentationBuilder
    {
        private const string WingRoot = "Harbor presentation - repair and survey wings";
        [MenuItem("Waterline/20 Apply repair and survey presentation")]
        public static void ApplyWings()
        {
            ExplorationRewardsBuilder.Open();
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var signature=PhysicsSignature();var previous=GameObject.Find(WingRoot);
            if(previous==null)CaptureWings("before");else Object.DestroyImmediate(previous);
            LoadMaterials();root=new GameObject(WingRoot).transform;
            RepairWing();SurveyWing();
            if(signature!=PhysicsSignature())throw new InvalidOperationException("Wing presentation changed original collision or interaction transforms");
            if(root.GetComponentsInChildren<Collider>(true).Length!=0 || root.GetComponentsInChildren<Light>(true).Length!=0)
                throw new InvalidOperationException("Wing decoration changed physics or light perception");
            ExplorationRewardsBuilder.Validate();AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ExplorationRewardsBuilder.ScenePath);
            CaptureWings("after");
            Debug.Log("[Waterline] WINGS PRESENTATION PASS: six work areas dressed; original collision and interactions unchanged.");
        }

        private static void RepairWing()
        {
            // Surface detail on existing solid machine covers preserves established stealth cover.
            foreach(float z in new[]{36f,44f})
            {
                Box("Machine enamel front",new Vector3(-26.785f,1,z),new Vector3(.025f,1.75f,2.72f),steel);
                Box("Machine blue header",new Vector3(-26.765f,1.78f,z),new Vector3(.018f,.18f,2.6f),blue);
                for(int i=0;i<7;i++)Box("Motor ventilation slot",new Vector3(-26.75f,.55f+i*.13f,z-.4f),new Vector3(.02f,.042f,1.25f),rubber);
                Pipe("Machine inspection port",new Vector3(-26.75f,1.17f,z+.77f),new Vector3(-26.72f,1.17f,z+.77f),.24f,ivory);
                Pipe("Port dark center",new Vector3(-26.717f,1.17f,z+.77f),new Vector3(-26.70f,1.17f,z+.77f),.17f,glass);
                Box("Machine base band",new Vector3(-28,.2f,z),new Vector3(2.42f,.2f,3.02f),steel);
                // The machine top sits at y=2; details remain inside its footprint.
                Pipe("Lathe spindle",new Vector3(-28,2.22f,z-.83f),new Vector3(-28,2.22f,z+.83f),.22f,ivory);
                foreach(float end in new[]{-.95f,.95f})Box("Spindle support",new Vector3(-28,2.12f,z+end),new Vector3(.7f,.24f,.26f),steel);
            }
            // Tool silhouettes sit directly on the west wall, away from the cabinet and return latch.
            Box("Workshop shadow board",new Vector3(-29.84f,1.65f,40),new Vector3(.035f,1.8f,3.1f),steel);
            for(int i=0;i<6;i++)
            {
                float z=38.8f+i*.47f;
                Pipe("Hanging spanner",new Vector3(-29.8f,1.25f,z),new Vector3(-29.8f,2.05f,z),.033f,ivory);
                Box("Spanner jaw",new Vector3(-29.78f,2.07f,z),new Vector3(.04f,.13f,.19f),ivory);
            }
            // Existing shelving gets explicit bin labels; no extra loose props occupy the aisles.
            foreach(float z in new[]{21f,25f})
            {
                Box("Shelf inventory strip",new Vector3(-28.85f,2.13f,z),new Vector3(.025f,.14f,2.1f),ivory);
                for(int row=0;row<3;row++)for(int col=0;col<4;col++)
                    Box("Parts bin tag",new Vector3(-28.919f,.44f+row*.5f,z-.8f+col*.5f),new Vector3(.022f,.07f,.19f),ivory);
            }
            Box("Fuse bench service mat",new Vector3(-27,1.006f,29),new Vector3(2.5f,.008f,.72f),blue);
            Board(new Vector3(-25,2.25f,18.2f),180,"ELECTRICAL / SPARES","FUSE > AUX POWER",blue);
            // Generator details wrap its existing body; the interaction pedestal stays exposed.
            for(int i=0;i<7;i++)Box("Generator grille",new Vector3(-11.085f,.85f,25.9f+i*.34f),new Vector3(.025f,.72f,.05f),rubber);
            Pipe("Generator exhaust",new Vector3(-12,2.07f,27.8f),new Vector3(-12,3.08f,27.8f),.12f,steel);
            Pipe("Exhaust to wall",new Vector3(-12,3.08f,27.8f),new Vector3(-13.85f,3.08f,27.8f),.12f,steel);
            Box("Power cable tray",new Vector3(-10,3.16f,24),new Vector3(7.7f,.12f,.4f),steel);
        }

        private static void SurveyWing()
        {
            // Instrument racks become recognisable storage for surveying optics.
            foreach(float x in new[]{-12f,2f})
            {
                Box("Instrument rack face",new Vector3(x,1,57.985f),new Vector3(1.64f,1.82f,.025f),steel);
                for(int i=0;i<3;i++)
                {
                    Box("Instrument drawer",new Vector3(x,.4f+i*.47f,57.962f),new Vector3(1.43f,.39f,.02f),ivory);
                    Box("Drawer recessed pull",new Vector3(x,.4f+i*.47f,57.947f),new Vector3(.48f,.055f,.012f),rubber);
                }
                Pipe("Stored optical barrel",new Vector3(x,2.19f,58.5f),new Vector3(x,2.19f,59.5f),.19f,brass);
                Pipe("Stored lens",new Vector3(x,2.19f,58.47f),new Vector3(x,2.19f,58.49f),.15f,glass);
                Box("Optics cradle",new Vector3(x,2.065f,59),new Vector3(.7f,.13f,1.1f),steel);
            }
            Box("Calibration bench mat",new Vector3(-9,1.007f,66),new Vector3(3.5f,.01f,.72f),blue);
            // A wall chart frames the existing calibration label without introducing a second numeric puzzle.
            Box("Survey reference chart",new Vector3(-9,1.55f,69.84f),new Vector3(5.2f,1.7f,.035f),steel);
            foreach(float zOffset in new[]{-.58f,.58f})
                Box("Drawing plate edge",new Vector3(-9,1.55f+zOffset,69.81f),new Vector3(3.8f,.035f,.015f),ivory);
            foreach(float xOffset in new[]{-1.9f,1.9f})
                Box("Drawing plate edge",new Vector3(-9+xOffset,1.55f,69.81f),new Vector3(.035f,1.16f,.015f),ivory);
            foreach(float xOffset in new[]{-1.5f,1.5f})foreach(float yOffset in new[]{-.35f,.35f})
            {
                Pipe("Drawing mounting hole",new Vector3(-9+xOffset,1.55f+yOffset,69.79f),new Vector3(-9+xOffset,1.55f+yOffset,69.80f),.08f,brass);
            }
            Board(new Vector3(-3,2.75f,69.78f),0,"INSTRUMENTS / CALIBRATION","PLATE > TIDE FRAME",brass);
            // Tide gauges remain the actual existing instruments; backing and connected plumbing add purpose.
            foreach(float x in new[]{21f,23.6f,26.2f})
            {
                Box("Gauge enamel backing",new Vector3(x,1.7f,68.24f),new Vector3(1.08f,3.45f,.07f),steel);
                foreach(float y in new[]{.35f,3.05f})
                    Pipe("Gauge mounting saddle",new Vector3(x,y,68.18f),new Vector3(x,y,68),.26f,ivory);
                Pipe("Gauge riser",new Vector3(x,3.37f,68),new Vector3(x,3.92f,68),.095f,brass);
            }
            Pipe("Observation manifold",new Vector3(20.5f,3.92f,68),new Vector3(27.2f,3.92f,68),.12f,steel);
            Pipe("Manifold wall feed",new Vector3(27.2f,3.92f,68),new Vector3(29.75f,3.92f,68),.12f,steel);
            foreach(float x in new[]{20.8f,25.2f})
                Box("Recording console drawer",new Vector3(x, .65f,66),new Vector3(.35f,.5f,.72f),steel);
            // Signal controls get a panel fascia and return diagram; keep every dial and its number legible.
            Box("Signal desk fascia",new Vector3(25,.7f,14.535f),new Vector3(3.8f,.34f,.025f),steel);
            for(int i=0;i<3;i++)
            {
                float x=23.7f+i*1.25f;
                Box("Signal channel band",new Vector3(x,.7f,14.515f),new Vector3(.8f,.13f,.02f),i==1?blue:brass);
                Pipe("Signal feed conduit",new Vector3(x,2.45f,17.8f),new Vector3(x,3.12f,17.8f),.045f,steel);
            }
            Pipe("Signal header conduit",new Vector3(23.7f,3.12f,17.8f),new Vector3(26.2f,3.12f,17.8f),.06f,steel);
            Board(new Vector3(25,2.55f,6.2f),180,"DEPARTURE / SIGNAL","TIDE > PRESSURE > DOCK",brass);
        }

        public static void CaptureWingsAfter(){ExplorationRewardsBuilder.Open();CaptureWings("after");}
        public static void CaptureTidalAudit()
        {
            TidalRouteBuilder.Open();Capture("day11-before");CaptureWings("day11-before");
            Debug.Log("[Waterline] TIDAL UPPER AUDIT CAPTURED: current scene, no save or gameplay changes.");
        }
        public static void CaptureTidalShadowAudit()
        {
            TidalRouteBuilder.Open();
            var bench=GameObject.Find("Spare fuse bench").GetComponent<Renderer>().bounds;bench.Expand(.05f);
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(r.bounds.Intersects(bench))Debug.Log("[Upper audit] "+r.name+" bounds="+r.bounds+" material="+r.sharedMaterial?.name);
                if(r.bounds.center.y>.8f && r.bounds.center.y<1.05f && r.bounds.size.y<.2f)r.receiveShadows=false;
            }
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))Debug.Log("[Upper light] "+l.name+" "+l.type+" bias="+l.shadowBias+" normal="+l.shadowNormalBias+" shadows="+l.shadows);
            CaptureWings("shadow-probe");
        }
        public static void CaptureTidalNoShadows()
        {
            TidalRouteBuilder.Open();
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.shadows=LightShadows.None;
            CaptureWings("no-shadow-probe");
        }
        private static void CaptureWings(string phase)
        {
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            Directory.CreateDirectory("TestResults/WingPresentation");
            Shot("workshop",new Vector3(-23,1.7f,34),new Vector3(-27,1.7f,42));
            Shot("spares",new Vector3(-23,1.7f,24),new Vector3(-28,1.2f,28));
            Shot("power",new Vector3(-8,1.7f,30),new Vector3(-12,1.3f,27));
            Shot("instruments",new Vector3(-3,1.7f,60),new Vector3(-9,1.8f,66));
            Shot("tide",new Vector3(18,1.7f,61),new Vector3(23,1.8f,67));
            Shot("signal",new Vector3(25,1.7f,10),new Vector3(25,1.4f,15));
            camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;
            void Shot(string name,Vector3 p,Vector3 target)
            {
                camera.transform.position=p;camera.transform.LookAt(target);camera.fieldOfView=72;
                var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;var oldTarget=camera.targetTexture;
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("TestResults/WingPresentation/"+name+"-"+phase+".png",image.EncodeToPNG());
                camera.targetTexture=oldTarget;RenderTexture.active=active;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
            }
        }
    }
}
