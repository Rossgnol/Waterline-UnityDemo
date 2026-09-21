using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Waterline.Editor
{
    [InitializeOnLoad] public static class ConsoleVisualCheck
    {
        private const string Key="Waterline.ConsoleVisual";
        private static double start;
        static ConsoleVisualCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){ConsolePresentationBuilder.CaptureSaved();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            start=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Check;
        }
        private static void Check()
        {
            if(EditorApplication.timeSinceStartup<start)return;
            try
            {
                var layout=Object.FindFirstObjectByType<HarborLayout>();layout.enabled=false;layout.dock.Paused=true;
                var player=Object.FindFirstObjectByType<FirstPersonController>();player.enabled=false;
                foreach(var e in Object.FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None)){e.enabled=false;e.body.gameObject.SetActive(false);}
                var root=GameObject.Find(ConsolePresentationBuilder.RootName);
                Require(root!=null && root.GetComponentsInChildren<Collider>().Length==0,"Unexpected console collision");
                var labels=Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                foreach(var item in ConsolePresentationBuilder.Items())
                {
                    var head=item.GetComponent<Renderer>().bounds;string value=PrototypeBuilder.ControlLabel(item.action);
                    var pair=labels.Where(t=>t.text==value && (t.name.StartsWith("Sign ") || t.name=="Mounted sign / "+value)).ToArray();
                    Require(pair.Length==2,"Missing label pair "+value);
                    foreach(var text in pair)
                    {
                        Require(text.GetComponent<Renderer>().enabled && Mathf.Abs(text.transform.position.x-head.center.x)<.001f && Mathf.Abs(text.transform.position.y-head.max.y-.25f)<.001f && Mathf.Abs(text.transform.position.z-head.center.z-.12f)<.05f,"Label not mounted at its console: "+value);
                    }
                    player.view.transform.position=item.transform.position+new Vector3(0,.6f,-1.6f);
                    Physics.SyncTransforms();var delta=item.transform.position-player.view.transform.position;
                    Require(player.RaycastInteraction(delta.normalized,out var hit) && hit.collider.GetComponentInParent<DockInteractable>()==item,"Console interaction occluded: "+item.action);
                    Debug.Log("[Waterline] CONSOLE RAY PASS: "+item.action+" hits original interaction component; paired signs aligned.");
                }
                ConsolePresentationBuilder.Capture("runtime");
                Debug.Log("[Waterline] CONSOLE VISUAL PASS: five original interaction rays, ten mounted labels, no new collision, runtime captures saved. Not a full gameplay or performance test.");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
    }
}
