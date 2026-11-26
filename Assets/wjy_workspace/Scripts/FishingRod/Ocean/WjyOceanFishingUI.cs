using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyOceanFishingUI : MonoBehaviour
    {
        private const int RainbowFrames = 48;
        private string rodControls = "Hold the <b>Grip</b> button the entire time\nyou’re using the fishing rod.\nUse the <b>Trigger</b> to cast and reel in";
        private static readonly string[] Captions = MakeCaptions("Start the Fishing Experience",
            "On the raft: GRIP to hold\nTRIGGER: hold, swing, release\n<b>", "</b>\n(7 Surprises!)");
        private static readonly string[] GalleryCaptions = MakeCaptions("Discover 7 different catches!", "<b>", "</b>");
        private string[] activeCaptions;
        private Text pedestalLabel;
        private string originalText;
        private GameObject startPanel, returnPanel, fallbackLabel;
        private Transform rodHint;
        private Renderer legacyRenderer;
        private bool legacyWasVisible, atSea;

        public void BuildMuseum(Transform exhibitRoot, Text label, Camera view, UnityAction enter)
        {
            if (startPanel) return;
            if (!label)
            {
                var face = exhibitRoot.Find("FishingInstructions");
                var fallback = MakeCanvas("Ocean Fishing Instructions", exhibitRoot, view, new Vector2(830, 500));
                fallbackLabel = fallback.gameObject;
                fallback.localPosition = face ? face.localPosition : new Vector3(0f, .29f, .366f);
                fallback.localRotation = face ? face.localRotation : Quaternion.Euler(0f, 180f, 0f);
                fallback.localScale = Vector3.one * .001f;
                fallback.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = false;
                label = AddText(fallback, "Instructions", "", Vector2.zero, new Vector2(800, 470), 54);
                label.color = new Color(.14f, .12f, .08f);
                if (face && face.TryGetComponent(out legacyRenderer))
                {
                    legacyWasVisible = legacyRenderer.enabled;
                    legacyRenderer.enabled = false;
                }
            }
            pedestalLabel = label;
            originalText = label.text;
            var session = exhibitRoot.GetComponent<WjyOceanFishingSession>();
            if (session && session.PressTriggerToCast)
                rodControls = "Keep <b>Grip</b> held.\nAim and tap <b>Trigger</b> once to cast.\nTap <b>Trigger</b> again to reel in.";
            if (session && session.UseMuseumExhibitLayout)
            {
                activeCaptions = GalleryCaptions;
                BuildGalleryEntrance(exhibitRoot, view, enter);
            }
            else
            {
                activeCaptions = Captions;
                startPanel = MakeButton(exhibitRoot, view, "Start Fishing", "Point and press Trigger", enter,
                    new Vector3(-.73f, .53f, .32f), Quaternion.Euler(0f, 175f, 0f));
            }
            ShowOcean(atSea);
        }

        private void BuildGalleryEntrance(Transform exhibit, Camera view, UnityAction enter)
        {
            // The scene places FishingInstructions on the face opposite the four screens.
            var title = (RectTransform)pedestalLabel.canvas.transform;
            title.sizeDelta = new Vector2(655, 85);
            pedestalLabel.rectTransform.offsetMin = Vector2.zero;
            pedestalLabel.rectTransform.offsetMax = Vector2.zero;
            pedestalLabel.color = Color.white;
            pedestalLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            pedestalLabel.resizeTextMinSize = 22;
            pedestalLabel.resizeTextMaxSize = 35;
            var outline = pedestalLabel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, .96f, .9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var button = MakeCanvas("Go Fishing", exhibit, view, new Vector2(240, 96));
            button.localScale = title.localScale;
            button.rotation = title.rotation;
            button.position = GalleryButtonPosition(title, button, onRight: false);
            startPanel = button.gameObject;
            AddButton(button, enter, new Color(.02f, .40f, .15f), new Color(.02f, .57f, .22f),
                new Color(.01f, .29f, .10f));
            var label = AddText(button, "Label", "Start fishing", new Vector2(17, 0), new Vector2(172, 42), 29);
            label.fontStyle = FontStyle.Bold;
            // Two strokes form a play chevron without depending on a font's symbol glyphs.
            for (int side = -1; side <= 1; side += 2)
            {
                var stroke = new GameObject("Play chevron", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                stroke.gameObject.layer = button.gameObject.layer;
                stroke.raycastTarget = false;
                stroke.rectTransform.SetParent(button, false);
                stroke.rectTransform.sizeDelta = new Vector2(22, 5);
                stroke.rectTransform.anchoredPosition = new Vector2(-82, side * 6);
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, -side * 35f);
            }

            // The motion transform contains the shaft and guides. Its grandparent is the
            // stationary exhibit, where the same factory used by the helmet attaches its hint.
            var rod = exhibit.GetComponentInChildren<WjyFishingRodCatchInteraction>();
            ExhibitInspectionHint.Create(rod.DisplayModel, rodControls);
            rodHint = exhibit.GetComponentInChildren<ExhibitInspectionHint>().transform;
            rodHint.GetComponent<TextMesh>().richText = true;
            rodHint.position -= exhibit.up * .055f;
        }

        internal static Vector3 GalleryButtonPosition(Transform title, RectTransform button, bool onRight)
        {
            // Align the two outer button edges to a 462-unit row centred on the title.
            const float rowHalfWidth = 231f;
            float halfRow = title.TransformVector(Vector3.right * rowHalfWidth).magnitude;
            float halfButton = button.TransformVector(Vector3.right * (button.rect.width * .5f)).magnitude;
            float offset = halfRow - halfButton;
            return title.position - title.up * .107f + title.right * (onRight ? offset : -offset);
        }

        public void BuildRaft(Transform raftOrigin, Camera view, UnityAction leave)
        {
            if (returnPanel) return;
            var position = new Vector3(-1.05f, 1.02f, .85f);
            var direction = new Vector3(position.x, 0f, position.z);
            returnPanel = MakeButton(raftOrigin, view, "Return to Museum", "Point and press Trigger", leave,
                position, Quaternion.LookRotation(direction));
            var guide = (RectTransform)new GameObject("Raft controls", typeof(RectTransform), typeof(Image)).transform;
            guide.gameObject.layer = returnPanel.layer;
            guide.SetParent(returnPanel.transform, false);
            guide.anchoredPosition = new Vector2(0, -159);
            guide.sizeDelta = new Vector2(480, 154);
            var backing = guide.GetComponent<Image>();
            backing.color = new Color(.02f, .09f, .13f, .94f);
            backing.raycastTarget = false;
            AddText(guide, "Fishing controls", rodControls, Vector2.zero, new Vector2(462, 145), 28);
            ShowOcean(atSea);
        }

        public void ShowOcean(bool onOcean)
        {
            atSea = onOcean;
            if (startPanel) startPanel.SetActive(!onOcean);
            if (rodHint) rodHint.gameObject.SetActive(!onOcean);
            if (returnPanel) returnPanel.SetActive(onOcean);
            if (!onOcean) UpdateCaption();
        }

        private void Update()
        {
            if (!atSea && pedestalLabel && pedestalLabel.isActiveAndEnabled) UpdateCaption();
        }

        private void UpdateCaption()
        {
            if (!pedestalLabel) return;
            int frame = Mathf.FloorToInt(Mathf.Repeat(Time.unscaledTime, 4f) * (RainbowFrames / 4f));
            string caption = activeCaptions[frame];
            if (!ReferenceEquals(caption, pedestalLabel.text)) pedestalLabel.text = caption;
        }

        private static string[] MakeCaptions(string title, string prefix, string suffix)
        {
            var frames = new string[RainbowFrames];
            var text = new StringBuilder(512);
            for (int frame = 0; frame < frames.Length; frame++)
            {
                text.Clear();
                text.Append(prefix);
                float phase = (float)frame / frames.Length;
                float value = .48f + .09f * Mathf.Sin(2f * Mathf.PI * phase);
                for (int i = 0; i < title.Length; i += 3)
                {
                    var colour = Color.HSVToRGB(Mathf.Repeat((float)i / title.Length - phase, 1f), .9f, value);
                    text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(colour)).Append('>');
                    text.Append(title, i, Mathf.Min(3, title.Length - i)).Append("</color>");
                }
                frames[frame] = text.Append(suffix).ToString();
            }
            return frames;
        }

        private static GameObject MakeButton(Transform parent, Camera view, string title, string hint, UnityAction action,
            Vector3 position, Quaternion rotation)
        {
            var rect = MakeCanvas(title, parent, view, new Vector2(480, 146));
            rect.localPosition = position;
            rect.localRotation = rotation;
            rect.localScale = Vector3.one * .00106f;

            AddButton(rect, action, new Color(.025f, .18f, .23f), new Color(.02f, .35f, .38f),
                new Color(.02f, .27f, .30f));
            var heading = AddText(rect, "Action", title, new Vector2(0, 21), new Vector2(458, 70), 48);
            heading.fontStyle = FontStyle.Bold;
            var detail = AddText(rect, "Control", hint, new Vector2(0, -35), new Vector2(458, 42), 29);
            detail.color = new Color(.78f, .96f, .92f);
            return rect.gameObject;
        }

        internal static void AddButton(RectTransform rect, UnityAction action, Color normal, Color hover, Color pressed)
        {
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colours = button.colors;
            colours.normalColor = normal;
            colours.highlightedColor = colours.selectedColor = hover;
            colours.pressedColor = pressed;
            button.colors = colours;
            button.onClick.AddListener(action);
        }

        internal static RectTransform MakeCanvas(string name, Transform parent, Camera view, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            return rect;
        }

        internal static Text AddText(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.gameObject.layer = parent.gameObject.layer;
            var rect = text.rectTransform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = fontSize / 2;
            text.resizeTextMaxSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            if (pedestalLabel) pedestalLabel.text = originalText;
            if (legacyRenderer) legacyRenderer.enabled = legacyWasVisible;
            if (startPanel) Destroy(startPanel);
            if (returnPanel) Destroy(returnPanel);
            if (fallbackLabel) Destroy(fallbackLabel);
            if (rodHint) Destroy(rodHint.gameObject);
        }
    }
}
