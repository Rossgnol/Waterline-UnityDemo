using System.Collections.Generic;
using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public enum ThreatMode { Patrol, Investigate, Chase, Search, Evacuate }

    [RequireComponent(typeof(CharacterController))]
    public sealed class ThreatEncounter : MonoBehaviour
    {
        public DockSession session;
        public FirstPersonController player;
        public ThreatGraph graph;
        public ThreatTuning tuning;
        public Transform body;
        public Transform[] limbs;
        public string displayName="拖链工";
        public bool managesDockFlood=true;
        public bool areaLimited;
        public bool Available=true;
        public Vector2 areaMin, areaMax;
        public ThreatMode Mode { get; private set; }
        public bool FloodRequested { get; private set; }
        public bool EvacuationComplete { get; private set; }
        public float Awareness { get; private set; }
        public float NoiseRadius { get; private set; }
        public GroundKind Ground { get; private set; }
        public MotionKind Motion { get; private set; }
        public int TargetNode { get; private set; } = -1;
        public int Attempts { get; private set; } = 1;
        public int StepSequence { get; private set; }
        public bool HasCheckpoint { get { return session.HasCheckpoint; } }
        public bool InSafeArea { get { return Protected(player.transform.position); } }
        public float CaptureProgress { get { return windup/Mathf.Max(.1f,tuning.captureWindup); } }
        private CharacterController controller;
        private List<int> path=new List<int>();
        private int pathIndex, patrolIndex;
        private float verticalSpeed, grace, lostSight, searchTime, windup, stride, noiseAge, chainBeat, gait;
        private Vector3 lastSeen, previousPlayer, lastPosition;
        private AudioSource chainSource, stepSource, alarmSource;
        private AudioClip chainClip, stepClip, metalClip, alarmClip;
        private ThreatEncounter[] neighbours;

        private void Awake()
        {
            controller=GetComponent<CharacterController>();
            chainSource=gameObject.AddComponent<AudioSource>(); chainSource.spatialBlend=1;chainSource.minDistance=2;chainSource.maxDistance=18;
            chainSource.rolloffMode=AudioRolloffMode.Linear;chainSource.volume=.32f;
            stepSource=player.gameObject.AddComponent<AudioSource>();stepSource.volume=.24f;
            alarmSource=session.gameObject.AddComponent<AudioSource>();alarmSource.volume=.28f;
            chainClip=DockAudio.Pulse("Chain drag",.35f,160,true);
            stepClip=DockAudio.Pulse("Soft footstep",.15f,85,false);
            metalClip=DockAudio.Pulse("Metal footstep",.25f,360,true);
            alarmClip=DockAudio.Pulse("Dock alarm",.6f,540,true);
        }

        private void Start() {neighbours=FindObjectsByType<ThreatEncounter>(FindObjectsSortMode.None); ResetEncounter(); }
        private void Update() { Advance(Time.deltaTime); }

        public static bool IsSafe(Vector3 p)
        {
            if(HarborLayout.Active!=null)
            {
                if(p.z>=6)return HarborLayout.Active.SafeAt(p);
                p=HarborLayout.Active.dock.WorldToDock(p);
            }
            if(ConnectedLayout.Active!=null && p.z>5.5f)return ConnectedLayout.Active.SafeAt(p);
            if(p.z<=5.5f)return p.x < -5.15f && p.z > -4.5f;
            return (p.x< -5 && p.z<11) || (p.x>5 && p.z<12);
        }
        private bool InArea(Vector3 p) {return !areaLimited || (p.x>=areaMin.x && p.x<=areaMax.x && p.z>=areaMin.y && p.z<=areaMax.y);}
        private bool Protected(Vector3 p) {return IsSafe(p) || !InArea(p);}
        public static GroundKind GroundAt(Vector3 p)
        {
            if(HarborLayout.Active!=null)
            {
                if(p.z>=6)return HarborLayout.Active.GroundAt(p);
                p=HarborLayout.Active.dock.WorldToDock(p);
            }
            if(ConnectedLayout.Active!=null && p.z>5.5f)return ConnectedLayout.Active.GroundAt(p);
            if(p.z>5.5f)return (p.x>=1 && p.x<=5) || p.z>=18?GroundKind.Metal:GroundKind.Concrete;
            if(p.y>-.5f) return p.z < -4.5f ? GroundKind.Rubber : Mathf.Abs(p.x)<5 && p.z>2.5f ? GroundKind.Metal : GroundKind.Concrete;
            return p.z>2.9f && Mathf.Abs(p.x)<6 ? GroundKind.Metal : p.z< -2.9f || p.x< -5 ? GroundKind.Rubber : GroundKind.Concrete;
        }

        public void ObserveMotion(bool running, bool quiet)
        {
            Vector3 p=player.transform.position;
            Vector3 delta=p-previousPlayer;delta.y=0;previousPlayer=p;
            Ground=GroundAt(p);
            Motion=delta.magnitude<.002f ? MotionKind.Still : quiet ? MotionKind.Quiet : running ? MotionKind.Run : MotionKind.Walk;
            if(delta.magnitude>1 || Motion==MotionKind.Still)return;
            stride+=delta.magnitude;
            if(stride<(running?1.35f:.8f))return;
            stride=0;NoiseRadius=NoiseRules.Radius(Ground,Motion);noiseAge=1.2f;StepSequence++;
            stepSource.PlayOneShot(Ground==GroundKind.Metal?metalClip:stepClip,quiet?.35f:running?1:.65f);
            HearNoise(p,NoiseRadius);
        }

        public bool HearNoise(Vector3 source, float radius)
        {
            if(!Available || session.InputBlocked || grace>0 || Mode==ThreatMode.Evacuate || Mode==ThreatMode.Chase || radius<=0 ||
                !InArea(source) || Mathf.Abs(source.y-transform.position.y)>1.3f)return false;
            bool clear=ClearLine(transform.position+Vector3.up*1.3f,source+Vector3.up);
            float effective=radius*(clear?1:tuning.obstructedSoundMultiplier);
            if(Vector3.Distance(transform.position,source)>effective)return false;
            lastSeen=source;Mode=ThreatMode.Investigate;searchTime=0;
            SetTarget(graph.Closest(source,EvacuationComplete?1:-1));
            return true;
        }

        public bool CanSeePlayer()
        {
            if(!Available)return false;
            Vector3 offset=player.transform.position-transform.position;
            float sight=HarborLayout.Active!=null?HarborLayout.Active.SightMultiplier(player.transform.position):1;
            if(Protected(player.transform.position) || Mathf.Abs(offset.y)>1.3f || offset.magnitude>tuning.viewDistance*sight)return false;
            Vector3 flat=offset;flat.y=0;
            if(Mode!=ThreatMode.Chase && Vector3.Angle(transform.forward,flat)>tuning.fieldOfView*.5f)return false;
            return ClearLine(transform.position+Vector3.up*1.55f,player.transform.position+Vector3.up*1.35f);
        }

        private bool ClearLine(Vector3 from, Vector3 to)
        {
            Vector3 delta=to-from;
            foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player.transform))continue;
                return false;
            }
            return true;
        }

        public void RequestFlood()
        {
            if(FloodRequested)return;
            FloodRequested=true;Mode=ThreatMode.Evacuate;Awareness=0;windup=0;
            SetTarget(graph.upperArrival,true);
            alarmSource.PlayOneShot(alarmClip);
            session.Announce("警报响起：拖链工正沿东梯上楼。请留在上层，疏散完成后自动注水。" );
        }

        public void Advance(float dt)
        {
            if(!Available || session.State==null || dt<=0)return;
            if(session.InputBlocked) {chainSource.Pause();stepSource.Pause();alarmSource.Pause();return;}
            chainSource.UnPause();stepSource.UnPause();alarmSource.UnPause();
            grace=Mathf.Max(0,grace-dt);noiseAge=Mathf.Max(0,noiseAge-dt);
            if(noiseAge==0)NoiseRadius=0;
            if(managesDockFlood && !session.HasCheckpoint && player.transform.position.y>-.2f && session.WorldToDock(player.transform.position).x>7 &&
                (session.State.HasPin || session.State.PinInstalled))
                session.SaveCheckpoint(session.DockToWorld(new Vector3(10,.08f,-3.8f)));
            if(FloodRequested && EvacuationComplete && session.State.Phase==DockPhase.Dry && session.PlayerSafeForFlood)
                session.Perform(DockAction.StartFlood);

            if(Mode==ThreatMode.Evacuate)
            {
                // The alarm overrides pursuit, but standing in the narrow stair cannot stop the encounter forever.
                bool obstructing = !InSafeArea && Vector3.Distance(transform.position,player.transform.position)<tuning.captureDistance &&
                    ClearLine(transform.position+Vector3.up*1.3f,player.transform.position+Vector3.up*1.3f);
                windup=obstructing?windup+dt:0;
                if(windup>=tuning.captureWindup){session.CapturePlayer();session.Announce("被"+displayName+"拦截。按 R 从检查点重试。");chainSource.Stop();return;}
                Follow(tuning.evacuationSpeed,dt);
                if(Arrived())
                {
                    EvacuationComplete=true;Mode=ThreatMode.Patrol;patrolIndex=0;TargetNode=-1;path.Clear();grace=2;
                    session.Announce(session.Planner!=null?"疏散完成。回西侧水控室开始注水，检修桥将收回。"+session.Planner.FloodRoute:session.PlayerSafeForFlood ? "拖链工已到上层，开始注水。可轻步走桥，或从南栈道绕行。" :
                        "疏散已完成。请回到上层房间，联锁确认安全后自动注水。");
                }
                Animate(dt);return;
            }

            bool visible=grace<=0 && CanSeePlayer();
            if(visible)
            {
                Awareness=Mathf.Min(1,Awareness+dt/Mathf.Max(.1f,tuning.recognitionSeconds));
                if(Awareness>=1) {Mode=ThreatMode.Chase;lastSeen=player.transform.position;lostSight=0;}
                else
                {
                    Vector3 facing=player.transform.position-transform.position;facing.y=0;
                    if(facing.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(facing),360*dt);
                    Animate(dt);return;
                }
            }
            else if(Mode!=ThreatMode.Chase)Awareness=Mathf.Max(0,Awareness-dt*.55f);

            if(Mode==ThreatMode.Chase)
            {
                if(visible){lastSeen=player.transform.position;lostSight=0;}
                else lostSight+=dt;
                float distance=Vector3.Distance(transform.position,player.transform.position);
                if(visible && distance<tuning.captureDistance)
                {
                    windup+=dt;
                    if(windup>=tuning.captureWindup){session.CapturePlayer();session.Announce("被"+displayName+"拦截。按 R 从检查点重试。");chainSource.Stop();return;}
                }
                else windup=0;
                if(lostSight>=tuning.loseSightSeconds || Protected(player.transform.position))
                {
                    Mode=ThreatMode.Search;Awareness=0;searchTime=0;windup=0;
                    SetTarget(graph.Closest(lastSeen,EvacuationComplete?1:-1));
                }
                else if(visible && !Protected(player.transform.position) && ClearWalkToPlayer())
                    MoveTowards(player.transform.position,tuning.chaseSpeed,dt);
                else {SetTarget(graph.Closest(lastSeen,EvacuationComplete?1:-1));Follow(tuning.chaseSpeed,dt);}
            }
            if(Mode==ThreatMode.Patrol)
            {
                int[] patrol=EvacuationComplete?graph.upperPatrol:graph.lowerPatrol;
                if(TargetNode<0 || Arrived()){patrolIndex=(patrolIndex+1)%patrol.Length;SetTarget(patrol[patrolIndex]);}
                Follow(tuning.patrolSpeed,dt);
            }
            else if(Mode==ThreatMode.Investigate || Mode==ThreatMode.Search)
            {
                Follow(tuning.investigateSpeed,dt);
                if(Arrived())
                {
                    Mode=ThreatMode.Search;searchTime+=dt;
                    transform.Rotate(0,55*dt,0);
                    if(searchTime>=tuning.searchSeconds){Mode=ThreatMode.Patrol;TargetNode=-1;path.Clear();}
                }
            }
            Animate(dt);
        }

        private void SetTarget(int node, bool allLevels=false)
        {
            if(node==TargetNode && path.Count>0)return;
            TargetNode=node;pathIndex=0;
            int level=allLevels?2:EvacuationComplete?1:-1;
            path=graph.Path(graph.Closest(transform.position,level),node,level);
            // A sound can arrive midway along an edge. Enter the remaining part of that
            // same edge instead of walking back to its nearest endpoint first.
            if(!allLevels && (Mode==ThreatMode.Investigate || Mode==ThreatMode.Search) && path.Count>1 &&
                CanEnterEdge(graph.nodes[path[0]].position,graph.nodes[path[1]].position))pathIndex=1;
        }

        private bool CanEnterEdge(Vector3 from,Vector3 to)
        {
            // Keep stair traversal and turns on the authored graph. Only a flat edge
            // that already contains the body can be entered partway through.
            if(Mathf.Abs(from.y-to.y)>.15f || Mathf.Abs(transform.position.y-from.y)>.25f)return false;
            Vector3 edge=to-from;edge.y=0;
            if(edge.sqrMagnitude<.01f)return false;
            Vector3 offset=transform.position-from;offset.y=0;
            float t=Vector3.Dot(offset,edge)/edge.sqrMagnitude;
            if(t<=.05f || t>=1 || (offset-edge*t).sqrMagnitude>.25f)return false;
            Vector3 delta=to-transform.position;delta.y=0;
            foreach(var hit in Physics.CapsuleCastAll(transform.position+Vector3.up*.45f,
                transform.position+Vector3.up*1.45f,controller.radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform))return false;
            return true;
        }

        private bool ClearWalkToPlayer()
        {
            Vector3 delta=player.transform.position-transform.position;delta.y=0;
            if(delta.sqrMagnitude<.01f)return true;
            // A visible target may be farther from every patrol node in a large room.
            // Sweep the body before steering directly, so low props still block movement.
            Vector3 bottom=transform.position+Vector3.up*.45f,top=transform.position+Vector3.up*1.45f;
            foreach(var hit in Physics.CapsuleCastAll(bottom,top,controller.radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(player.transform))return false;
            return true;
        }

        private bool Arrived()
        { return TargetNode>=0 && Vector3.Distance(transform.position,graph.nodes[TargetNode].position)<.35f; }

        private void Follow(float speed,float dt)
        {
            if(path.Count==0)return;
            while(pathIndex<path.Count-1 && Vector3.Distance(transform.position,graph.nodes[path[pathIndex]].position)<.3f)pathIndex++;
            MoveTowards(graph.nodes[path[pathIndex]].position,speed,dt);
        }

        private void MoveTowards(Vector3 destination,float speed,float dt)
        {
            Vector3 direction=destination-transform.position;direction.y=0;
            if(neighbours!=null && direction.magnitude>.1f)
            {
                foreach(var other in neighbours)
                {
                    if(other==null || other==this || !other.Available)continue;
                    var offset=other.transform.position-transform.position;
                    if(Mathf.Abs(offset.y)<1 && offset.magnitude<1.15f && Vector3.Dot(offset.normalized,direction.normalized)>.25f)
                    {direction=direction.normalized+Vector3.Cross(Vector3.up,direction.normalized)*1.1f;break;}
                }
            }
            if(direction.magnitude>.08f)
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),360*dt);
            if(controller.isGrounded && verticalSpeed<0)verticalSpeed=-2;
            verticalSpeed=Mathf.Max(-15,verticalSpeed+Physics.gravity.y*dt);
            Vector3 step=Vector3.ClampMagnitude(direction,speed*dt);step.y=verticalSpeed*dt;
            controller.Move(step);
        }

        private void Animate(float dt)
        {
            float travel=Vector3.Distance(transform.position,lastPosition);lastPosition=transform.position;
            if(travel>.005f)
            {
                gait+=travel*7;chainBeat-=dt;
                if(chainBeat<=0){chainSource.PlayOneShot(chainClip);chainBeat=.8f;}
                if(body!=null)body.localPosition=new Vector3(0,Mathf.Sin(gait*2)*.025f,0);
                if(limbs!=null)for(int i=0;i<limbs.Length;i++)limbs[i].localRotation=Quaternion.Euler(Mathf.Sin(gait+(i%2)*Mathf.PI)*18,0,0);
            }
        }

        public void ResetEncounter()
        {
            controller.enabled=false;transform.position=graph.nodes[graph.lowerPatrol[0]].position;transform.rotation=Quaternion.Euler(0,-90,0);controller.enabled=true;
            Mode=ThreatMode.Patrol;FloodRequested=false;EvacuationComplete=false;Awareness=0;windup=0;lostSight=0;searchTime=0;
            grace=tuning.retryGraceSeconds;TargetNode=-1;path.Clear();pathIndex=0;patrolIndex=0;verticalSpeed=0;stride=0;
            previousPlayer=player.transform.position;lastPosition=transform.position;NoiseRadius=0;
            chainSource.Stop();stepSource.Stop();alarmSource.Stop();
        }

        public void CountRetry() {Attempts++;ResetEncounter();}
        private void OnDestroy()
        {
            if(chainClip!=null)Destroy(chainClip);if(stepClip!=null)Destroy(stepClip);
            if(metalClip!=null)Destroy(metalClip);if(alarmClip!=null)Destroy(alarmClip);
        }
    }
}
