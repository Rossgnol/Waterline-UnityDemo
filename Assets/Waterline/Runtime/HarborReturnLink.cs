using UnityEngine;
namespace Waterline
{
    // A visible connection between the two gates controlled by the existing north latch.
    public sealed class HarborReturnLink : MonoBehaviour
    {
        public AnnexSession annex;
        public Renderer[] routeMarkers;
        public Material closedMaterial,openMaterial;
        public TextMesh[] statusLabels;
        public bool ShowingOpen {get;private set;}
        private bool applied;
        private void LateUpdate(){Apply();}
        public void Apply()
        {
            if(annex==null || annex.State==null)return;
            bool open=annex.State.ObservationLoopOpen;
            if(applied && ShowingOpen==open)return;
            applied=true;ShowingOpen=open;
            foreach(var marker in routeMarkers)if(marker!=null)marker.sharedMaterial=open?openMaterial:closedMaterial;
            foreach(var label in statusLabels)if(label!=null)label.text=open?"RETURN LINK / OPEN":"RETURN LINK / NORTH LATCH";
        }
    }
}
