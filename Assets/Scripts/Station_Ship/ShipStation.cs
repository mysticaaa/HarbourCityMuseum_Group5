using UnityEngine;

/// <summary>
/// Ship Model Station — the primary interactive exhibit.
/// Visitors inspect the ship in 360 degrees, then enter to see the
/// cabin, seating layout, instrument displays and operating machinery.
/// 
/// Interaction modes:
///   Phase 1 (default): Orbit — visitor rotates the ship 360 degrees
///   Phase 2 (select):  Enter — visitor enters the ship interior
///                      (panoramic visit or cut-away fallback)
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class ShipStation : StationBase
{
    public enum InteriorMode
    {
        Panorama,
        CutAway,
        Undecided
    }

    [Header("Ship Model References")]
    [SerializeField] private Transform shipModelRoot;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Interior Configuration")]
    [SerializeField] private InteriorMode interiorMode = InteriorMode.Undecided;
    [SerializeField] private GameObject panoramaInterior;
    [SerializeField] private GameObject cutAwayView;
    [SerializeField] private Transform interiorEntryPoint;
    [SerializeField] private float interiorTransitionSpeed = 1.5f;

    [Header("Interior Hotspots")]
    [SerializeField] private Transform[] cabinHotspots;
    [SerializeField] private string[] hotspotLabels = {
        "Cargo hold",
        "Crew seating",
        "Instrument panel",
        "Operating machinery"
    };

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float orbitDistance = 3f;
    [SerializeField] private float orbitHeight = 1.5f;

    // State
    private enum ShipInteractionState
    {
        Orbiting,
        TransitioningToInterior,
        InsideInterior,
        TransitioningBack
    }

    private ShipInteractionState interactionState = ShipInteractionState.Orbiting;
    private float currentOrbitAngle = 0f;
    private bool isInsideInterior = false;
    private int currentHotspotIndex = 0;

    protected override void HandleStationInteraction()
    {
        switch (interactionState)
        {
            case ShipInteractionState.Orbiting:
                TransitionToInterior();
                break;
            case ShipInteractionState.InsideInterior:
                CycleHotspot();
                break;
            default:
                // Transitioning, ignore input
                break;
        }
    }

    private void Update()
    {
        if (currentState != StationState.Visiting)
            return;

        switch (interactionState)
        {
            case ShipInteractionState.Orbiting:
                UpdateOrbit();
                break;
            case ShipInteractionState.TransitioningToInterior:
                UpdateTransitionToInterior();
                break;
            case ShipInteractionState.InsideInterior:
                UpdateInteriorView();
                break;
            case ShipInteractionState.TransitioningBack:
                UpdateTransitionBack();
                break;
        }
    }

    /// <summary>
    /// Smoothly rotates the ship model for 360 inspection.
    /// </summary>
    private void UpdateOrbit()
    {
        if (shipModelRoot == null)
            return;

        // Auto-rotate slowly; visitor can adjust with ray selection
        currentOrbitAngle += rotationSpeed * Time.deltaTime;
        shipModelRoot.rotation = Quaternion.Euler(0f, currentOrbitAngle, 0f);

        // Position camera at orbit distance
        if (mainCamera != null)
        {
            Vector3 orbitPos = new Vector3(
                Mathf.Sin(currentOrbitAngle * Mathf.Deg2Rad) * orbitDistance,
                orbitHeight,
                Mathf.Cos(currentOrbitAngle * Mathf.Deg2Rad) * orbitDistance
            );

            if (shipModelRoot != null)
                orbitPos += shipModelRoot.position;

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position, orbitPos, Time.deltaTime * 2f
            );
            mainCamera.transform.LookAt(shipModelRoot.position + Vector3.up * orbitHeight);
        }
    }

    /// <summary>
    /// Begins the transition from orbit view to ship interior.
    /// </summary>
    private void TransitionToInterior()
    {
        // Decide interior mode if not yet chosen
        if (interiorMode == InteriorMode.Undecided)
        {
            // Default to panorama; cut-away is the fallback
            // This decision should be made by the team early in Week 2
            interiorMode = InteriorMode.Panorama;
            Debug.Log("[ShipStation] Interior mode decided: Panorama (default)");
        }

        // Activate the appropriate interior representation
        if (interiorMode == InteriorMode.Panorama && panoramaInterior != null)
        {
            panoramaInterior.SetActive(true);
            cutAwayView?.SetActive(false);
        }
        else if (interiorMode == InteriorMode.CutAway && cutAwayView != null)
        {
            cutAwayView.SetActive(true);
            panoramaInterior?.SetActive(false);
        }

        interactionState = ShipInteractionState.TransitioningToInterior;
        isInsideInterior = true;

        Debug.Log("[ShipStation] Transitioning to interior view...");
    }

    /// <summary>
    /// Smoothly moves camera toward the interior entry point.
    /// </summary>
    private void UpdateTransitionToInterior()
    {
        if (mainCamera == null || interiorEntryPoint == null)
        {
            interactionState = ShipInteractionState.InsideInterior;
            return;
        }

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            interiorEntryPoint.position,
            Time.deltaTime * interiorTransitionSpeed
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            interiorEntryPoint.rotation,
            Time.deltaTime * interiorTransitionSpeed
        );

        float dist = Vector3.Distance(mainCamera.transform.position, interiorEntryPoint.position);
        if (dist < 0.1f)
        {
            interactionState = ShipInteractionState.InsideInterior;
            currentHotspotIndex = 0;
            FocusHotspot(currentHotspotIndex);

            Debug.Log("[ShipStation] Now inside interior. Select to cycle hotspots.");
        }
    }

    /// <summary>
    /// Manages the interior view when the visitor is inside.
    /// </summary>
    private void UpdateInteriorView()
    {
        // Idle — waiting for visitor to cycle hotspots via selection
    }

    /// <summary>
    /// Cycles through predefined interior hotspots.
    /// </summary>
    private void CycleHotspot()
    {
        currentHotspotIndex = (currentHotspotIndex + 1) % cabinHotspots.Length;
        FocusHotspot(currentHotspotIndex);
    }

    /// <summary>
    /// Focuses the camera on a specific interior hotspot.
    /// </summary>
    private void FocusHotspot(int index)
    {
        if (cabinHotspots == null || index >= cabinHotspots.Length)
            return;

        Transform hotspot = cabinHotspots[index];
        if (hotspot == null)
            return;

        if (mainCamera != null)
        {
            // Smooth look at hotspot
            Vector3 direction = hotspot.position - mainCamera.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            mainCamera.transform.rotation = targetRotation;
        }

        string label = index < hotspotLabels.Length ? hotspotLabels[index] : $"Hotspot {index + 1}";
        Debug.Log($"[ShipStation] Focused hotspot: {label}");

        // Notify UI to show label
        UIManager.Instance?.ShowTooltip(label, 3f);
    }

    /// <summary>
    /// Returns the visitor to orbit view.
    /// </summary>
    public void ExitInterior()
    {
        interactionState = ShipInteractionState.TransitioningBack;
        Debug.Log("[ShipStation] Transitioning back to orbit view...");
    }

    /// <summary>
    /// Smoothly moves camera back to orbit position.
    /// </summary>
    private void UpdateTransitionBack()
    {
        // Deactivate interior representations
        panoramaInterior?.SetActive(false);
        cutAwayView?.SetActive(false);

        interactionState = ShipInteractionState.Orbiting;
        isInsideInterior = false;
        Debug.Log("[ShipStation] Returned to orbit view.");
    }

    /// <summary>
    /// Sets the interior representation mode (for runtime switching).
    /// </summary>
    public void SetInteriorMode(InteriorMode mode)
    {
        interiorMode = mode;
        Debug.Log($"[ShipStation] Interior mode set to: {mode}");
    }

    public override string GetStatusText()
    {
        string baseText = base.GetStatusText();
        if (isInsideInterior)
        {
            int current = currentHotspotIndex + 1;
            int total = cabinHotspots != null ? cabinHotspots.Length : 0;
            return $"{baseText} | Interior hotspot {current}/{total}";
        }
        return baseText;
    }
}
