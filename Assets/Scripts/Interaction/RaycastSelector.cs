using UnityEngine;
using Valve.VR;

/// <summary>
/// Ray-based selection controller using SteamVR input.
/// Casts a ray from the VR controller; detects InteractableObject hits
/// and handles hover/select states via trigger input.
/// 
/// Designed for Meta Quest 3 + SteamVR streaming.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RaycastSelector : MonoBehaviour
{
    [Header("SteamVR Input Actions")]
    [Tooltip("Boolean action for trigger press (e.g. /actions/default/in/InteractUI)")]
    public SteamVR_Action_Boolean selectAction = SteamVR_Input.GetBooleanAction("InteractUI");

    [Tooltip("Which hand this controller represents")]
    public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any;

    [Header("Ray Settings")]
    [Tooltip("Maximum ray length in meters")]
    [SerializeField] private float maxRayDistance = 10f;

    [Tooltip("Ray width in world units")]
    [SerializeField] private float rayWidth = 0.01f;

    [Header("Visual")]
    [SerializeField] private Material rayMaterial;
    [SerializeField] private Color rayColorNormal = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    [SerializeField] private Color rayColorHover = new Color(1f, 0.85f, 0.3f, 0.8f);
    [SerializeField] private Color rayColorSelect = new Color(0.3f, 0.9f, 0.5f, 0.9f);

    [Header("Fallback (Non-VR Testing)")]
    [Tooltip("Enable mouse-based ray for testing without headset")]
    public bool enableMouseFallback = true;
    [SerializeField] private KeyCode mouseSelectKey = KeyCode.Mouse0;

    // Components
    private LineRenderer lineRenderer;
    private Transform controllerTransform;

    // Current state
    private InteractableObject currentHovered;
    private InteractableObject currentSelected;
    private bool triggerDown = false;

    // Debug
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        controllerTransform = transform;

        // Configure line renderer
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            if (rayMaterial != null)
                lineRenderer.material = rayMaterial;
            else
                lineRenderer.material = new Material(Shader.Find("Unlit/Transparent"));

            lineRenderer.startColor = rayColorNormal;
            lineRenderer.endColor = rayColorNormal;
        }
    }

    private void Update()
    {
        UpdateRay();
        HandleInput();
        UpdateRayVisual();
    }

    /// <summary>
    /// Casts a ray and updates the hovered object.
    /// </summary>
    private void UpdateRay()
    {
        Ray ray;
        bool validRay = false;

        // Try VR controller ray first
        if (controllerTransform != null && SteamVR.active)
        {
            ray = new Ray(controllerTransform.position, controllerTransform.forward);
            validRay = true;
        }
        // Fallback: mouse ray for desktop testing
        else if (enableMouseFallback && Camera.main != null)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            validRay = true;
        }
        else
        {
            ray = new Ray(Vector3.zero, Vector3.forward);
            validRay = false;
        }

        if (!validRay)
        {
            ClearHover();
            return;
        }

        RaycastHit hit;
        InteractableObject hitObject = null;

        if (Physics.Raycast(ray, out hit, maxRayDistance))
        {
            hitObject = hit.collider.GetComponent<InteractableObject>();
            if (hitObject == null)
            {
                hitObject = hit.collider.GetComponentInParent<InteractableObject>();
            }
        }

        // Update hover state
        if (hitObject != currentHovered)
        {
            ClearHover();

            if (hitObject != null && hitObject.IsInteractable)
            {
                currentHovered = hitObject;
                currentHovered.OnHoverEnter();

                if (enableDebugLog)
                    Debug.Log($"[RaycastSelector] Hover: {currentHovered.DisplayName}");
            }
        }

        // Store the ray end point for visual
        lastRayOrigin = ray.origin;
        lastRayDirection = ray.direction;
        lastRayHit = hit;
        lastRayValid = true;
    }

    private Vector3 lastRayOrigin;
    private Vector3 lastRayDirection;
    private RaycastHit lastRayHit;
    private bool lastRayValid = false;

    /// <summary>
    /// Handles trigger press/release for selection.
    /// </summary>
    private void HandleInput()
    {
        bool triggerPressed = false;

        // SteamVR trigger
        if (selectAction != null && SteamVR.active)
        {
            triggerPressed = selectAction.GetStateDown(inputSource);
        }

        // Mouse fallback
        if (enableMouseFallback && !SteamVR.active)
        {
            triggerPressed = Input.GetKeyDown(mouseSelectKey);
        }

        if (triggerPressed && !triggerDown)
        {
            triggerDown = true;

            if (currentHovered != null)
            {
                // Deselect previous
                if (currentSelected != null && currentSelected != currentHovered)
                {
                    currentSelected.OnDeselect();
                    currentSelected = null;
                }

                // Select new
                currentSelected = currentHovered;
                currentSelected.OnSelect();

                if (enableDebugLog)
                    Debug.Log($"[RaycastSelector] Selected: {currentSelected.DisplayName}");
            }
            else if (currentSelected != null)
            {
                // Click empty space: deselect
                currentSelected.OnDeselect();
                currentSelected = null;

                if (enableDebugLog)
                    Debug.Log("[RaycastSelector] Deselected.");
            }
        }

        if (!triggerPressed)
        {
            triggerDown = false;
        }
    }

    /// <summary>
    /// Updates the LineRenderer visual based on current state.
    /// </summary>
    private void UpdateRayVisual()
    {
        if (lineRenderer == null || !lastRayValid)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 startPos = lastRayOrigin;
        Vector3 endPos;

        if (currentHovered != null)
        {
            // Snap to hit point
            endPos = lastRayHit.point;

            Color c = currentSelected == currentHovered ? rayColorSelect : rayColorHover;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
        }
        else
        {
            // Full ray length
            endPos = lastRayOrigin + lastRayDirection * maxRayDistance;

            lineRenderer.startColor = rayColorNormal;
            lineRenderer.endColor = rayColorNormal;
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    /// <summary>
    /// Clears the current hover state.
    /// </summary>
    private void ClearHover()
    {
        if (currentHovered != null)
        {
            currentHovered.OnHoverExit();
            currentHovered = null;
        }
    }

    /// <summary>
    /// Forcefully clears all selection state.
    /// </summary>
    public void ClearSelection()
    {
        ClearHover();
        if (currentSelected != null)
        {
            currentSelected.OnDeselect();
            currentSelected = null;
        }
    }

    /// <summary>
    /// Returns the currently hovered object, or null.
    /// </summary>
    public InteractableObject GetHoveredObject()
    {
        return currentHovered;
    }

    /// <summary>
    /// Returns the currently selected object, or null.
    /// </summary>
    public InteractableObject GetSelectedObject()
    {
        return currentSelected;
    }
}
