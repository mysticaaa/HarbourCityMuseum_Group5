using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages all UI elements for the museum experience:
/// - Welcome/intro overlay
/// - Station status indicators
/// - Tooltips and labels
/// - Timer display
/// - Session complete screen
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private CanvasScaler canvasScaler;

    [Header("Welcome Panel")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Text welcomeTitleText;
    [SerializeField] private Text welcomeDescriptionText;
    [SerializeField] private string welcomeTitle = "Welcome to Harbour City Museum";
    [TextArea(3, 6)]
    [SerializeField] private string welcomeDescription =
        "Explore the maritime history of our port city. " +
        "Point your controller at an object and press the trigger to interact. " +
        "Your visit will take about 5 minutes.";

    [Header("HUD Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Text timerText;
    [SerializeField] private Text currentStationText;
    [SerializeField] private Text hintPromptText;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private Text tooltipText;
    [SerializeField] private float tooltipDefaultDuration = 3f;

    [Header("Session Complete")]
    [SerializeField] private GameObject completePanel;
    [SerializeField] private Text completeText;

    [Header("Crosshair")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Color crosshairNormalColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color crosshairHoverColor = new Color(1f, 0.85f, 0.3f, 0.8f);
    [SerializeField] private Color crosshairSelectColor = new Color(0.3f, 0.9f, 0.5f, 0.9f);

    private Coroutine activeTooltipCoroutine;

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
        ShowWelcome();
        SubscribeToGameManager();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStationEnter -= OnStationEnter;
            GameManager.Instance.OnStationExit -= OnStationExit;
            GameManager.Instance.OnSessionComplete -= OnSessionComplete;
            GameManager.Instance.OnTimerTick -= OnTimerTick;
        }
    }

    private void SubscribeToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStationEnter += OnStationEnter;
            GameManager.Instance.OnStationExit += OnStationExit;
            GameManager.Instance.OnSessionComplete += OnSessionComplete;
            GameManager.Instance.OnTimerTick += OnTimerTick;
        }
    }

    #region Welcome Panel

    /// <summary>
    /// Shows the welcome/intro overlay.
    /// </summary>
    public void ShowWelcome()
    {
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);

            if (welcomeTitleText != null)
                welcomeTitleText.text = welcomeTitle;

            if (welcomeDescriptionText != null)
                welcomeDescriptionText.text = welcomeDescription;
        }

        if (hudPanel != null)
            hudPanel.SetActive(false);

        if (completePanel != null)
            completePanel.SetActive(false);
    }

    /// <summary>
    /// Dismisses the welcome panel and starts the HUD.
    /// </summary>
    public void DismissWelcome()
    {
        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    #endregion

    #region HUD

    /// <summary>
    /// Updates the timer display.
    /// </summary>
    private void OnTimerTick(float remaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
        }
    }

    /// <summary>
    /// Updates the current station indicator.
    /// </summary>
    private void OnStationEnter(int stationIndex)
    {
        if (currentStationText != null && GameManager.Instance != null)
        {
            var stations = GameManager.Instance.Stations;
            if (stationIndex >= 0 && stationIndex < stations.Length && stations[stationIndex] != null)
            {
                currentStationText.text = stations[stationIndex].GetStatusText();
            }
        }

        ShowHint("Look around and point at objects to interact.");
    }

    private void OnStationExit(int stationIndex)
    {
        if (currentStationText != null)
        {
            currentStationText.text = "Moving to next station...";
        }
    }

    /// <summary>
    /// Shows a hint prompt.
    /// </summary>
    public void ShowHint(string hint)
    {
        if (hintPromptText != null)
        {
            hintPromptText.text = hint;
            hintPromptText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the hint prompt.
    /// </summary>
    public void HideHint()
    {
        if (hintPromptText != null)
        {
            hintPromptText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the crosshair color based on interaction state.
    /// </summary>
    public void SetCrosshairState(bool hovering, bool selected)
    {
        if (crosshair == null)
            return;

        if (selected)
        {
            crosshair.color = crosshairSelectColor;
        }
        else if (hovering)
        {
            crosshair.color = crosshairHoverColor;
        }
        else
        {
            crosshair.color = crosshairNormalColor;
        }
    }

    #endregion

    #region Tooltip

    /// <summary>
    /// Shows a tooltip with the given text for a duration.
    /// </summary>
    public void ShowTooltip(string text, float duration = 0f)
    {
        if (tooltipPanel == null || tooltipText == null)
            return;

        if (duration <= 0f)
            duration = tooltipDefaultDuration;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);

        if (activeTooltipCoroutine != null)
            StopCoroutine(activeTooltipCoroutine);

        activeTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(duration));
    }

    /// <summary>
    /// Hides the tooltip immediately.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }

        if (activeTooltipCoroutine != null)
        {
            StopCoroutine(activeTooltipCoroutine);
            activeTooltipCoroutine = null;
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        activeTooltipCoroutine = null;
    }

    #endregion

    #region Session Complete

    private void OnSessionComplete()
    {
        if (completePanel != null)
        {
            completePanel.SetActive(true);

            if (completeText != null)
            {
                completeText.text = "Thank you for visiting Harbour City Museum.\n" +
                    "Your feedback helps us improve the experience.";
            }
        }

        if (hudPanel != null)
            hudPanel.SetActive(false);
    }

    #endregion
}
