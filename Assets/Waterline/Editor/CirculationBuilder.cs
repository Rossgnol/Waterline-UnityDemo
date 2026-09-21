using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Waterline.Editor.AnnexBuilder;
using Object=UnityEngine.Object;
namespace Waterline.Editor
{
    public static class CirculationBuilder
    {
        public const string ScenePath="Assets/Waterline/Scenes/Day09_Circulation.unity";
        private const string Folder="Assets/Waterline/Generated/Circulation";
        [MenuItem("Waterline/21 Open circulation iteration")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        public static void Generate()
        {
            ExplorationRewardsBuilder.Open();Capture("before");
            annex=Object.FindFirstObjectByType<AnnexSession>();var h=annex.Harbor;
            var west=h.doors.Single(d=>d.from=="west_service" && d.to=="north_gallery");
            west.rule=HarborGateRule.SignalKey;west.hint="北回廊通行锁：信号钥匙统一解锁。送电后先经机房东门、泵压厅去航务档案室。";
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(Vector3.Distance(label.transform.position,new Vector3(-17,2.95f,50))<.5f)label.text="NORTH / ARCHIVE KEY";
            h.Room("west_service").purpose="蓝色服务廊串联工装、零件和机修；北端在取得档案室钥匙后开放。";
            h.Room("archive_exit").purpose="档案室东出口；青色回程线连接北回廊与泵压厅，门栓在北回廊。";
            h.Room("inspection_link").purpose="连接档案东廊与东侧长廊；青色回程线西折返回泵压厅。";
            foreach(var door in h.doors.Where(d=>d.rule==HarborGateRule.ObservationReturn))
                door.hint="观测回程联动门：北回廊门栓同时打开这两道门，沿青色管线接回泵压厅。";
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var dark=Material("Return structure",new Color(.07f,.15f,.17f));
            var closed=Material("Return dormant",new Color(.18f,.45f,.44f));
            var active=Material("Return active",new Color(.16f,.78f,.69f));
            var gold=Material("Latch amber",new Color(.8f,.51f,.17f));
            root=new GameObject("Circulation / linked return route").transform;
            var link=annex.gameObject.AddComponent<HarborReturnLink>();link.annex=annex;link.closedMaterial=closed;link.openMaterial=active;
            var marks=new List<Renderer>();var labels=new List<TextMesh>();
            // A continuous physical route motif connects the north latch to the pump-hall gate.
            Line(new Vector3(11.5f,.025f,52.3f),new Vector3(10,.025f,52.3f),.18f);
            Line(new Vector3(10,.025f,52.3f),new Vector3(10,.025f,30),.18f);
            Line(new Vector3(10,.025f,30),new Vector3(5,.025f,30),.18f);
            // High twin pipes make this corridor recognisable from either entrance without blocking sightlines.
            foreach(float offset in new[]{-.22f,.22f})
            {
                Line(new Vector3(10+offset,3.15f,49.6f),new Vector3(10+offset,3.15f,30+offset),.075f);
                Line(new Vector3(10+offset,3.15f,30+offset),new Vector3(6.4f,3.15f,30+offset),.075f);
            }
            foreach(var d in h.doors.Where(d=>d.rule==HarborGateRule.ObservationReturn))
            {
                // Frame is outside the clear 2.5m aperture; old gate and physics are retained.
                Vector3 along=d.vertical?Vector3.forward:Vector3.right;
                Vector3 normal=d.vertical?Vector3.right:Vector3.forward;
                foreach(float side in new[]{-1.42f,1.42f})foreach(float face in new[]{-1f,1f})
                {
                    var post=Cube("Return identification pillar",new Vector3(d.position.x,1.35f,d.position.z)+along*side+normal*face*.25f,new Vector3(.18f,2.7f,.18f),dark,false);
                    marks.Add(Cube("Return pillar inlay",post.transform.position,new Vector3(.2f,1.55f,.2f),closed,false).GetComponent<Renderer>());
                }
                Cube("Return status backing",new Vector3(d.position.x,2.53f,d.position.z),d.vertical?new Vector3(.35f,.27f,2.6f):new Vector3(2.6f,.27f,.35f),dark,false);
                foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                {
                    if(Vector3.Distance(label.transform.position,new Vector3(d.position.x,2.95f,d.position.z))>.5f)continue;
                    bool positive=Vector3.Dot(label.transform.position-d.position,normal)>0;
                    label.text=d.vertical?(positive?"PUMP HALL RETURN":"NORTH GALLERY RETURN"):(positive?"PUMP HALL RETURN":"NORTH GALLERY");
                }
                foreach(float face in new[]{-1f,1f})
                {
                    float yaw=d.vertical?(face<0?90:-90):(face<0?0:180);
                    labels.Add(Label("RETURN LINK / NORTH LATCH",new Vector3(d.position.x,2.53f,d.position.z)+normal*face*.26f,yaw,.018f));
                }
            }
            var latch=Object.FindObjectsByType<AnnexInteractable>(FindObjectsSortMode.None).Single(i=>i.action==Waterline.Core.AnnexAction.OpenObservationLoop);
            latch.description="联动打开正前方的档案东廊门与南端泵压厅门；沿青色回程线即可返回。";
            var p=latch.transform.position;
            Cube("Return latch backing",p+new Vector3(0,.2f,-.24f),new Vector3(1.05f,1.1f,.06f),dark,false);
            // The latch itself remains fully exposed and within its original interaction reach.
            foreach(float x in new[]{-.46f,.46f})Cube("Latch edge",p+new Vector3(x,.2f,-.2f),new Vector3(.06f,.96f,.035f),gold,false);
            link.routeMarkers=marks.ToArray();link.statusLabels=labels.ToArray();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            AssetDatabase.SaveAssets();Validate();Capture("after");
            Debug.Log("[Waterline] CIRCULATION GENERATED: 6/9/19 staged regions and visible linked return; original room/door count retained.");
            void Line(Vector3 a,Vector3 b,float width)
            {
                var line=Cube("Linked return line",(a+b)*.5f,new Vector3(width,a.y<.1f?.01f:width,(b-a).magnitude),closed,false);
                line.transform.rotation=Quaternion.LookRotation(b-a);marks.Add(line.GetComponent<Renderer>());
            }
        }
        private static Material Material(string name,Color color)
        {
            var path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.2f);return mat;
        }
        public static void Validate()
        {
            ExplorationRewardsBuilder.Validate();var h=Object.FindFirstObjectByType<HarborLayout>();
            if(h.rooms.Length!=27 || h.doors.Length!=30 || h.doors.Single(d=>d.from=="west_service" && d.to=="north_gallery").rule!=HarborGateRule.SignalKey)
                throw new Exception("Circulation topology invalid");
            var link=h.GetComponent<HarborReturnLink>();if(link==null || link.routeMarkers.Length<8 || link.statusLabels.Length!=4)throw new Exception("Return route references missing");
            if(GameObject.Find("Circulation / linked return route").GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Return markers must not obstruct routes");
        }
        [MenuItem("Waterline/22 Build circulation iteration")]
        public static void BuildWindows()
        {
            Open();Validate();Directory.CreateDirectory("Builds/Circulation");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Circulation/Waterline.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Circulation build failed");
        }
        internal static void Capture(string phase)
        {
            Directory.CreateDirectory("TestResults/Circulation");var cam=Object.FindFirstObjectByType<FirstPersonController>().view;
            var p=cam.transform.position;var q=cam.transform.rotation;var f=cam.fieldOfView;
            Shot("west-north",new Vector3(-17,1.7f,43),new Vector3(-17,1.6f,50));
            Shot("north-latch",new Vector3(16.5f,1.7f,53),new Vector3(10.5f,1.3f,50.5f));
            Shot("pump-return",new Vector3(1,1.7f,30),new Vector3(6,1.6f,30));
            Shot("return-corridor",new Vector3(10,1.7f,43),new Vector3(10,1.3f,30));
            cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=f;
            void Shot(string name,Vector3 position,Vector3 target)
            {
                cam.transform.position=position;cam.transform.LookAt(target);cam.fieldOfView=72;
                var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("TestResults/Circulation/"+name+"-"+phase+".png",image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=old;
                Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
            }
        }
    }
}
