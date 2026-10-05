using UnityEngine;
using System;

/// <summary>
/// Base class for all museum stations (ship model, helmet, timeline).
/// Manages station state machine: NotVisited -> Visiting -> Completed.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class StationBase : InteractableObject
{
    public enum StationState
    {
        NotVisited,
        Visiting,
        Completed
    }

    [Header("Station Configuration")]
    [SerializeField] protected int stationIndex = 0;
    [SerializeField] protected string stationTitle = "Station";
    [SerializeField] protected string stationDescription = "";
    [SerializeField] protected float estimatedDuration = 90f;

    [Header("Station References")]
    [SerializeField] protected Transform stationRoot;
    [SerializeField] protected Collider triggerZone;

    [Header("Audio Cues")]
    [SerializeField] protected AudioClip introClip;
    [SerializeField] protected AudioClip completeClip;

    // State
    protected StationState currentState = StationState.NotVisited;
    protected float stationEnterTime;
    protected bool hasBeenVisited = false;

    // Properties
    public StationState CurrentState => currentState;
    public bool HasBeenVisited => hasBeenVisited;
    public int StationIndex => stationIndex;
    public string StationTitle => stationTitle;
    public float StationElapsed => currentState == StationState.Visiting ? Time.time - stationEnterTime : 0f;

    /// <summary>
    /// Called when a visitor enters this station's trigger zone.
    /// </summary>
    public virtual void OnStationEntered()
    {
        if (currentState == StationState.NotVisited)
        {
            hasBeenVisited = true;
            currentState = StationState.Visiting;
            stationEnterTime = Time.time;

            if (introClip != null)
            {
                AudioManager.Instance?.PlayVoiceover(introClip);
            }

            Debug.Log($"[StationBase] Entered: {stationTitle}");
        }
    }

    /// <summary>
    /// Called when a visitor exits this station's trigger zone.
    /// </summary>
    public virtual void OnStationExited()
    {
        if (currentState == StationState.Visiting)
        {
            currentState = StationState.Completed;

            if (completeClip != null)
            {
                AudioManager.Instance?.PlaySFX(completeClip);
            }

            Debug.Log($"[StationBase] Exited: {stationTitle} (visited for {StationElapsed:F1}s)");

            // Check if all stations are done
            if (GameManager.Instance != null && GameManager.Instance.AllStationsVisited())
            {
                Debug.Log("[StationBase] All stations visited.");
            }
        }
    }

    /// <summary>
    /// Resets this station to its initial state.
    /// </summary>
    public virtual void ResetStation()
    {
        currentState = StationState.NotVisited;
        hasBeenVisited = false;
        OnDeselect();
        OnHoverExit();
    }

    /// <summary>
    /// Override in derived classes to provide custom interaction logic.
    /// Called when the visitor selects the station's main object.
    /// </summary>
    public override void OnSelect()
    {
        base.OnSelect();
        HandleStationInteraction();
    }

    /// <summary>
    /// Implemented by each station subclass to define its interaction.
    /// </summary>
    protected abstract void HandleStationInteraction();

    /// <summary>
    /// Returns a status string for UI display.
    /// </summary>
    public virtual string GetStatusText()
    {
        switch (currentState)
        {
            case StationState.NotVisited:
                return $"{stationTitle} — Not yet visited";
            case StationState.Visiting:
                return $"{stationTitle} — Exploring ({StationElapsed:F0}s)";
            case StationState.Completed:
                return $"{stationTitle} — Completed";
            default:
                return stationTitle;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Auto-enter when visitor walks into trigger zone
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnterStation(stationIndex);
            }
            else
            {
                OnStationEntered();
            }
        }
    }
}
