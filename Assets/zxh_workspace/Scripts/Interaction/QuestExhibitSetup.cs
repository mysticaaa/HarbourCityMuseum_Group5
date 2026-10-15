using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Scene references are explicit so other exhibits and cameras are not changed.
public class QuestExhibitSetup : MonoBehaviour
{
    public Transform helmet;
    public Transform ship;
    public Unity.XR.CoreUtils.XROrigin xrOrigin;

    private void Awake()
    {
        var origin = xrOrigin;
        if (origin == null)
        {
            Debug.LogError("[ZXH] Museum XR Origin is missing.", this);
            return;
        }
        origin.gameObject.SetActive(true);
        foreach (Camera camera in FindObjectsOfType<Camera>())
            if (!camera.transform.IsChildOf(origin.transform)) camera.gameObject.SetActive(false);
        foreach (DesktopMovementController desktop in FindObjectsOfType<DesktopMovementController>(true))
            desktop.enabled = false;
        var managers = FindObjectsOfType<XRInteractionManager>(true);
        XRInteractionManager manager = null;
        foreach (XRInteractionManager candidate in managers)
        {
            // A manager under a disabled duplicate Rig cannot process interaction events.
            if (!candidate.gameObject.activeInHierarchy || candidate.gameObject.scene != gameObject.scene)
                continue;
            manager = candidate;
            if (candidate.isActiveAndEnabled) break;
        }
        if (manager == null)
        {
            var managerObject = new GameObject("ZXH_XRInteractionManager");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject, gameObject.scene);
            manager = managerObject.AddComponent<XRInteractionManager>();
        }
        manager.enabled = true;
        foreach (XRBaseInteractor interactor in FindObjectsOfType<XRBaseInteractor>(true))
            interactor.interactionManager = manager;
        foreach (XRBaseInteractable interactable in FindObjectsOfType<XRBaseInteractable>(true))
            interactable.interactionManager = manager;
        foreach (XRInteractionManager other in managers)
            if (other != manager && other.gameObject.scene == gameObject.scene) other.enabled = false;
        foreach (XRRayInteractor ray in FindObjectsOfType<XRRayInteractor>())
        {
            var controller = ray.GetComponent<ActionBasedController>();
            if (controller != null && controller.activateAction.action != null)
            {
                controller.selectAction = controller.activateAction;
                controller.selectActionValue = controller.activateActionValue;
            }
            // Bring distant selected objects to the controller instead of holding at ray distance.
            ray.useForceGrab = true;
        }
        if (helmet != null)
            helmet.gameObject.AddComponent<HelmetHandInspection>();
        if (ship != null)
        {
            ship.gameObject.AddComponent<ShipDetailInspection>();
            ship.GetComponent<XRSimpleInteractable>().interactionManager = manager;
        }
    }
}
