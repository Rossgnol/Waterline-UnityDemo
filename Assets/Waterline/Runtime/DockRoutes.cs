using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class DockRoutes : MonoBehaviour
    {
        public DockSession session;
        public Transform player;
        public GameObject westFloodGate;
        public GameObject eastFloodGate;
        public bool LowerVisited { get; private set; }
        public bool BridgeUsedAfterFlood { get; private set; }
        public bool SouthUsedAfterFlood { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool LowerOpen { get { return session.State.Phase == DockPhase.Dry; } }

        public void ResetAttemptRoutes() { BridgeUsedAfterFlood=false;SouthUsedAfterFlood=false; }

        private void Update()
        {
            if (session.State == null) return;
            westFloodGate.SetActive(!LowerOpen);
            eastFloodGate.SetActive(!LowerOpen);
            if (session.InputBlocked) return;
            ElapsedSeconds += Time.deltaTime;
            Vector3 p = session.WorldToDock(player.position);
            if (p.y < -2.7f) LowerVisited = true;
            if (session.State.Phase != DockPhase.Dry)
            {
                if (Mathf.Abs(p.x) < 3 && p.z > 2.5f && p.z < 4.5f && p.y > -0.3f) BridgeUsedAfterFlood = true;
                if (Mathf.Abs(p.x) < 3 && p.z < -4.5f) SouthUsedAfterFlood = true;
            }
        }
    }
}
