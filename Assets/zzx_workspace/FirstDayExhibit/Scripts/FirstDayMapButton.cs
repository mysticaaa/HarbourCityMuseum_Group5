using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class FirstDayMapButton : MonoBehaviour
    {
        public FirstDayJourney journey;
        public int berth; // 0 = restart; 1/2/3 = map locations.
        private XRSimpleInteractable interactable;
        private void Awake() { interactable = GetComponent<XRSimpleInteractable>(); }
        private void Update()
        {
            // Same hover + controller activateAction pattern as the existing VideoButton.
            // Read the existing action; never enable/disable it or create a new binding.
            foreach (var hovering in interactable.interactorsHovering)
            {
                // Use XRI's per-interactor hover list so one exiting interactor cannot remove another.
                if (!(hovering is XRBaseControllerInteractor ray) ||
                    !(ray.xrController is ActionBasedController controller) ||
                    controller.activateAction.action == null ||
                    !controller.activateAction.action.WasPressedThisFrame()) continue;
                if (journey != null)
                {
                    if (berth == 0) journey.ResetJourney();
                    else journey.Choose(berth);
                }
                break;
            }
        }
    }
}
