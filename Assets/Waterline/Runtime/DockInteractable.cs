using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class DockInteractable : MonoBehaviour
    {
        public DockAction action;
        public string displayName;
        [TextArea] public string description;
        public bool hideAfterSuccess;
        public bool needsHold;
        public DockSession session;

        public bool Use()
        {
            if (session == null || !session.Perform(action)) return false;
            if (hideAfterSuccess) gameObject.SetActive(false);
            return true;
        }
    }
}
