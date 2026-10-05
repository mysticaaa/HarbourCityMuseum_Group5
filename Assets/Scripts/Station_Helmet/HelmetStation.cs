using UnityEngine;

/// <summary>
/// Diving Helmet Station — visitors examine the vintage diving helmet's
/// exterior shape, construction and function at scale, and review its
/// development history and the underwater work it supported.
/// 
/// Interaction:
///   - Hover: helmet glows
///   - Select once: zoom in to inspect construction details
///   - Select again: cycle through labeled component highlights
///   - Select on last component: zoom out
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class HelmetStation : StationBase
{
    [Header("Helmet Model References")]
    [SerializeField] private Transform helmetModel;
    [SerializeField] private float modelOrbitSpeed = 15f;

    [Header("Zoom Configuration")]
    [SerializeField] private float defaultScale = 1f;
    [SerializeField] private float zoomedScale = 2.5f;
    [SerializeField] private float zoomTransitionSpeed = 2f;

    [Header("Component Highlights")]
    [SerializeField] private Transform[] helmetComponents;
    [SerializeField] private string[] componentLabels = {
        "Bronze body",
        "Viewing ports",
        "Air intake valve",
        "Communication fitting",
        "Weight collar"
    };

    [Header("Info Panels")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private UnityEngine.UI.Text infoText;

    // State
    private enum HelmetInteractionState
    {
        Idle,
        ZoomingIn,
        Inspecting,
        ZoomingOut
    }

    private HelmetInteractionState helmetState = HelmetInteractionState.Idle;
    private float currentScale = 1f;
    private float targetScale = 1f;
    private int currentComponentIndex = -1;
    private bool isZoomed = false;

    protected override void HandleStationInteraction()
    {
        switch (helmetState)
        {
            case HelmetInteractionState.Idle:
                ZoomIn();
                break;
            case HelmetInteractionState.Inspecting:
                CycleComponent();
                break;
            case HelmetInteractionState.ZoomingIn:
            case HelmetInteractionState.ZoomingOut:
                // Ignore during transition
                break;
        }
    }

    private void Update()
    {
        if (currentState != StationState.Visiting)
            return;

        // Always rotate the helmet slowly
        if (helmetModel != null)
        {
            helmetModel.Rotate(Vector3.up, modelOrbitSpeed * Time.deltaTime);
        }

        // Smooth scale transition
        if (Mathf.Abs(currentScale - targetScale) > 0.01f)
        {
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * zoomTransitionSpeed);

            if (helmetModel != null)
            {
                helmetModel.localScale = Vector3.one * currentScale;
            }

            // Check if transition complete
            if (helmetState == HelmetInteractionState.ZoomingIn &&
                Mathf.Abs(currentScale - zoomedScale) < 0.05f)
            {
                helmetState = HelmetInteractionState.Inspecting;
                currentComponentIndex = 0;
                HighlightComponent(currentComponentIndex);
                Debug.Log("[HelmetStation] Now inspecting. Select to cycle components.");
            }
            else if (helmetState == HelmetInteractionState.ZoomingOut &&
                     Mathf.Abs(currentScale - defaultScale) < 0.05f)
            {
                helmetState = HelmetInteractionState.Idle;
                isZoomed = false;
                HideInfoPanel();
                Debug.Log("[HelmetStation] Zoomed out. Select to zoom in again.");
            }
        }
    }

    /// <summary>
    /// Zooms in on the helmet for detailed inspection.
    /// </summary>
    private void ZoomIn()
    {
        targetScale = zoomedScale;
        helmetState = HelmetInteractionState.ZoomingIn;
        isZoomed = true;
        ShowInfoPanel("Inspecting diving helmet — select to cycle components");
        Debug.Log("[HelmetStation] Zooming in...");
    }

    /// <summary>
    /// Zooms out back to default view.
    /// </summary>
    private void ZoomOut()
    {
        targetScale = defaultScale;
        helmetState = HelmetInteractionState.ZoomingOut;
        ClearHighlights();
        currentComponentIndex = -1;
        Debug.Log("[HelmetStation] Zooming out...");
    }

    /// <summary>
    /// Cycles through labeled components of the helmet.
    /// </summary>
    private void CycleComponent()
    {
        // Clear previous highlight
        if (currentComponentIndex >= 0)
        {
            ClearHighlight(currentComponentIndex);
        }

        currentComponentIndex++;

        // If we've gone through all components, zoom out
        if (helmetComponents != null && currentComponentIndex >= helmetComponents.Length)
        {
            ZoomOut();
            return;
        }

        HighlightComponent(currentComponentIndex);
    }

    /// <summary>
    /// Highlights a specific component and shows its label.
    /// </summary>
    private void HighlightComponent(int index)
    {
        if (helmetComponents == null || index < 0 || index >= helmetComponents.Length)
            return;

        Transform component = helmetComponents[index];
        if (component == null)
            return;

        // Emissive highlight
        Renderer r = component.GetComponent<Renderer>();
        if (r == null)
            r = component.GetComponentInChildren<Renderer>();

        if (r != null)
        {
            r.material.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f, 1f));
            r.material.EnableKeyword("_EMISSION");
        }

        string label = index < componentLabels.Length ? componentLabels[index] : $"Component {index + 1}";
        ShowInfoPanel(label);

        Debug.Log($"[HelmetStation] Highlighting: {label}");
    }

    /// <summary>
    /// Clears highlight on a specific component.
    /// </summary>
    private void ClearHighlight(int index)
    {
        if (helmetComponents == null || index < 0 || index >= helmetComponents.Length)
            return;

        Transform component = helmetComponents[index];
        if (component == null)
            return;

        Renderer r = component.GetComponent<Renderer>();
        if (r == null)
            r = component.GetComponentInChildren<Renderer>();

        if (r != null)
        {
            r.material.SetColor("_EmissionColor", Color.black);
            r.material.DisableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// Clears all component highlights.
    /// </summary>
    private void ClearHighlights()
    {
        if (helmetComponents == null)
            return;

        for (int i = 0; i < helmetComponents.Length; i++)
        {
            ClearHighlight(i);
        }
    }

    /// <summary>
    /// Shows the info panel with given text.
    /// </summary>
    private void ShowInfoPanel(string text)
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (infoText != null)
            infoText.text = text;
    }

    /// <summary>
    /// Hides the info panel.
    /// </summary>
    private void HideInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    public override void ResetStation()
    {
        base.ResetStation();
        helmetState = HelmetInteractionState.Idle;
        currentScale = defaultScale;
        targetScale = defaultScale;
        currentComponentIndex = -1;
        isZoomed = false;
        ClearHighlights();
        HideInfoPanel();

        if (helmetModel != null)
            helmetModel.localScale = Vector3.one * defaultScale;
    }

    public override string GetStatusText()
    {
        string baseText = base.GetStatusText();
        if (isZoomed && currentComponentIndex >= 0)
        {
            int total = helmetComponents != null ? helmetComponents.Length : 0;
            return $"{baseText} | Component {currentComponentIndex + 1}/{total}";
        }
        return baseText;
    }
}
