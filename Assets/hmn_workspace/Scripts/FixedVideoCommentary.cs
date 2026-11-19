using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public sealed class FixedVideoCommentary : MonoBehaviour
{
    [SerializeField, TextArea(3, 8)] private string commentary;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Vector3 panelOffset = new Vector3(1.48f, 0f, -0.04f);

    [SerializeField] private Vector2 panelSize = new Vector2(440f, 280f);
    [SerializeField] private Color backgroundColor = new Color32(36, 70, 90, 255);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool useCaptionTypography;

    private VideoPlayer player;
    private GameObject panel;

    private void OnEnable()
    {
        player = GetComponent<VideoPlayer>();
        player.started += Show;
        player.loopPointReached += OnEnded;
        player.errorReceived += OnError;
        if (player.isPlaying) Show(player);
    }

    private void OnDisable()
    {
        if (player == null) return;
        player.started -= Show;
        player.loopPointReached -= OnEnded;
        player.errorReceived -= OnError;
        Hide();
    }

    private void Show(VideoPlayer source)
    {
        if (string.IsNullOrWhiteSpace(commentary)) return;
        if (panel == null) BuildPanel();
        panel.SetActive(true);
    }

    private void OnEnded(VideoPlayer source)
    {
        if (!source.isLooping) Hide();
    }

    private void OnError(VideoPlayer source, string message) => Hide();

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void BuildPanel()
    {
        panel = new GameObject("Video Commentary Panel", typeof(RectTransform), typeof(Canvas), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(transform, false);
        rect.localPosition = panelOffset;
        rect.localScale = Vector3.one * 0.0025f;
        rect.sizeDelta = panelSize;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var background = panel.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        var label = new GameObject("Commentary", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24f, 20f);
        label.rectTransform.offsetMax = new Vector2(-24f, -20f);
        if (font != null) label.font = font;
        label.text = commentary;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = true;
        label.fontSizeMin = 20;
        label.fontSizeMax = 34;
        if (useCaptionTypography)
        {
            label.fontStyle = FontStyles.Normal;
            label.fontWeight = FontWeight.Regular;
            label.enableAutoSizing = false;
            label.fontSize = 36f;
            label.lineSpacing = 8f;
            label.enableWordWrapping = true;
            // Use a private material so the caption does not change other TMP labels.
            label.outlineWidth = 0f;
        }
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.richText = false;
    }
}
