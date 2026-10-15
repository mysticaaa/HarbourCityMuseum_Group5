using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    // Replaces only ship_museum on our scene copy; preserves the teammate's hover + trigger gesture.
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class VensailCabinEntry : MonoBehaviour
    {
        private XRBaseInteractable target;
        private MuseumSession session;
        private WorldPanel confirmation;
        private float openedAt;
        public bool ConfirmationOpen => confirmation != null;
        private void Start()
        {
            target = GetComponent<XRBaseInteractable>();
            var origin = System.Array.Find(FindObjectsOfType<XROrigin>(), item => item.gameObject.scene == gameObject.scene);
            if (origin == null || origin.Camera == null)
            { Debug.LogError("[ZXH] Vensail XR Origin or camera missing.", this); enabled = false; return; }
            var root = new GameObject("ZXH_BoatSession");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            session = root.AddComponent<MuseumSession>();
            session.InitializeBoat(origin, transform);
            gameObject.AddComponent<DesktopObjectClick>().Selected = RequestConfirmation;
        }
        public void RequestConfirmation()
        {
            if (session == null || session.Busy || session.InCabin || ConfirmationOpen) return;
            var camera = session.Origin.Camera;
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = session.Origin.transform.forward;
            confirmation = WorldPanel.Create(session.transform, "Board the ship?",
                camera.transform.position + forward * 1.2f - Vector3.up * .1f, Quaternion.LookRotation(forward), 650);
            confirmation.Status.text = "Enter the deck, then walk into the cabin?";
            confirmation.Button("Enter ship", ConfirmEntry);
            confirmation.Button("Cancel", CancelEntry);
            openedAt = Time.unscaledTime;
        }
        public void ConfirmEntry()
        {
            // The trigger that opened the panel must not also accept it in the same frame.
            if (!ConfirmationOpen || Time.unscaledTime - openedAt < .25f) return;
            CancelEntry(); session.RequestEnter();
        }
        public void CancelEntry()
        {
            if (confirmation != null) { confirmation.gameObject.SetActive(false); Destroy(confirmation.gameObject); }
            confirmation = null;
        }
        private void OnDisable() => CancelEntry();
        private void Update()
        {
            if (session == null || session.InCabin || session.Busy || ConfirmationOpen) return;
            foreach (var interactor in target.interactorsHovering)
            {
                var controller = (interactor as XRBaseControllerInteractor)?.xrController as ActionBasedController;
                if (controller != null && controller.activateAction.action != null &&
                    controller.activateAction.action.WasPressedThisFrame())
                { RequestConfirmation(); break; }
            }
        }
    }
}
