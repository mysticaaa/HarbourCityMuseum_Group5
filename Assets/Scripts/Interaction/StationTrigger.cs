using UnityEngine;

/// <summary>
/// Attach to trigger zone colliders at each station.
/// When the player enters the zone, it notifies the GameManager
/// to activate the corresponding station.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
[RequireComponent(typeof(Collider))]
public class StationTrigger : MonoBehaviour
{
    [Tooltip("Index of the station this trigger activates (0=ship, 1=timeline, 2=helmet)")]
    [SerializeField] private int stationIndex = 0;

    [Tooltip("Show a hint message when entering this zone")]
    [SerializeField] private string entryHint = "";

    [Tooltip("Play this sound when entering")]
    [SerializeField] private AudioClip entrySound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            Debug.LogWarning($"[StationTrigger] Collider on {gameObject.name} was not set to trigger. Fixed.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("MainCamera"))
            return;

        if (enableDebugLog)
            Debug.Log($"[StationTrigger] Player entered station {stationIndex}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterStation(stationIndex);
        }

        if (!string.IsNullOrEmpty(entryHint) && UIManager.Instance != null)
        {
            UIManager.Instance.ShowHint(entryHint);
        }

        if (entrySound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(entrySound);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("MainCamera"))
            return;

        if (enableDebugLog)
            Debug.Log($"[StationTrigger] Player exited station {stationIndex}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHint();
        }
    }

    /// <summary>
    /// Sets the station index (for runtime configuration).
    /// </summary>
    public void SetStationIndex(int index)
    {
        stationIndex = index;
    }
}
