using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class HelmetHandInspection : MonoBehaviour
{
    private XRGrabInteractable grab;
    private ExhibitAutoRotation rotation;
    private Rigidbody body;
    private Transform homeParent;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 homeScale;
    private Coroutine returning;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        rotation = GetComponentInParent<ExhibitAutoRotation>();
        SaveHomePose();
        grab.useDynamicAttach = false;
        grab.throwOnDetach = false;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.attachEaseInTime = 0.25f;
        // An offset anchor keeps the model ahead of the hand, away from the headset.
        var anchor = new GameObject("ZXH_HelmetGripAnchor").transform;
        anchor.SetParent(transform, false);
        Bounds bounds = ExhibitInspectionHint.GetBounds(transform);
        anchor.position = bounds.center - Vector3.forward * 0.15f;
        anchor.rotation = Quaternion.identity;
        grab.attachTransform = anchor;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        ExhibitInspectionHint.Create(transform, "Helmet: hold trigger to inspect; release to return");
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseControllerInteractor controller)
            controller.SendHapticImpulse(0.2f, 0.06f);
        if (returning != null) { StopCoroutine(returning); returning = null; }
        if (rotation != null) rotation.Paused = true;
    }

    private void SaveHomePose()
    {
        homeParent = transform.parent;
        homePosition = transform.localPosition;
        homeRotation = transform.localRotation;
        homeScale = transform.localScale;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (isActiveAndEnabled) returning = StartCoroutine(ReturnHome());
    }

    private IEnumerator ReturnHome()
    {
        transform.SetParent(homeParent, true);
        body.useGravity = false;
        body.isKinematic = true;
        Vector3 start = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        Vector3 startScale = transform.localScale;
        for (float t = 0; t < 1; t += Time.deltaTime / 0.45f)
        {
            float eased = Mathf.SmoothStep(0, 1, t);
            transform.localPosition = Vector3.Lerp(start, homePosition, eased);
            transform.localRotation = Quaternion.Slerp(startRotation, homeRotation, eased);
            transform.localScale = Vector3.Lerp(startScale, homeScale, eased);
            yield return null;
        }
        transform.localPosition = homePosition;
        transform.localRotation = homeRotation;
        transform.localScale = homeScale;
        returning = null;
        if (rotation != null) rotation.Paused = false;
    }

    private void OnDisable()
    {
        if (grab == null) return;
        if (returning != null) { StopCoroutine(returning); returning = null; }
        transform.SetParent(homeParent, false);
        transform.localPosition = homePosition;
        transform.localRotation = homeRotation;
        transform.localScale = homeScale;
        if (body != null) { body.useGravity = false; body.isKinematic = true; }
        if (rotation != null) rotation.Paused = false;
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }
        if (rotation != null) rotation.Paused = false;
    }
}
