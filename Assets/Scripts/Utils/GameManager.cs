using UnityEngine;
using System;

/// <summary>
/// Central game manager for the Harbour City Museum XR experience.
/// Manages overall session state, station progression, and cross-system coordination.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Session Configuration")]
    [Tooltip("Maximum session duration in seconds (5 minutes = 300)")]
    [SerializeField] private float maxSessionDuration = 300f;

    [Header("Station References")]
    [SerializeField] private StationBase[] stations;
    [SerializeField] private string[] stationNames = {
        "Ship Model",
        "Port-City Timeline",
        "Diving Helmet"
    };

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    // Session state
    private float sessionStartTime;
    private int currentStationIndex = -1;
    private bool sessionActive = false;
    private bool sessionComplete = false;

    // Events
    public event Action<int> OnStationEnter;
    public event Action<int> OnStationExit;
    public event Action OnSessionStart;
    public event Action OnSessionComplete;
    public event Action<float> OnTimerTick;

    // Properties
    public bool IsSessionActive => sessionActive;
    public bool IsSessionComplete => sessionComplete;
    public int CurrentStationIndex => currentStationIndex;
    public float SessionElapsed => sessionActive ? Time.time - sessionStartTime : 0f;
    public float SessionRemaining => Mathf.Max(0f, maxSessionDuration - SessionElapsed);
    public StationBase[] Stations => stations;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartSession();
    }

    private void Update()
    {
        if (sessionActive && !sessionComplete)
        {
            float remaining = SessionRemaining;
            OnTimerTick?.Invoke(remaining);

            if (remaining <= 0f)
            {
                CompleteSession();
            }

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[GameManager] Time remaining: {remaining:F1}s | Station: {currentStationIndex}");
            }
        }
    }

    /// <summary>
    /// Begins the museum visit session.
    /// </summary>
    public void StartSession()
    {
        if (sessionActive)
        {
            Debug.LogWarning("[GameManager] Session already active.");
            return;
        }

        sessionStartTime = Time.time;
        sessionActive = true;
        sessionComplete = false;

        if (enableDebugLog)
            Debug.Log("[GameManager] Session started.");

        OnSessionStart?.Invoke();
    }

    /// <summary>
    /// Marks the session as complete and notifies all systems.
    /// </summary>
    public void CompleteSession()
    {
        if (!sessionActive || sessionComplete)
            return;

        sessionComplete = true;
        sessionActive = false;

        if (enableDebugLog)
            Debug.Log("[GameManager] Session complete.");

        OnSessionComplete?.Invoke();
    }

    /// <summary>
    /// Called when a visitor enters a station trigger zone.
    /// </summary>
    public void EnterStation(int index)
    {
        if (index < 0 || index >= stations.Length)
        {
            Debug.LogError($"[GameManager] Invalid station index: {index}");
            return;
        }

        if (currentStationIndex == index)
            return;

        if (currentStationIndex >= 0 && currentStationIndex < stations.Length)
        {
            stations[currentStationIndex].OnStationExited();
            OnStationExit?.Invoke(currentStationIndex);
        }

        currentStationIndex = index;
        stations[index].OnStationEntered();

        if (enableDebugLog)
            Debug.Log($"[GameManager] Entered station {index}: {stationNames[index]}");

        OnStationEnter?.Invoke(index);
    }

    /// <summary>
    /// Resets the entire session (for staff reset between visitors).
    /// </summary>
    public void ResetSession()
    {
        sessionActive = false;
        sessionComplete = false;
        currentStationIndex = -1;

        foreach (var station in stations)
        {
            if (station != null)
                station.ResetStation();
        }

        if (enableDebugLog)
            Debug.Log("[GameManager] Session reset.");

        StartSession();
    }

    /// <summary>
    /// Checks whether all stations have been visited.
    /// </summary>
    public bool AllStationsVisited()
    {
        foreach (var station in stations)
        {
            if (station != null && !station.HasBeenVisited)
                return false;
        }
        return true;
    }
}
