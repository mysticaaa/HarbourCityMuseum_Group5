using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class HarbourPrototypeButton : MonoBehaviour
    {
        public Action action;
        public Renderer face;
        public Material normal, highlight;
        public bool suggested;
        private XRSimpleInteractable interactable;
        private int lastFrame=-1;
        private void Awake()
        {
            interactable=GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelect);
        }
        private void OnSelect(SelectEnterEventArgs args) => Invoke();
        public void Invoke() { if(lastFrame==Time.frameCount) return; lastFrame=Time.frameCount; action?.Invoke(); }
        private void Update()
        {
            if(face) face.sharedMaterial=(interactable.isHovered || suggested) ? highlight : normal;
            foreach(var hover in interactable.interactorsHovering)
                if(hover is XRBaseControllerInteractor ray && ray.xrController is ActionBasedController controller &&
                   controller.activateAction.action != null && controller.activateAction.action.WasPressedThisFrame()) { Invoke(); break; }
        }
        private void OnDestroy() { if(interactable) interactable.selectEntered.RemoveListener(OnSelect); }
    }
}
