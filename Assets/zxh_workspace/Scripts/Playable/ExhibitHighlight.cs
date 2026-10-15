using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class ExhibitHighlight : MonoBehaviour
    {
        private XRBaseInteractable target;
        private Renderer[] renderers;
        private MaterialPropertyBlock[] original;
        private void Start()
        {
            target = GetComponent<XRBaseInteractable>(); renderers = GetComponentsInChildren<Renderer>();
            original = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { original[i] = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(original[i]); }
            target.firstHoverEntered.AddListener(Enter); target.lastHoverExited.AddListener(Exit);
        }
        private void Enter(HoverEnterEventArgs args)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var block = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(block);
                block.SetColor("_Color", new Color(1, 0.8f, 0.25f)); renderers[i].SetPropertyBlock(block);
            }
        }
        private void Exit(HoverExitEventArgs args)
        { for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(original[i]); }
        private void OnDestroy()
        { if (target != null) { target.firstHoverEntered.RemoveListener(Enter); target.lastHoverExited.RemoveListener(Exit); } }
    }
}
