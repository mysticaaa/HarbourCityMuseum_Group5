using UnityEngine;

namespace ZxhMuseum
{
    public sealed class InteractionRecord : MonoBehaviour
    {
        public string assetId;
        public string category;
        [TextArea] public string description;
        [TextArea] public string provenance = "Project-authored teaching reconstruction; not a historical replica.";
    }
}
