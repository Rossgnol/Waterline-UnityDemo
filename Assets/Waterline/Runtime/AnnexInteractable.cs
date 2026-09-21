using UnityEngine;
using Waterline.Core;
namespace Waterline
{
    public sealed class AnnexInteractable : MonoBehaviour
    {
        public AnnexSession annex;
        public AnnexAction action;
        public string displayName, description;
        public bool Use() {return annex.Perform(action);}
    }
}
