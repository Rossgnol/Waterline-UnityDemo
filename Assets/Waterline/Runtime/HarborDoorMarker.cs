using UnityEngine;
namespace Waterline
{
    public sealed class HarborDoorMarker : MonoBehaviour
    {
        public HarborLayout layout;
        public int index;
        public string Description {get {return layout.doors[index].hint;}}
        public void Use(){layout.dock.Announce(Description);}
    }
}
