using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ShipDetailInspection : MonoBehaviour
{
    [SerializeField, Range(1.1f, 3f)] private float magnification = 2f;
    [SerializeField] private float returnAfterSeconds = 12f;
    private XRSimpleInteractable interactable;
    private ExhibitAutoRotation rotation;
    private Vector3 homePosition, homeScale, focusPoint;
    private Quaternion homeRotation;
    private float remaining;
    private bool inspecting;
    private GameObject marker;
    private Vector3 enlargedPosition, enlargedScale;
    private float zoomTime;

    private void Awake()
    {
        rotation = GetComponentInParent<ExhibitAutoRotation>();
        // Mesh colliders make the ray hit the actual detail, not an oversized bounding box.
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            var collider = filter.gameObject.GetComponent<MeshCollider>();
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
        }
        interactable = gameObject.AddComponent<XRSimpleInteractable>();
        interactable.colliders.Clear();
        interactable.colliders.AddRange(GetComponentsInChildren<Collider>());
        interactable.selectEntered.AddListener(OnSelect);
        ExhibitInspectionHint.Create(transform, "Ship: click a detail to enlarge; click again to reset");
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        if (inspecting) { Restore(); return; }
        var ray = args.interactorObject as XRRayInteractor;
        if (ray == null || !ray.TryGetCurrent3DRaycastHit(out RaycastHit hit)) return;
        if (InspectSurface(hit)) ray.SendHapticImpulse(0.15f, 0.05f);
    }

    public bool InspectSurface(RaycastHit hit)
    {
        if (inspecting || hit.transform == null) return false;
        if (!hit.transform.IsChildOf(transform) && hit.transform != transform) return false;
        homePosition = transform.localPosition;
        homeRotation = transform.localRotation;
        homeScale = transform.localScale;
        focusPoint = transform.InverseTransformPoint(hit.point);
        Vector3 displayCenter = ExhibitInspectionHint.GetBounds(transform).center;
        if (rotation != null) rotation.Paused = true;
        transform.localScale = homeScale * magnification;
        // Bring the clicked region to the exhibit center without moving the visitor's camera.
        transform.position += displayCenter - transform.TransformPoint(focusPoint);
        enlargedPosition = transform.localPosition;
        enlargedScale = transform.localScale;
        transform.localPosition = homePosition;
        transform.localScale = homeScale;
        zoomTime = 0;
        marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ZXH_SelectedDetail";
        marker.transform.position = transform.TransformPoint(focusPoint);
        marker.transform.localScale = Vector3.one * 0.018f;
        var collider = marker.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        marker.transform.SetParent(transform, true);
        marker.GetComponent<Renderer>().material.color = Color.yellow;
        remaining = returnAfterSeconds;
        inspecting = true;
        return true;
    }

    private void Update()
    {
        if (inspecting)
        {
            zoomTime = Mathf.Min(1f, zoomTime + Time.deltaTime / 0.35f);
            float eased = Mathf.SmoothStep(0, 1, zoomTime);
            transform.localPosition = Vector3.Lerp(homePosition, enlargedPosition, eased);
            transform.localScale = Vector3.Lerp(homeScale, enlargedScale, eased);
        }
        if (inspecting && (remaining -= Time.deltaTime) <= 0) Restore();
    }

    public void Restore()
    {
        if (!inspecting) return;
        transform.localPosition = homePosition;
        transform.localRotation = homeRotation;
        transform.localScale = homeScale;
        if (marker != null)
        {
            Destroy(marker.GetComponent<Renderer>().material);
            Destroy(marker);
        }
        inspecting = false;
        if (rotation != null) rotation.Paused = false;
    }

    private void OnDisable() => Restore();
    private void OnDestroy()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(OnSelect);
    }
}
