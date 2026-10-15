using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

namespace ZxhMuseum
{
    // Same hover + Activate gesture as vensail's ship_museum, without remapping the rig.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class BoatTriggerAction : MonoBehaviour
    {
        public Action Pressed;
        private XRSimpleInteractable target;
        private int pressedFrame = -1;
        private void Awake() => target = GetComponent<XRSimpleInteractable>();
        public void Press()
        {
            if (!isActiveAndEnabled || pressedFrame == Time.frameCount) return;
            pressedFrame = Time.frameCount;
            Pressed?.Invoke();
        }
        private void Update()
        {
            ProcessTriggerInput();
            if (GetComponent<UnityEngine.UI.Button>() != null && !WorldPanel.HeadsetTracked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null &&
                Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 30) &&
                hit.collider.GetComponent<BoatTriggerAction>() == this) Press();
        }
        public void ProcessTriggerInput()
        {
            foreach (var interactor in target.interactorsHovering)
            {
                var controller = (interactor as XRBaseControllerInteractor)?.xrController as ActionBasedController;
                var action = controller != null ? controller.activateAction.action : null;
                if (action != null && action.WasPressedThisFrame()) { Press(); break; }
            }
        }
        public static void Bind(GameObject go, Action action)
        {
            var interactable = go.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = go.AddComponent<XRSimpleInteractable>();
            var origin = MuseumSession.Instance != null ? MuseumSession.Instance.Origin : null;
            if (origin != null)
                foreach (var interactor in origin.GetComponentsInChildren<XRBaseInteractor>())
                    if (interactor.isActiveAndEnabled && interactor.interactionManager != null)
                    { interactable.interactionManager = interactor.interactionManager; break; }
            go.AddComponent<BoatTriggerAction>().Pressed = action;
        }
    }
}
