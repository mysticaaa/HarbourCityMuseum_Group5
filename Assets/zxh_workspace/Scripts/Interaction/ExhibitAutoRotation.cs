using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Rotates an exhibit upright without moving its display cabinet.</summary>
[DisallowMultipleComponent]
public class ExhibitAutoRotation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 15f;
    private Vector3 localCenter;
    private XRBaseInteractable interactable;
    public bool Paused { get; set; }

    private void Start()
    {
        interactable = GetComponentInChildren<XRBaseInteractable>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            localCenter = transform.InverseTransformPoint(bounds.center);
        }
    }

    private void FixedUpdate()
    {
        // Let the XR grab component control the model while held.
        if (Paused || (interactable != null && interactable.isSelected))
            return;

        Quaternion delta = Quaternion.AngleAxis(degreesPerSecond * Time.fixedDeltaTime, Vector3.up);
        Vector3 center = transform.TransformPoint(localCenter);
        Vector3 position = center + delta * (transform.position - center);
        Quaternion rotation = delta * transform.rotation;
        transform.SetPositionAndRotation(position, rotation);
    }
}
