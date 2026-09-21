using System;
using UnityEngine;
using Waterline.Core;
namespace Waterline
{
    [Serializable] public sealed class MapRoom
    {
        public Rect bounds;
        public string label;
        public bool safe;
        public GroundKind ground;
    }
    [Serializable] public sealed class MapDoor
    {
        public Vector2 position;
        public bool vertical;
        public GameObject blocker;
    }
    // The same authored regions drive the route map, safe rooms and footstep materials.
    public sealed class ConnectedLayout : MonoBehaviour
    {
        public static ConnectedLayout Active { get; private set; }
        public MapRoom[] rooms;
        public MapDoor[] doors;
        public GameObject serviceGate;
        public Transform bell;
        private AnnexSession annex;
        private AudioSource audioSource;
        private float bellDelay=-1, cooldown;
        private void Awake() { Active=this;annex=GetComponent<AnnexSession>(); }
        private void Start()
        {
            audioSource=bell.gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;
            audioSource.maxDistance=35;audioSource.volume=.6f;
            audioSource.clip=DockAudio.Pulse("Inspection bell",1.2f,720,true);
        }
        private void OnDestroy() { if(Active==this)Active=null;if(audioSource!=null && audioSource.clip!=null)Destroy(audioSource.clip); }
        public bool ScheduleBell()
        {
            if(cooldown>0){annex.dock.Announce("查验铃正在复位，请稍候再用。");return false;}
            bellDelay=3;cooldown=12;return true;
        }
        public void ResetTransient() {bellDelay=-1;cooldown=0;}
        private void Update()
        {
            serviceGate.SetActive(!annex.State.ServiceLoopOpen);
            if(annex.dock.InputBlocked)return;
            cooldown=Mathf.Max(0,cooldown-Time.deltaTime);
            if(bellDelay<0)return;
            bellDelay-=Time.deltaTime;
            if(bellDelay>0)return;
            bellDelay=-1;audioSource.Play();
            foreach(var guard in FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None))
                if(!guard.managesDockFlood)guard.HearNoise(bell.position,60);
            annex.dock.Announce("查验铃正在响，趁巡查者调查时绕行。");
        }
        public bool SafeAt(Vector3 p)
        {
            foreach(var room in rooms)if(room.safe && room.bounds.Contains(new Vector2(p.x,p.z)))return true;
            return false;
        }
        public GroundKind GroundAt(Vector3 p)
        {
            foreach(var room in rooms)if(room.bounds.Contains(new Vector2(p.x,p.z)))return room.ground;
            return GroundKind.Concrete;
        }
        public static float ExpandZ(float z) {return z<=6?z:6+(z-6)*1.6f;}
    }
}
