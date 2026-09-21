using UnityEngine;
using UnityEngine.InputSystem;

namespace Waterline
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public DockSession session;
        public Camera view;
        public DockInteractable Target { get; private set; }
        public AnnexInteractable AnnexTarget { get; private set; }
        public HarborDoorMarker DoorTarget { get; private set; }
        public float HoldProgress { get; private set; }
        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;
        private Vector3 spawn;
        private bool interactionConsumed;

        private void Awake() { controller = GetComponent<CharacterController>(); spawn = transform.position; }

        private void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || mouse == null || session == null) return;
            if(session.Caught) { if(keys.rKey.wasPressedThisFrame)session.RetryCheckpoint(); return; }
            if(session.annex!=null && session.annex.NoteOpen)
            {if(keys.eKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame)session.annex.NoteOpen=false;return;}
            if (!keys.eKey.isPressed) interactionConsumed = false;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (session.MapVisible) session.MapVisible = false;
                else if (session.DebugVisible) session.DebugVisible = false;
                else session.Paused = !session.Paused;
            }
            if (keys.mKey.wasPressedThisFrame && session.explorationLayout)
            { if(session.annex!=null && session.annex.Journal!=null && session.annex.Journal.Showing)session.annex.Journal.ShowMap();else session.MapVisible = !session.MapVisible; session.DebugVisible = false; }
            if(keys.jKey.wasPressedThisFrame && session.annex!=null && session.annex.Journal!=null && !session.Paused && !session.DebugVisible)
                session.annex.Journal.Toggle();
            if (keys.f1Key.wasPressedThisFrame)
            { session.DebugVisible = !session.DebugVisible; session.MapVisible = false; }
            if (session.InputBlocked) { Target = null; AnnexTarget=null; HoldProgress = 0; return; }
            if(keys.qKey.wasPressedThisFrame && session.annex!=null && session.annex.Supplies!=null)session.annex.Perform(Waterline.Core.AnnexAction.DeployDecoy);

            view.fieldOfView = session.tuning.fieldOfView;
            var delta = mouse.delta.ReadValue() * session.tuning.mouseSensitivity;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var input = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            float speed = keys.leftShiftKey.isPressed ? session.tuning.runSpeed : session.tuning.walkSpeed;
            bool quiet=session.threat!=null && keys.leftCtrlKey.isPressed;
            if(quiet)speed=session.threat.tuning.quietPlayerSpeed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var movement = (transform.right * input.x + transform.forward * input.y) * speed;
            controller.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
            if(session.threat!=null)
            {
                session.threat.ObserveMotion(keys.leftShiftKey.isPressed && !quiet,quiet);
                if(session.annex!=null)session.annex.BroadcastFootstep(session.threat.NoiseRadius,transform.position);
            }
            if (transform.position.y < -5f)
            {
                controller.enabled = false; transform.position = spawn; controller.enabled = true;
                verticalSpeed = 0;
            }

            RaycastHit hit;
            DockInteractable next = null;
            AnnexTarget=null;DoorTarget=null;
            if (RaycastInteraction(view.transform.forward, out hit))
            {next = hit.collider.GetComponentInParent<DockInteractable>();AnnexTarget=hit.collider.GetComponentInParent<AnnexInteractable>();DoorTarget=hit.collider.GetComponentInParent<HarborDoorMarker>();}
            if (next != Target) HoldProgress = 0;
            Target = next;
            if (Target == null)
            {if(AnnexTarget!=null && keys.eKey.wasPressedThisFrame)AnnexTarget.Use();else if(DoorTarget!=null && keys.eKey.wasPressedThisFrame)DoorTarget.Use();return;}
            if (Target.needsHold)
            {
                if (keys.eKey.isPressed && !interactionConsumed)
                {
                    HoldProgress += Time.deltaTime / Mathf.Max(0.1f, session.tuning.floodHoldSeconds);
                    if (HoldProgress >= 1) { Target.Use(); HoldProgress = 0; interactionConsumed = true; }
                }
                else HoldProgress = 0;
            }
            else if (keys.eKey.wasPressedThisFrame) Target.Use();
        }

        // A just-repositioned CharacterController can report a zero-distance self hit.
        // Ignore only this player's hierarchy; walls and enemies still block interaction.
        public bool RaycastInteraction(Vector3 direction, out RaycastHit nearest)
        {
            nearest=default;float distance=float.PositiveInfinity;bool found=false;
            foreach(var hit in Physics.RaycastAll(view.transform.position,direction,session.tuning.interactionDistance,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(transform) || hit.distance>=distance)continue;
                distance=hit.distance;nearest=hit;found=true;
            }
            return found;
        }

        public void ResetPose(Vector3 position, float yaw)
        {
            controller.enabled=false;transform.position=position;transform.rotation=Quaternion.Euler(0,yaw,0);controller.enabled=true;
            verticalSpeed=0;pitch=0;view.transform.localRotation=Quaternion.identity;Target=null;AnnexTarget=null;HoldProgress=0;interactionConsumed=true;
        }
    }
}
