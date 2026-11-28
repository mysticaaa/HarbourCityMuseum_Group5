using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using ZxhMuseum;

namespace CsyMuseum
{
    public sealed class CSYGuideCard : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private Text instructions, transcript, feedback, state;
        private Collider[] targets;
        private float y;
        private CSYExhibitGuide owner;
        private bool presented;
        private Button listen, stop, automatic;
        private readonly List<Button> buttons = new List<Button>();
        public bool TextFits { get; private set; } = true;
        public void Build(CSYExhibitGuide guide, XRInteractionManager manager)
        {
            owner = guide;
            var go = new GameObject("Exhibit guide card", typeof(RectTransform), typeof(Canvas), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * .0011f;
            panel = (RectTransform)go.transform; panel.pivot = new Vector2(.5f, 1f);
            panel.sizeDelta = new Vector2(700, 900);
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var background = go.GetComponent<Image>(); background.color = new Color(.025f, .055f, .075f, 1f);
            background.raycastTarget = false;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); y = 18;
            Label(guide.exhibitName, 40, 60);
            instructions = Label(guide.instruction, 30, 90);
            transcript = Label(guide.transcript, 28, 150);
            state = Label("Approach and pause to hear the introduction.", 26, 90);
            var audioRow = Buttons("Listen", () => guide.Coordinator.Replay(guide), "Skip audio", () => guide.Coordinator.StopFor(guide), manager);
            listen = audioRow[0]; stop = audioRow[1];
            var preferenceRow = Buttons("Auto audio: ON", () => guide.Coordinator.ToggleAutomatic(),
                guide.inspectionTask ? "Reset observation" : "Read without audio", () => {
                    if (guide.inspectionTask) guide.ResetTask(); else guide.Coordinator.StopFor(guide);
                }, manager);
            automatic = preferenceRow[0];
            feedback = Label(guide.inspectionTask ? guide.question : "", 28, 110);
            if (guide.inspectionTask)
                Buttons(guide.correctAnswer, () => guide.Answer(true), guide.otherAnswer, () => guide.Answer(false), manager);
            panel.sizeDelta = new Vector2(700, y + 16);
            targets = panel.GetComponentsInChildren<Collider>();
        }
        private Text Label(string value, int size, float height)
        {
            var rect = Row("Label", height); var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.color = Color.white;
            label.text = value; label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false; return label;
        }
        private RectTransform Row(string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)go.transform;
            rect.SetParent(panel, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f); rect.anchoredPosition = new Vector2(0, -y);
            rect.sizeDelta = new Vector2(652, height); y += height + 12; return rect;
        }
        private Button[] Buttons(string left, Action a, string right, Action b, XRInteractionManager manager)
        {
            var row = Row("Controls", 72);
            return new[] { Button(row, left, a, -168, manager), Button(row, right, b, 168, manager) };
        }
        private Button Button(RectTransform parent, string caption, Action action, float x, XRInteractionManager manager)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -36); rect.sizeDelta = new Vector2(316, 72);
            var image = go.GetComponent<Image>(); image.color = new Color(.08f, .30f, .36f); image.raycastTarget = false;
            var button = go.GetComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => {
                if (isActiveAndEnabled && presented && owner && owner.Coordinator
                    && owner.Coordinator.CanInteract(owner) && button.IsInteractable()) action();
            });
            buttons.Add(button);
            var collider = go.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 0, -5); collider.size = new Vector3(316, 72, 8);
            // Uses existing controller Activate and direct mouse-ray fallback, with one-frame deduplication.
            BoatTriggerAction.Bind(go, () => button.onClick.Invoke());
            var target = go.GetComponent<XRSimpleInteractable>(); target.interactionManager = manager;
            target.firstHoverEntered.AddListener(_ => image.color = new Color(.18f, .48f, .54f));
            target.lastHoverExited.AddListener(_ => image.color = new Color(.08f, .30f, .36f));
            var child = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            var text = child.GetComponent<Text>(); var tr = (RectTransform)child.transform;
            tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(8, 2); tr.offsetMax = new Vector2(-8, -2);
            text.font = font; text.fontSize = 28; text.text = caption; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return button;
        }
        public void SetText(string instruction, string caption, string result)
        {
            instructions.text = instruction; transcript.text = caption; feedback.text = result;
            TextFits = Fits(instructions) && Fits(transcript) && Fits(feedback);
            if (!TextFits) Debug.LogError("[CSY E] Card text exceeds its fixed readable layout.", this);
        }
        private static bool Fits(Text text) => text.preferredHeight <= text.rectTransform.rect.height + 1;
        public void SetState(string value) { if (state.text != value) state.text = value; }
        public void SetControls(CSYGuideControls controls, bool canInteract)
        {
            SetState(controls.Message);
            foreach (var button in buttons) button.interactable = canInteract;
            SetButton(listen, controls.ListenLabel, canInteract && controls.CanListen);
            SetButton(stop, owner.AudioState == CSYGuideAudioState.Playing ? "Stop" : "Skip audio", canInteract && controls.CanStop);
            SetButton(automatic, controls.AutoLabel, canInteract);
        }
        private static void SetButton(Button button, string value, bool enabled)
        {
            button.interactable = enabled;
            var label = button.GetComponentInChildren<Text>();
            if (label.text != value) label.text = value;
        }
        public void SetPresentation(bool available, Camera camera)
        {
            bool visible = isActiveAndEnabled && available && camera && Vector3.Dot(-transform.forward, camera.transform.position - transform.position) > 0;
            presented = visible;
            canvas.enabled = visible;
            if (targets != null) foreach (var target in targets) if (target) target.enabled = visible;
        }
        private void OnDisable() { if (canvas) SetPresentation(false, null); }
    }
}
