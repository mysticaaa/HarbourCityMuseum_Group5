using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MuseumQuickMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuButton;
    [SerializeField] private GameObject expandedList;
    [SerializeField] private GameObject volumeControls;
    [SerializeField] private Text volumePercent;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private MuseumControllerTutorial tutorial;

    private const string OpenHint = "Press M (B on Quest) on the right controller\nto open the menu.";
    private const string SelectHint = "Point the controller ray at a menu button\nand press the trigger to select it.";
    private const string CloseHint = "Press M (B on Quest) again to hide the menu.";

    private enum MenuState
    {
        Tutorial,
        AwaitingOpen,
        AwaitingSelection,
        AwaitingClose,
        Hidden
    }

    private InputAction menuToggle;
    private MenuState state;
    private Text menuHint;

    private void Awake()
    {
        menuToggle = new InputAction("Toggle Museum Menu", InputActionType.Button);
        menuToggle.AddBinding("<XRController>{RightHand}/{MenuButton}");
        menuToggle.AddBinding("<XRController>{RightHand}/{SecondaryButton}");
        menuToggle.AddBinding("<Keyboard>/m");

        BuildHint();
        CloseList();
        SetHint(null);
    }

    private void OnEnable()
    {
        tutorial.Completed += OnTutorialCompleted;
        volumeSlider.onValueChanged.AddListener(ApplyVolume);
        menuToggle.Enable();
        menuHint.gameObject.SetActive(!string.IsNullOrEmpty(menuHint.text));
    }

    private void OnDisable()
    {
        menuToggle.Disable();
        tutorial.Completed -= OnTutorialCompleted;
        volumeSlider.onValueChanged.RemoveListener(ApplyVolume);
        menuHint.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        menuToggle.Dispose();
        if (menuHint)
            Destroy(menuHint.gameObject);
    }

    private void Update()
    {
        if (!menuToggle.WasPressedThisFrame())
            return;

        switch (state)
        {
            case MenuState.AwaitingOpen:
            case MenuState.Hidden:
                state = MenuState.AwaitingSelection;
                Open();
                SetHint(SelectHint);
                break;
            case MenuState.AwaitingSelection:
            case MenuState.AwaitingClose:
                state = MenuState.Hidden;
                CloseList();
                tutorial.gameObject.SetActive(false);
                SetHint(null);
                break;
        }
    }

    private void OnTutorialCompleted()
    {
        if (state != MenuState.Tutorial)
            return;

        state = MenuState.AwaitingOpen;
        SetHint(OpenHint);
    }

    public void Open()
    {
        menuButton.SetActive(false);
        expandedList.SetActive(true);
    }

    public void OpenTutorial()
    {
        OnMenuSelection();
        CloseList();
        tutorial.RestartTutorial();
    }

    public void ToggleVolume()
    {
        OnMenuSelection();
        volumeControls.SetActive(!volumeControls.activeSelf);
        if (volumeControls.activeSelf)
            RefreshVolume();
    }

    private void CloseList()
    {
        menuButton.SetActive(false);
        volumeControls.SetActive(false);
        expandedList.SetActive(false);
    }

    private void OnMenuSelection()
    {
        if (state != MenuState.AwaitingSelection)
            return;

        state = MenuState.AwaitingClose;
        SetHint(CloseHint);
    }

    private void SetHint(string message)
    {
        menuHint.text = message;
        menuHint.gameObject.SetActive(message != null);
    }

    private void BuildHint()
    {
        var hintObject = new GameObject("Menu Guidance", typeof(RectTransform), typeof(Text), typeof(Outline));
        hintObject.SetActive(false);
        hintObject.layer = gameObject.layer;

        // Use the head-follow canvas so the hint stays visible while the tutorial is hidden.
        var rect = (RectTransform)hintObject.transform;
        rect.SetParent(transform.parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, 424);
        rect.sizeDelta = new Vector2(1270, 148);

        menuHint = hintObject.GetComponent<Text>();
        menuHint.font = tutorial.PromptFont;
        menuHint.fontSize = 49;
        menuHint.fontStyle = FontStyle.Bold;
        menuHint.alignment = TextAnchor.MiddleCenter;
        menuHint.color = new Color(.92549f, .96471f, .96863f);
        menuHint.supportRichText = false;
        menuHint.raycastTarget = false;

        var outline = hintObject.GetComponent<Outline>();
        outline.effectColor = new Color(.02f, .045f, .07f);
        outline.effectDistance = new Vector2(3, -3);
    }

    private void ApplyVolume(float value)
    {
        int percent = Mathf.RoundToInt(value);
        AudioListener.volume = percent / 100f;
        volumePercent.text = $"{percent}%";
    }

    private void RefreshVolume()
    {
        int percent = Mathf.RoundToInt(AudioListener.volume * 100);
        volumeSlider.SetValueWithoutNotify(percent);
        volumePercent.text = $"{percent}%";
    }
}
