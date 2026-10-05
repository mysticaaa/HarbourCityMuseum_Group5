using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the 5-minute visitor experience flow:
/// 1. Welcome & spoken intro (~30s)
/// 2. Ship model station — 360 inspection + interior (~2min)
/// 3. Port-city timeline wall (~1min)
/// 4. Diving helmet station — zoom inspection (~1min)
/// 5. Session complete & feedback prompt (~30s)
/// 
/// Total: ~5 minutes
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class ExperienceFlow : MonoBehaviour
{
    [Header("Flow Timing (seconds)")]
    [SerializeField] private float introDuration = 30f;
    [SerializeField] private float shipStationDuration = 120f;
    [SerializeField] private float timelineStationDuration = 60f;
    [SerializeField] private float helmetStationDuration = 60f;
    [SerializeField] private float outroDuration = 30f;

    [Header("Auto-Advance")]
    [Tooltip("If true, automatically advances to next station after duration. " +
             "If false, waits for visitor to trigger next station.")]
    [SerializeField] private bool autoAdvance = false;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    // State
    public enum FlowPhase
    {
        None,
        Intro,
        ShipStation,
        TimelineStation,
        HelmetStation,
        Outro,
        Complete
    }

    private FlowPhase currentPhase = FlowPhase.None;
    private Coroutine phaseCoroutine;

    public FlowPhase CurrentPhase => currentPhase;

    private void Start()
    {
        StartFlow();
    }

    /// <summary>
    /// Begins the experience flow.
    /// </summary>
    public void StartFlow()
    {
        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Starting experience flow.");

        BeginIntro();
    }

    private void BeginIntro()
    {
        currentPhase = FlowPhase.Intro;

        // Play welcome audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySessionIntro();
            AudioManager.Instance.SetAmbientPort();
        }

        // Show welcome UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWelcome();
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Intro phase started.");

        phaseCoroutine = StartCoroutine(PhaseTimer(introDuration, () => BeginShipStation()));
    }

    private void BeginShipStation()
    {
        currentPhase = FlowPhase.ShipStation;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.DismissWelcome();
            UIManager.Instance.ShowHint("Point at the ship model and press trigger to inspect it.");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterStation(0);
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Ship station phase started.");

        if (autoAdvance)
        {
            phaseCoroutine = StartCoroutine(PhaseTimer(shipStationDuration, () => BeginTimelineStation()));
        }
    }

    private void BeginTimelineStation()
    {
        currentPhase = FlowPhase.TimelineStation;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterStation(1);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHint("Point at the timeline wall to explore port-city stories.");
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Timeline station phase started.");

        if (autoAdvance)
        {
            phaseCoroutine = StartCoroutine(PhaseTimer(timelineStationDuration, () => BeginHelmetStation()));
        }
    }

    private void BeginHelmetStation()
    {
        currentPhase = FlowPhase.HelmetStation;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterStation(2);
        }

        // Switch ambient to underwater sounds
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetAmbientUnderwater();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHint("Point at the diving helmet and press trigger to zoom in.");
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Helmet station phase started.");

        if (autoAdvance)
        {
            phaseCoroutine = StartCoroutine(PhaseTimer(helmetStationDuration, () => BeginOutro()));
        }
    }

    private void BeginOutro()
    {
        currentPhase = FlowPhase.Outro;

        // Switch ambient back to port
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetAmbientPort();
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Outro phase started.");

        phaseCoroutine = StartCoroutine(PhaseTimer(outroDuration, () => CompleteFlow()));
    }

    private void CompleteFlow()
    {
        currentPhase = FlowPhase.Complete;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteSession();
        }

        if (enableDebugLog)
            Debug.Log("[ExperienceFlow] Experience flow complete.");
    }

    /// <summary>
    /// Manually advances to the next phase (for non-auto mode).
    /// </summary>
    public void AdvanceToNextPhase()
    {
        if (phaseCoroutine != null)
        {
            StopCoroutine(phaseCoroutine);
            phaseCoroutine = null;
        }

        switch (currentPhase)
        {
            case FlowPhase.Intro:
                BeginShipStation();
                break;
            case FlowPhase.ShipStation:
                BeginTimelineStation();
                break;
            case FlowPhase.TimelineStation:
                BeginHelmetStation();
                break;
            case FlowPhase.HelmetStation:
                BeginOutro();
                break;
            case FlowPhase.Outro:
                CompleteFlow();
                break;
        }
    }

    /// <summary>
    /// Resets the entire flow to the beginning.
    /// </summary>
    public void ResetFlow()
    {
        if (phaseCoroutine != null)
        {
            StopCoroutine(phaseCoroutine);
            phaseCoroutine = null;
        }

        currentPhase = FlowPhase.None;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
        }

        StartFlow();
    }

    /// <summary>
    /// Returns the estimated total duration of the experience.
    /// </summary>
    public float GetTotalDuration()
    {
        return introDuration + shipStationDuration + timelineStationDuration +
               helmetStationDuration + outroDuration;
    }

    /// <summary>
    /// Returns the name of the current phase for UI display.
    /// </summary>
    public string GetCurrentPhaseName()
    {
        switch (currentPhase)
        {
            case FlowPhase.Intro:
                return "Welcome";
            case FlowPhase.ShipStation:
                return "Ship Model";
            case FlowPhase.TimelineStation:
                return "Port-City Timeline";
            case FlowPhase.HelmetStation:
                return "Diving Helmet";
            case FlowPhase.Outro:
                return "Thank You";
            case FlowPhase.Complete:
                return "Complete";
            default:
                return "Not Started";
        }
    }

    private IEnumerator PhaseTimer(float duration, System.Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }
}
