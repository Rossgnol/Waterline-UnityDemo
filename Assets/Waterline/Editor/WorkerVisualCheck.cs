using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waterline.Core;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class WorkerVisualCheck
    {
        private const string Key="Waterline.WorkerVisual";
        private static double next;
        static WorkerVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){TidalRouteBuilder.Open();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;
        }
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            EditorApplication.update-=Check;
            try
            {
                var layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;player.ResetPose(new Vector3(100,10,100),0);
                var enemies=Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None).OrderBy(e=>e.displayName).ToArray();
                foreach(var e in enemies)e.enabled=false;
                Require(enemies.Length==4,"Four patrol actors missing");
                Require(layout.guards.All(e=>!e.Available && !e.body.gameObject.activeSelf),"Unopened north guard model visible");
                foreach(var a in new[]{DockAction.TakePin,DockAction.InstallPin})Require(layout.dock.State.TryApply(a,out _),"Cannot prepare north guard state");
                layout.annex.State.PowerRestored=true;layout.annex.State.HasSignalKey=true;layout.Apply();
                Require(enemies.All(e=>e.Available && e.body.gameObject.activeSelf),"Unlocked worker model missing");
                Directory.CreateDirectory("TestResults/Workers/runtime");
                for(int i=0;i<enemies.Length;i++)
                {
                    var e=enemies[i];var root=e.body.Find("Rainwear presentation");
                    Require(root!=null && e.limbs.All(t=>t.IsChildOf(root)),"Rig binding broken");
                    Require(e.body.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(root)).All(r=>!r.enabled),"Old block model visible");
                    Shot(player.view,e,i+"-idle");var start=e.transform.position;float swing=0;
                    for(int frame=0;frame<100;frame++){e.Advance(.016f);swing=Mathf.Max(swing,Quaternion.Angle(Quaternion.identity,e.limbs[0].localRotation));}
                    Require(Vector3.Distance(start,e.transform.position)>.25f && swing>5,"Patrol or animated hip did not advance");
                    Require(Quaternion.Angle(e.limbs[0].localRotation,e.limbs[2].localRotation)<.1f,"Opposite shoulder swing not bound to gait");
                    Shot(player.view,e,i+"-walking");
                    Debug.Log("[Waterline] WORKER MOTION "+e.displayName+": moved="+Vector3.Distance(start,e.transform.position)+" maxHipAngle="+swing);
                }
                Debug.Log("[Waterline] WORKER VISUAL PASS: initial hidden north guards, four awakened render rigs, actual Advance-driven hip/shoulder motion, eight in-scene captures.");Finish(0);
            }
            catch(Exception ex){Debug.LogException(ex);Finish(1);}
        }
        private static void Shot(Camera camera,ThreatEncounter e,string name)
        {
            var target=e.transform.position+Vector3.up*1.02f;var position=Vector3.zero;bool found=false;
            foreach(float distance in new[]{3.0f,2.5f,2.0f})
            {
                foreach(float angle in new[]{25f,-25f,0,90,180,270})
                {
                    var candidate=e.transform.position+Quaternion.Euler(0,angle,0)*e.transform.forward*distance+Vector3.up*1.5f;
                    var delta=target-candidate;
                    if(Physics.CheckSphere(candidate,.1f) || Physics.RaycastAll(candidate,delta.normalized,delta.magnitude).Any(h=>!h.transform.IsChildOf(e.transform)))continue;
                    position=candidate;found=true;break;
                }
                if(found)break;
            }
            Require(found,"No unoccluded worker inspection camera: "+e.displayName);
            var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;var old=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1280,720,24);Texture2D image=null;
            try
            {
                camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=58;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("TestResults/Workers/runtime/"+name+".png",image.EncodeToPNG());
            }
            finally{camera.targetTexture=old;RenderTexture.active=active;camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(image!=null)Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
        }
        private static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException(reason);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
    }
}
