using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static class HiddenSignRepair
    {
        public static void Apply()
        {
            TidalRouteBuilder.Open();
            var h=Object.FindFirstObjectByType<HarborLayout>();
            var physics=PhysicsSignature();var layout=EditorJsonUtility.ToJson(h);
            var labels=Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            var lower=labels.Where(t=>(t.text=="OPTIONAL / SUPPLIES" || t.text=="DRAIN SERVICE") && Mathf.Abs(t.transform.position.z-5.36f)<.3f).ToArray();
            Require(lower.Length==4,"Expected four lower portal faces");
            foreach(var t in lower)
            {
                var p=t.transform.position;
                var lintel=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Single(c=>c.name=="Lower portal lintel" && Mathf.Abs(c.bounds.center.x-p.x)<.01f);
                p.z=p.z<lintel.bounds.center.z?lintel.bounds.min.z-.02f:lintel.bounds.max.z+.02f;
                t.transform.position=p;
            }
            var shutter=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Single(c=>c.name=="Service shutter lintel");
            var unlock=labels.Where(t=>t.text=="UNLOCK IN EAST ROOM").ToArray();
            Require(unlock.Length==2,"Expected two shutter instruction faces");
            foreach(var t in unlock)
            {
                bool west=Vector3.Dot(t.transform.forward,Vector3.right)>.9f;
                t.transform.position=new Vector3(west?shutter.bounds.min.x-.02f:shutter.bounds.max.x+.02f,shutter.bounds.center.y,shutter.bounds.center.z);
            }
            var oldSupports=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>(r.name=="Console sign backing" || r.name=="Console sign stem") && Mathf.Abs(r.transform.position.x-5.388f)<.02f && Mathf.Abs(r.transform.position.z+13.2f)<.02f).ToArray();
            Require(oldSupports.Length==2,"Expected original shutter sign backing and stem");
            foreach(var r in oldSupports)r.enabled=false;
            var quiet=labels.Single(t=>t.text=="QUIET WALK / CTRL");
            quiet.transform.position=new Vector3(-15.15f,-1.28f,5.09f);quiet.characterSize=.025f;
            var stairs=labels.Single(t=>t.text=="EAST STAIRS >");
            stairs.transform.position=new Vector3(13,-1.28f,5.09f);stairs.characterSize=.026f;
            Require(physics==PhysicsSignature() && layout==EditorJsonUtility.ToJson(h),"Collision or door data changed");
            EditorSceneManager.MarkSceneDirty(shutter.gameObject.scene);EditorSceneManager.SaveScene(shutter.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("TestResults/SignPlacement/after");
            var camera=Object.FindFirstObjectByType<FirstPersonController>().view;
            int i=0;
            foreach(var t in lower)
            {
                var target=t.GetComponent<Renderer>().bounds.center;var from=target-t.transform.forward*3;from.y=-1.85f;
                BoatPresentationBuilder.Shot(camera,"TestResults/SignPlacement/after/lower-"+(i++)+".png",from,target);
            }
            foreach(var t in new[]{quiet,stairs})
            {
                var target=t.GetComponent<Renderer>().bounds.center;var from=target-Vector3.forward*3;from.y=-1.85f;
                BoatPresentationBuilder.Shot(camera,"TestResults/SignPlacement/after/"+(t==quiet?"quiet":"stairs")+".png",from,target);
            }
            var door=h.dock.serviceDoor;var saved=door.position;
            try
            {
                foreach(bool open in new[]{false,true})
                {
                    door.position=saved+(open?Vector3.up*3.2f:Vector3.zero);Physics.SyncTransforms();i=0;
                    foreach(var t in unlock)
                    {
                        var target=t.GetComponent<Renderer>().bounds.center;var from=target-t.transform.forward*3;from.y=1.65f;
                        BoatPresentationBuilder.Shot(camera,"TestResults/SignPlacement/after/shutter-"+(open?"open":"closed")+"-"+(i++)+".png",from,target);
                    }
                }
            }
            finally {door.position=saved;Physics.SyncTransforms();}
            Debug.Log("[Waterline] HIDDEN SIGN REPAIR SAVED: four lower faces outside lintels, two shutter instructions on fixed lintel, two floating route hints placed on adjacent walls, two obsolete sign support renderers hidden; original collision and HarborLayout unchanged. Open-shutter screenshots are diagnostic staging only.");
        }
        public static void ApplyAndAudit(){Apply();SignPlacementAudit.Run();}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private static string PhysicsSignature()=>string.Join("\n",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.localToWorldMatrix.ToString("F5")));
    }
}
