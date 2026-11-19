using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

[DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers - 1)]
[DisallowMultipleComponent]
public sealed class MuseumDesktopMode : MonoBehaviour
{
    [SerializeField] private XRInteractionManager interactionManager;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(1f)] private float rayDistance = 20f;

    private readonly List<Behaviour> paused = new List<Behaviour>();
    private XROrigin rig;
    private Camera view;
    private CharacterController body;
    private Vector3 trackedPosition;
    private Quaternion trackedRotation;
    private StereoTargetEyeMask trackedEye;
    private XRRayInteractor pointer;
    private LineRenderer beam;
    private Material beamMaterial;

    private void OnEnable()
    {
        foreach (var origin in FindObjectsOfType<XROrigin>())
        {
            if (origin.gameObject.scene != gameObject.scene || origin.Camera == null)
                continue;
            rig = origin;
            break;
        }

        if (rig == null || interactionManager == null)
        {
            Debug.LogError("Keyboard and Mouse Interaction requires the scene XR rig and interaction manager.", this);
            enabled = false;
            return;
        }

        view = rig.Camera;
        body = rig.GetComponent<CharacterController>();
        trackedPosition = view.transform.localPosition;
        trackedRotation = view.transform.localRotation;
        trackedEye = view.stereoTargetEye;

        Pause(view.GetComponents<TrackedPoseDriver>());
        Pause(rig.GetComponentsInChildren<LocomotionProvider>(true));
        Pause(rig.GetComponentsInChildren<XRBaseInteractor>(true));
        Pause(rig.GetComponentsInChildren<XRInteractorLineVisual>(true));
        Pause(rig.GetComponentsInChildren<XRBaseController>(true));
        view.stereoTargetEye = StereoTargetEyeMask.None;

        if (pointer == null)
            CreatePointer();
        UpdatePointerPose();
        pointer.gameObject.SetActive(true);
    }

    private void Pause<T>(T[] components) where T : Behaviour
    {
        foreach (var component in components)
        {
            if (!component.enabled)
                continue;
            paused.Add(component);
            component.enabled = false;
        }
    }

    private void CreatePointer()
    {
        var rayObject = new GameObject("Mouse Ray");
        rayObject.SetActive(false);
        rayObject.transform.SetParent(transform, false);
        rayObject.AddComponent<MuseumMouseController>().enableInputTracking = false;

        pointer = rayObject.AddComponent<XRRayInteractor>();
        pointer.interactionManager = interactionManager;
        pointer.rayOriginTransform = rayObject.transform;
        pointer.lineType = XRRayInteractor.LineType.StraightLine;
        pointer.maxRaycastDistance = rayDistance;
        pointer.hitClosestOnly = true;
        pointer.allowAnchorControl = false;
        // The scene's EventSystem already handles screen-space mouse input.
        pointer.enableUIInteraction = false;

        beam = rayObject.AddComponent<LineRenderer>();
        beam.positionCount = 2;
        beam.useWorldSpace = true;
        beam.startWidth = 0.005f;
        beam.endWidth = 0.012f;
        beam.startColor = beam.endColor = Color.cyan;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            beamMaterial = new Material(shader);
            beam.sharedMaterial = beamMaterial;
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (Application.isFocused && keyboard != null)
        {
            int turn = (keyboard.dKey.wasPressedThisFrame ? 1 : 0) -
                       (keyboard.aKey.wasPressedThisFrame ? 1 : 0);
            if (turn != 0)
                rig.transform.Rotate(Vector3.up, turn * 30f, Space.World);

            int move = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector3 forward = Vector3.ProjectOnPlane(rig.transform.forward, Vector3.up).normalized;
            Vector3 velocity = forward * (move * moveSpeed);
            if (body != null && body.enabled)
                body.SimpleMove(velocity);
            else
                rig.transform.position += velocity * Time.deltaTime;
        }

        UpdatePointerPose();
    }

    private void UpdatePointerPose()
    {
        Transform cameraTransform = view.transform;
        cameraTransform.SetPositionAndRotation(
            rig.transform.TransformPoint(Vector3.up * rig.CameraYOffset),
            rig.transform.rotation);

        var mouse = Mouse.current;
        bool canAim = Application.isFocused && mouse != null &&
                      view.pixelRect.Contains(mouse.position.ReadValue());
        if (!canAim)
        {
            pointer.enabled = false;
            beam.enabled = false;
            return;
        }

        Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
        pointer.transform.SetPositionAndRotation(ray.origin, Quaternion.LookRotation(ray.direction, cameraTransform.up));
        pointer.enabled = true;
        beam.enabled = beamMaterial != null;
    }

    private void LateUpdate()
    {
        if (!beam.enabled)
            return;

        Transform aim = pointer.transform;
        Vector3 end = pointer.TryGetCurrent3DRaycastHit(out RaycastHit hit)
            ? hit.point
            : aim.position + aim.forward * rayDistance;
        beam.SetPosition(0, aim.position - view.transform.up * 0.05f);
        beam.SetPosition(1, end);
    }

    private void OnDisable()
    {
        if (pointer != null)
            pointer.gameObject.SetActive(false);

        if (view != null)
        {
            view.transform.localPosition = trackedPosition;
            view.transform.localRotation = trackedRotation;
            view.stereoTargetEye = trackedEye;
        }

        foreach (var component in paused)
        {
            if (component != null)
                component.enabled = true;
        }
        paused.Clear();
        view = null;
        rig = null;
    }

    private void OnDestroy()
    {
        if (beamMaterial != null)
            Destroy(beamMaterial);
    }
}
