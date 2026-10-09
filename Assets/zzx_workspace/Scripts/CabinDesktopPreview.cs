using UnityEngine;

namespace ZzxWorkspace
{
    public class CabinDesktopPreview : MonoBehaviour
    {
        public Camera previewCamera;
        private InteractableObject hovered;
        private Vector2 previousPointer;
        private bool dragging;
        private CabinNavigationSimulation navigation;
        private void Awake()
        {
            navigation = GetComponent<CabinNavigationSimulation>();
            if (navigation == null) navigation = gameObject.AddComponent<CabinNavigationSimulation>();
            navigation.requireEngineControl = GetComponentInChildren<CabinExhibit>() != null &&
                System.Array.Exists(GetComponentsInChildren<CabinExhibit>(), e => e.kind == CabinExhibit.ExhibitKind.EngineControl);
            foreach (var exhibit in GetComponentsInChildren<CabinExhibit>())
            {
                exhibit.inspectionCamera = previewCamera;
                exhibit.navigation = navigation;
            }
        }
        private void Update()
        {
            if (previewCamera == null) return;
            if (Input.GetKeyDown(KeyCode.R))
                foreach (var exhibit in GetComponentsInChildren<CabinExhibit>()) exhibit.ResetExhibit();
            if (Input.GetKeyDown(KeyCode.Escape) && CabinExhibit.OpenExhibit != null) CabinExhibit.OpenExhibit.Close();
            var open = CabinExhibit.OpenExhibit;
            Vector2 pointer = Input.mousePosition;
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (CabinExhibit.PointerOverPanel(pointer))
            {
                dragging = false;
                previousPointer = pointer;
                ClearHover();
                return;
            }
            if (open != null)
            {
                if (Input.GetMouseButtonDown(0)) { dragging = true; previousPointer = pointer; }
                if (dragging && Input.GetMouseButton(0))
                {
                    Vector2 delta = pointer - previousPointer;
                    if (open.IsOperating && open.kind == CabinExhibit.ExhibitKind.Wheel) open.DragWheel(delta.x);
                    else open.DragView(delta);
                }
                previousPointer = pointer;
                return;
            }
            dragging = false;
            RaycastHit hit;
            InteractableObject next = null;
            if (Physics.Raycast(previewCamera.ScreenPointToRay(pointer), out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                next = hit.collider.GetComponentInParent<InteractableObject>();
            if (next != hovered)
            {
                ClearHover();
                hovered = next;
                if (hovered != null) hovered.OnHoverEnter();
            }
            if (Input.GetMouseButtonDown(0) && hovered != null) hovered.OnSelect();
        }
        private void ClearHover() { if (hovered != null) hovered.OnHoverExit(); hovered = null; }
        private void OnDisable()
        {
            ClearHover();
            dragging = false;
            if (CabinExhibit.OpenExhibit != null) CabinExhibit.OpenExhibit.Close();
        }
    }
}
