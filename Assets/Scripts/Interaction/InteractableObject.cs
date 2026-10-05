using UnityEngine;
using System;

/// <summary>
/// Base class for all interactive museum objects.
/// Attach to any GameObject the visitor can select with the ray.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private string displayName = "Interactive Object";
    [SerializeField] private string description = "";

    [Header("Visual Feedback")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material selectedMaterial;

    [Header("Outline Effect")]
    [SerializeField] private bool useOutlineEffect = true;
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.3f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.9f, 0.5f, 0.5f);

    // State tracking
    private bool isHovered = false;
    private bool isSelected = false;
    private Renderer objectRenderer;
    private Material originalMaterial;

    // Events
    public event Action<InteractableObject> OnHoverEnterEvent;
    public event Action<InteractableObject> OnHoverExitEvent;
    public event Action<InteractableObject> OnSelectEvent;
    public event Action<InteractableObject> OnDeselectEvent;

    // Properties
    public bool IsInteractable => isInteractable;
    public bool IsHovered => isHovered;
    public bool IsSelected => isSelected;
    public string DisplayName => displayName;
    public string Description => description;

    protected virtual void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }

        if (objectRenderer != null && normalMaterial != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    /// <summary>
    /// Called when the ray hovers over this object.
    /// </summary>
    public virtual void OnHoverEnter()
    {
        if (!isInteractable || isHovered)
            return;

        isHovered = true;

        if (useOutlineEffect)
        {
            ApplyOutlineEffect(hoverColor);
        }

        if (hoverMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = hoverMaterial;
        }

        OnHoverEnterEvent?.Invoke(this);
    }

    /// <summary>
    /// Called when the ray leaves this object.
    /// </summary>
    public virtual void OnHoverExit()
    {
        if (!isHovered)
            return;

        isHovered = false;

        if (useOutlineEffect)
        {
            RemoveOutlineEffect();
        }

        if (!isSelected)
        {
            if (normalMaterial != null && objectRenderer != null)
            {
                objectRenderer.material = normalMaterial;
            }
            else if (originalMaterial != null && objectRenderer != null)
            {
                objectRenderer.material = originalMaterial;
            }
        }

        OnHoverExitEvent?.Invoke(this);
    }

    /// <summary>
    /// Called when the visitor selects this object (trigger press).
    /// </summary>
    public virtual void OnSelect()
    {
        if (!isInteractable)
            return;

        isSelected = true;

        if (useOutlineEffect)
        {
            ApplyOutlineEffect(selectedColor);
        }

        if (selectedMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = selectedMaterial;
        }

        OnSelectEvent?.Invoke(this);
    }

    /// <summary>
    /// Called when the visitor releases the selection.
    /// </summary>
    public virtual void OnDeselect()
    {
        isSelected = false;

        if (useOutlineEffect && !isHovered)
        {
            RemoveOutlineEffect();
        }

        if (objectRenderer != null)
        {
            if (isHovered && hoverMaterial != null)
            {
                objectRenderer.material = hoverMaterial;
            }
            else if (normalMaterial != null)
            {
                objectRenderer.material = normalMaterial;
            }
            else if (originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }
        }

        OnDeselectEvent?.Invoke(this);
    }

    /// <summary>
    /// Enables interaction with this object.
    /// </summary>
    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (!isInteractable)
        {
            if (isHovered) OnHoverExit();
            if (isSelected) OnDeselect();
        }
    }

    protected virtual void ApplyOutlineEffect(Color color)
    {
        // Simple emissive outline as placeholder.
        // Can be replaced with a proper outline shader later.
        if (objectRenderer != null)
        {
            objectRenderer.material.SetColor("_EmissionColor", color);
            objectRenderer.material.EnableKeyword("_EMISSION");
        }
    }

    protected virtual void RemoveOutlineEffect()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.SetColor("_EmissionColor", Color.black);
            objectRenderer.material.DisableKeyword("_EMISSION");
        }
    }

    protected virtual void OnDestroy()
    {
        OnHoverEnterEvent = null;
        OnHoverExitEvent = null;
        OnSelectEvent = null;
        OnDeselectEvent = null;
    }
}
