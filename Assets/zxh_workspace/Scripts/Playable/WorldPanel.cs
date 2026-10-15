using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR;

namespace ZxhMuseum
{
    public sealed class WorldPanel : MonoBehaviour
    {
        private RectTransform body;
        private float nextY = -20;
        private static int trackedFrame = -1;
        private static bool trackedThisFrame;
        public Text Status { get; private set; }
        public static WorldPanel Create(Transform parent, string title, Vector3 position, Quaternion rotation, float width = 800)
        {
            var go = new GameObject("ZXH_UI_" + title, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = Vector3.one * 0.001f;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            go.GetComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.ThreeD;
            go.GetComponent<GraphicRaycaster>().enabled = false;
            go.GetComponent<TrackedDeviceGraphicRaycaster>().checkFor3DOcclusion = true;
            go.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = false;
            var panel = go.AddComponent<WorldPanel>();
            panel.body = go.GetComponent<RectTransform>();
            panel.body.sizeDelta = new Vector2(width, 1000);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.025f, 0.055f, 0.075f, 0.97f);
            panel.Label(title, 44, 75);
            panel.Status = panel.Label("", 28, 180);
            return panel;
        }
        private RectTransform Row(string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(body, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, nextY);
            rect.sizeDelta = new Vector2(body.sizeDelta.x - 40, height);
            nextY -= height + 16;
            body.sizeDelta = new Vector2(body.sizeDelta.x, Mathf.Max(350, -nextY + 20));
            return rect;
        }
        public Text Label(string value, int size = 28, float height = 100)
        {
            var text = Row("Label", height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
        public Button Button(string caption, Action action)
        {
            var row = Row("Button_" + caption, 70);
            var image = row.gameObject.AddComponent<Image>();
            image.color = new Color(0.10f, 0.30f, 0.38f);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());
            // A physical XRI target accepts the teammate's existing ray + Activate binding.
            // It does not depend on a separate UI Press action or remap public controllers.
            var hit = row.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0, -35, -4);
            hit.size = new Vector3(row.sizeDelta.x, 70, 6);
            BoatTriggerAction.Bind(row.gameObject, () => { if (button.IsInteractable()) button.onClick.Invoke(); });
            var target = row.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
            target.firstHoverEntered.AddListener(_ => image.color = new Color(.22f, .55f, .62f));
            target.lastHoverExited.AddListener(_ => image.color = new Color(.10f, .30f, .38f));
            var child = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(row, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 0); rect.offsetMax = new Vector2(-10, 0);
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 30;
            text.text = caption; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }
        public static void EnsureEventSystem()
        {
            var system = EventSystem.current;
            if (system == null) system = new GameObject("ZXH_EventSystem").AddComponent<EventSystem>();
            foreach (var input in system.GetComponents<BaseInputModule>())
                if (!(input is XRUIInputModule)) input.enabled = false;
            if (system.GetComponent<XRUIInputModule>() == null) system.gameObject.AddComponent<XRUIInputModule>();
            foreach (var other in UnityEngine.Object.FindObjectsOfType<EventSystem>())
                if (other != system) other.enabled = false;
            UpdateInputMode();
        }
        public static bool HeadsetTracked
        {
            get
            {
                if (trackedFrame == Time.frameCount) return trackedThisFrame;
                trackedFrame = Time.frameCount;
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                trackedThisFrame = head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked;
                return trackedThisFrame;
            }
        }
        public static void UpdateInputMode()
        {
            var module = EventSystem.current != null ? EventSystem.current.GetComponent<XRUIInputModule>() : null;
            if (module == null) return;
            module.enableXRInput = HeadsetTracked;
            module.enableMouseInput = !HeadsetTracked;
        }
        private void LateUpdate()
        {
            // Mouse and Quest both hit physical buttons through BoatTriggerAction.
            GetComponent<GraphicRaycaster>().enabled = false;
            // XR buttons use physical targets, avoiding two callbacks for one trigger press.
            GetComponent<TrackedDeviceGraphicRaycaster>().enabled = false;
            // Unity UI is two-sided by default; hide backs so mirrored panels cannot block the view.
            if (Camera.main != null)
            {
                bool visible = Vector3.Dot(-transform.forward,
                    Camera.main.transform.position - transform.position) > 0;
                GetComponent<Canvas>().enabled = visible;
                foreach (var collider in GetComponentsInChildren<BoxCollider>()) collider.enabled = visible;
            }
        }
    }
}
