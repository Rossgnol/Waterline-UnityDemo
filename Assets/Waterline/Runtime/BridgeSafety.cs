using UnityEngine;

namespace Waterline
{
    public sealed class BridgeSafety : MonoBehaviour
    {
        public DockSession session;
        public GameObject west;
        public GameObject east;
        private void Update()
        {
            bool closed = session.bridge == null || session.bridge.localScale.x < 9.9f*session.dockScale.x;
            if (west != null) west.SetActive(closed);
            if (east != null) east.SetActive(closed);
        }
    }
}
