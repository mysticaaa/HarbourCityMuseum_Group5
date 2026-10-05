using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Port-City Story Timeline Station — a timeline wall presenting
/// maritime work, migration history and everyday life in the port city.
/// 
/// Visitors connect the objects to the people who used them via
/// opt-in exploration: selecting a timeline entry expands its details.
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class TimelineStation : StationBase
{
    [System.Serializable]
    public struct TimelineEntry
    {
        public string year;
        public string title;
        [TextArea(2, 4)] public string description;
        public Sprite image;
        public Color entryColor;
    }

    [Header("Timeline Data")]
    [SerializeField] private TimelineEntry[] entries;

    [Header("UI References")]
    [SerializeField] private GameObject timelinePanel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private UnityEngine.UI.Text detailYearText;
    [SerializeField] private UnityEngine.UI.Text detailTitleText;
    [SerializeField] private UnityEngine.UI.Text detailDescriptionText;
    [SerializeField] private UnityEngine.UI.Image detailImage;

    [Header("Timeline Configuration")]
    [SerializeField] private float entrySpacing = 120f;
    [SerializeField] private float panelOpenDuration = 0.3f;

    // State
    private bool isPanelVisible = false;
    private int currentEntryIndex = -1;
    private List<GameObject> spawnedEntries = new List<GameObject>();

    protected override void HandleStationInteraction()
    {
        if (!isPanelVisible)
        {
            ShowTimelinePanel();
        }
        else
        {
            // Cycle to next entry when selecting the wall
            CycleToNextEntry();
        }
    }

    /// <summary>
    /// Populates the timeline with entries (call from Start or Awake).
    /// </summary>
    private void Start()
    {
        PopulateTimeline();
    }

    /// <summary>
    /// Creates UI elements for each timeline entry.
    /// </summary>
    private void PopulateTimeline()
    {
        if (entries == null || entries.Length == 0 || entryPrefab == null || entryContainer == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            GameObject entryObj = Instantiate(entryPrefab, entryContainer);
            entryObj.name = $"TimelineEntry_{i}_{entries[i].year}";

            // Set entry text
            var yearText = entryObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (yearText != null)
            {
                yearText.text = $"{entries[i].year}\n{entries[i].title}";
            }

            // Position entry
            RectTransform rt = entryObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                float xOffset = (i - (entries.Length - 1) / 2f) * entrySpacing;
                rt.anchoredPosition = new Vector2(xOffset, 0);
            }

            // Store index for click handling
            var entryButton = entryObj.GetComponent<UnityEngine.UI.Button>();
            if (entryButton != null)
            {
                int capturedIndex = i;
                entryButton.onClick.AddListener(() => SelectEntry(capturedIndex));
            }

            spawnedEntries.Add(entryObj);
        }
    }

    /// <summary>
    /// Shows the main timeline panel.
    /// </summary>
    private void ShowTimelinePanel()
    {
        if (timelinePanel != null)
        {
            timelinePanel.SetActive(true);
            isPanelVisible = true;
            Debug.Log("[TimelineStation] Timeline panel opened.");
        }
    }

    /// <summary>
    /// Hides the timeline panel.
    /// </summary>
    public void HideTimelinePanel()
    {
        if (timelinePanel != null)
        {
            timelinePanel.SetActive(false);
        }

        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        isPanelVisible = false;
        currentEntryIndex = -1;
    }

    /// <summary>
    /// Selects a specific timeline entry by index.
    /// </summary>
    public void SelectEntry(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length)
            return;

        currentEntryIndex = index;
        ShowEntryDetail(index);
    }

    /// <summary>
    /// Cycles to the next timeline entry.
    /// </summary>
    private void CycleToNextEntry()
    {
        if (entries == null || entries.Length == 0)
            return;

        int nextIndex = (currentEntryIndex + 1) % entries.Length;
        SelectEntry(nextIndex);
    }

    /// <summary>
    /// Shows the detail panel for a specific entry.
    /// </summary>
    private void ShowEntryDetail(int index)
    {
        if (detailPanel == null)
            return;

        detailPanel.SetActive(true);

        var entry = entries[index];

        if (detailYearText != null)
            detailYearText.text = entry.year;

        if (detailTitleText != null)
            detailTitleText.text = entry.title;

        if (detailDescriptionText != null)
            detailDescriptionText.text = entry.description;

        if (detailImage != null && entry.image != null)
        {
            detailImage.sprite = entry.image;
            detailImage.gameObject.SetActive(true);
        }
        else if (detailImage != null)
        {
            detailImage.gameObject.SetActive(false);
        }

        Debug.Log($"[TimelineStation] Showing entry: {entry.year} — {entry.title}");
    }

    public override void ResetStation()
    {
        base.ResetStation();
        HideTimelinePanel();
    }

    public override string GetStatusText()
    {
        string baseText = base.GetStatusText();
        if (isPanelVisible && currentEntryIndex >= 0 && entries != null)
        {
            if (currentEntryIndex < entries.Length)
            {
                return $"{baseText} | {entries[currentEntryIndex].year}: {entries[currentEntryIndex].title}";
            }
        }
        return baseText;
    }
}
