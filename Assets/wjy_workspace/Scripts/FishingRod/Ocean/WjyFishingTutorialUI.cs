using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyFishingTutorialUI : MonoBehaviour
    {
        internal enum Step { Hidden, Available, Grip, HoldTrigger, Swing, Flying, Reel, Reeling, Complete }

        internal static readonly Color GripColour = new Color(.38f, .93f, .70f);
        internal static readonly Color TriggerColour = new Color(1f, .77f, .39f);
        private static readonly Color Paper = new Color(.94f, .97f, .95f);
        private static readonly Color Navy = new Color(.035f, .105f, .15f);
        private static readonly Color Teal = new Color(.06f, .39f, .33f);
        private Transform title;
        private RectTransform canvas, lesson;
        private GameObject replayButton, skipButton, finishButtons, diagramGroup;
        private Text heading, instruction, correction, gripLabel, triggerLabel, skipLabel;
        private WjyFishingControllerDiagram diagram;

        internal void Build(Transform pedestalTitle, Camera view, UnityAction skip, UnityAction replay, UnityAction goFishing)
        {
            if (canvas) return;
            title = pedestalTitle;
            canvas = WjyOceanFishingUI.MakeCanvas("Fishing casting tutorial", transform, view, new Vector2(670, 314));
            canvas.localScale = Vector3.one * .00118f;
            replayButton = ButtonAt(canvas, "How to Use Rod", Vector2.zero, new Vector2(176, 68), replay, Teal);
            replayButton.GetComponentInChildren<Text>().rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 34f);
            lesson = Rect(canvas, "Casting lesson", Vector2.zero, canvas.sizeDelta);
            var background = lesson.gameObject.AddComponent<Image>();
            background.color = Navy;
            background.raycastTarget = false;
            heading = Label(lesson, "Heading", "", -66, 106, 485, 61, 39);
            heading.fontStyle = FontStyle.Bold;
            skipButton = ButtonAt(lesson, "Skip", new Vector2(236, 103), new Vector2(144, 68), skip, new Color(.18f, .30f, .34f));
            skipLabel = skipButton.GetComponentInChildren<Text>();

            var illustration = Rect(lesson, "Controller diagram", new Vector2(-211, -24), new Vector2(196, 180));
            diagramGroup = illustration.gameObject;
            diagram = illustration.gameObject.AddComponent<WjyFishingControllerDiagram>();
            diagram.raycastTarget = false;
            triggerLabel = Label(illustration, "Trigger", "Trigger", 57, 36, 72, 29, 22);
            gripLabel = Label(illustration, "Grip", "Grip", 57, -35, 72, 29, 22);
            instruction = Label(lesson, "Instruction", "", 105, 8, 400, 124, 32);
            correction = Label(lesson, "Correction", "", 105, -94, 400, 75, 26);
            correction.color = TriggerColour;
            var choices = Rect(lesson, "After practice", new Vector2(0, -89), new Vector2(612, 68));
            finishButtons = choices.gameObject;
            ButtonAt(choices, "Go Fishing", new Vector2(-145, 0), new Vector2(270, 66), goFishing, Teal);
            ButtonAt(choices, "Practise Again", new Vector2(145, 0), new Vector2(270, 66), replay, new Color(.18f, .30f, .34f));
            Show(Step.Hidden);
        }

        internal void Show(Step step, string feedback = null, bool leftHand = false)
        {
            if (!canvas) return;
            canvas.gameObject.SetActive(step != Step.Hidden);
            if (step == Step.Hidden) return;
            bool compact = step == Step.Available;
            // The replay button fits beside the existing green entry button. The lesson
            // sits to the side at reading height so the rod and its collider stay clear.
            canvas.rotation = compact ? title.rotation : Quaternion.AngleAxis(23f, title.up) * title.rotation;
            canvas.position = compact
                ? WjyOceanFishingUI.GalleryButtonPosition(title, (RectTransform)replayButton.transform, onRight: true) - title.forward * .005f
                : title.position + title.right * .76f + title.up * .94f;
            replayButton.SetActive(compact);
            lesson.gameObject.SetActive(!compact);
            if (compact) return;

            bool finished = step == Step.Complete;
            diagramGroup.SetActive(!finished);
            skipButton.SetActive(!finished);
            skipLabel.text = step == Step.Grip ? "Skip" : "Skip\n<size=22>Use free hand</size>";
            finishButtons.SetActive(finished);
            instruction.rectTransform.anchoredPosition = finished ? new Vector2(0, 9) : new Vector2(105, 8);
            instruction.rectTransform.sizeDelta = finished ? new Vector2(610, 110) : new Vector2(400, 124);
            correction.gameObject.SetActive(!finished && !string.IsNullOrEmpty(feedback));
            correction.text = feedback ?? "";
            switch (step)
            {
                case Step.Grip:
                    heading.text = "1 / 3   Pick up the rod";
                    instruction.text = "Point at the rod. Squeeze and hold <b>Grip</b> with your middle finger.";
                    break;
                case Step.HoldTrigger:
                    heading.text = "2 / 3   Cast";
                    instruction.text = "Keep <b>Grip</b> held. Hold <b>Trigger</b> with your index finger.";
                    break;
                case Step.Swing:
                    heading.text = "2 / 3   Cast";
                    instruction.text = "Swing forward. Release <b>Trigger</b> during the swing. Keep <b>Grip</b> held.";
                    break;
                case Step.Flying:
                    heading.text = "2 / 3   Casting";
                    instruction.text = "Keep holding <b>Grip</b> and watch the hook land.";
                    break;
                case Step.Reel:
                    heading.text = "3 / 3   Reel in";
                    instruction.text = "Keep <b>Grip</b> held. Press <b>Trigger</b> once to reel in.";
                    break;
                case Step.Reeling:
                    heading.text = "3 / 3   Reeling in";
                    instruction.text = "Keep holding <b>Grip</b> as the line comes back.";
                    break;
                case Step.Complete:
                    heading.text = "Nice cast!";
                    instruction.text = "Release <b>Grip</b> to choose, or point with your free hand.";
                    break;
            }
            bool showTrigger = step == Step.HoldTrigger || step == Step.Swing || step == Step.Reel;
            triggerLabel.color = showTrigger ? TriggerColour : Paper;
            gripLabel.color = GripColour;
            diagram.SetPose(showTrigger, step == Step.Swing, leftHand);
        }

        private static Text Label(RectTransform parent, string name, string value, float x, float y, float w, float h, int fontSize)
        {
            var label = WjyOceanFishingUI.AddText(parent, name, value, new Vector2(x, y), new Vector2(w, h), fontSize);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Paper;
            return label;
        }

        private static RectTransform Rect(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static GameObject ButtonAt(RectTransform parent, string name, Vector2 position, Vector2 size, UnityAction click, Color colour)
        {
            var rect = Rect(parent, name, position, size);
            WjyOceanFishingUI.AddButton(rect, click, colour, Color.Lerp(colour, Color.white, .16f), colour * .84f);
            var text = WjyOceanFishingUI.AddText(rect, "Label", name, Vector2.zero, size - new Vector2(12, 6), 28);
            text.fontStyle = FontStyle.Bold;
            return rect.gameObject;
        }

        private void OnDestroy()
        {
            if (canvas) Destroy(canvas.gameObject);
        }
    }

    // Side view of a ringless touch controller. Labels remain readable with either hand.
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class WjyFishingControllerDiagram : MaskableGraphic
    {
        private bool triggerActive, showSwing, leftHand;
        private static readonly Color Shell = new Color(.84f, .89f, .89f);
        private static readonly Color Face = new Color(.25f, .34f, .37f);
        private static readonly Color Inactive = new Color(.55f, .63f, .65f);

        internal void SetPose(bool showTrigger, bool cast, bool left)
        {
            if (triggerActive == showTrigger && showSwing == cast && leftHand == left) return;
            triggerActive = showTrigger;
            showSwing = cast;
            leftHand = left;
            SetVerticesDirty();
        }

        private Vector2 Mirrored(float x, float y) => new Vector2(-37f + (leftHand ? -x : x), y);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color trigger = triggerActive ? WjyFishingTutorialUI.TriggerColour : Inactive;
            var palm = Mirrored(-8, -18);
            var head = Mirrored(-1, 37);
            Stroke(vh, palm + new Vector2(leftHand ? -12 : 12, -38), head, 35f, Shell);
            Oval(vh, Mirrored(4, -55), 17.5f, 12f, Shell);
            Oval(vh, head, 35f, 22f, Shell);
            Oval(vh, head + Vector2.up * 9f, 32f, 13f, Face);
            Oval(vh, Mirrored(-9, 54), 9f, 5f, Inactive);
            var index = Mirrored(-24, 22);
            var grip = Mirrored(-10, -16);
            Oval(vh, index, 9f, 11f, trigger);
            Oval(vh, grip, 6f, 16f, WjyFishingTutorialUI.GripColour);
            Stroke(vh, index, new Vector2(16, 35), 2.2f, trigger);
            Stroke(vh, grip, new Vector2(16, -35), 2.2f, WjyFishingTutorialUI.GripColour);
            if (!showSwing) return;
            // A rising arc shows the direction of the forward swing.
            var a = new Vector2(-93, 53);
            var b = new Vector2(-82, 72);
            var c = new Vector2(-58, 84);
            var d = new Vector2(-21, 88);
            Stroke(vh, a, b, 3f, trigger);
            Stroke(vh, b, c, 3f, trigger);
            Stroke(vh, c, d, 3f, trigger);
            Stroke(vh, d, d + new Vector2(-11, 5), 3f, trigger);
            Stroke(vh, d, d + new Vector2(-10, -7), 3f, trigger);
        }

        private static void Oval(VertexHelper vh, Vector2 centre, float rx, float ry, Color colour)
        {
            const int edges = 24;
            int start = vh.currentVertCount;
            vh.AddVert(centre, colour, Vector2.zero);
            for (int i = 0; i < edges; i++)
            {
                float angle = i * (2f * Mathf.PI / edges);
                vh.AddVert(centre + new Vector2(rx * Mathf.Cos(angle), ry * Mathf.Sin(angle)), colour, Vector2.zero);
            }
            for (int i = 0; i < edges; i++) vh.AddTriangle(start, start + 1 + (i + 1) % edges, start + 1 + i);
        }

        private static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color colour)
        {
            var halfWidth = Vector2.Perpendicular((b - a).normalized) * (.5f * width);
            int start = vh.currentVertCount;
            vh.AddVert(a - halfWidth, colour, Vector2.zero);
            vh.AddVert(a + halfWidth, colour, Vector2.zero);
            vh.AddVert(b + halfWidth, colour, Vector2.zero);
            vh.AddVert(b - halfWidth, colour, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
