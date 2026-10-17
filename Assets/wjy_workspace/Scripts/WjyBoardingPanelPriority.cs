using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ZxhMuseum;

namespace Wjy.Museum
{
    // Scene-local adapter: shared boarding/UI scripts and shared rig assets stay unchanged.
    [DisallowMultipleComponent, DefaultExecutionOrder(10000)]
    public sealed class WjyBoardingPanelPriority : MonoBehaviour
    {
        private const int UiLayer = 5; // Unity's built-in UI layer; no ProjectSettings changes.
        private VensailCabinEntry entry;
        private WorldPanel panel;
        private readonly Dictionary<XRRayInteractor, LayerMask> savedMasks = new Dictionary<XRRayInteractor, LayerMask>();
        private readonly Dictionary<GameObject, int> savedLayers = new Dictionary<GameObject, int>();

        private void Awake() => entry = GetComponent<VensailCabinEntry>();

        private void LateUpdate()
        {
            var session = MuseumSession.Instance;
            if (!entry || !entry.ConfirmationOpen || !session || session.gameObject.scene != gameObject.scene)
            {
                Restore();
                return;
            }

            if (!panel || !panel.gameObject.activeInHierarchy)
            {
                Restore();
                foreach (var candidate in session.GetComponentsInChildren<WorldPanel>(true))
                    if (candidate.name == "ZXH_UI_Board the ship?" && candidate.gameObject.activeInHierarchy)
                    { panel = candidate; break; }
            }
            if (!panel || !session.Origin) return;

            // WorldPanel buttons are physical XRI targets, not GraphicRaycaster targets.
            foreach (var button in panel.GetComponentsInChildren<BoatTriggerAction>(true))
            {
                var target = button.gameObject;
                if (!savedLayers.ContainsKey(target)) savedLayers.Add(target, target.layer);
                target.layer = UiLayer;
            }
            foreach (var ray in session.Origin.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (!savedMasks.ContainsKey(ray)) savedMasks.Add(ray, ray.raycastMask);
                // Do not disable the vessel's colliders: only omit it from menu raycasts.
                ray.raycastMask = 1 << UiLayer;
            }
        }

        private void Restore()
        {
            foreach (var saved in savedMasks)
                if (saved.Key) saved.Key.raycastMask = saved.Value;
            savedMasks.Clear();
            foreach (var saved in savedLayers)
                if (saved.Key) saved.Key.layer = saved.Value;
            savedLayers.Clear();
            panel = null;
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
