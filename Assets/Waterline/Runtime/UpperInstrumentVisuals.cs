using UnityEngine;

namespace Waterline
{
    // Presentation reads the existing puzzle state; it never changes inventory or dial values.
    public sealed class UpperInstrumentVisuals : MonoBehaviour
    {
        public AnnexSession annex;
        public Transform[] signalWheels;
        public GameObject installedPlate;
        private void LateUpdate(){Apply();}
        public void Apply()
        {
            if(annex==null || annex.State==null)return;
            var s=annex.State;
            signalWheels[0].localRotation=Quaternion.Euler(0,-s.Harbor*360f/7,0);
            signalWheels[1].localRotation=Quaternion.Euler(0,-s.Dock*360f/7,0);
            signalWheels[2].localRotation=Quaternion.Euler(0,-s.Sea*360f/7,0);
            installedPlate.SetActive(s.GaugeAligned);
        }
    }
}
