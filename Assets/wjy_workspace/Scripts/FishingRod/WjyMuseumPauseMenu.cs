using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Keyboard = UnityEngine.InputSystem.Keyboard;

namespace Wjy.Museum
{
    [DefaultExecutionOrder(-20), DisallowMultipleComponent]
    public sealed class WjyMuseumPauseMenu : MonoBehaviour
    {
        [SerializeField] private WjyFishingRodCatchInteraction rod;
        private GameObject panel;
        private Camera view;
        private bool leftWasDown = true, rightWasDown = true;
        public bool IsOpen => panel && panel.activeInHierarchy;

        private void Start()
        {
            if (gameObject.scene.name != "Museum_261122_1" || !rod || !rod.SharedRig || !rod.SharedRig.Camera)
            {
                enabled = false;
                return;
            }
            view = rod.SharedRig.Camera;
            BuildMenu();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root.name == "UIFollowCamera" && root.transform.Find("Canvas/Quit"))
                    root.SetActive(false);
        }

        private void Update()
        {
            bool left = ReadMenuButton(XRNode.LeftHand, ref leftWasDown);
            bool right = ReadMenuButton(XRNode.RightHand, ref rightWasDown);
            if (panel && (left || right || (Keyboard.current?.mKey.wasPressedThisFrame ?? false)))
                SetOpen(!IsOpen);
        }

        private static bool ReadMenuButton(XRNode hand, ref bool wasDown)
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(hand);
            bool down = !device.isValid || (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) && pressed);
            bool justPressed = device.isValid && down && !wasDown;
            wasDown = down;
            return justPressed;
        }

        private void SetOpen(bool open)
        {
            if (!panel) return;
            if (open)
            {
                var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .01f)
                    forward = Vector3.ProjectOnPlane(view.transform.up, Vector3.up).normalized;
                Vector3 position = view.transform.position + forward * 1.25f - Vector3.up * .08f;
                panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            }
            panel.SetActive(open);
        }

        private void BuildMenu()
        {
            panel = new GameObject("Museum Menu", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            panel.SetActive(false);
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = new Vector2(630, 390);
            rect.localScale = Vector3.one * .0016f;
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            var backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(.035f, .085f, .13f, .97f);
            backdrop.raycastTarget = true;

            Label("Museum Menu", rect, new Vector2(0, 125), new Vector2(550, 62), 44, FontStyle.Bold);
            AddButton("Resume", rect, new Vector2(0, 26), new Vector2(510, 94), 46,
                new Color(.02f, .39f, .42f), () => SetOpen(false));
            AddButton("Quit", rect, new Vector2(0, -115), new Vector2(240, 60), 32,
                new Color(.18f, .24f, .3f), Quit);
        }

        private static void AddButton(string text, RectTransform parent, Vector2 position, Vector2 size,
            int fontSize, Color fill, UnityEngine.Events.UnityAction action)
        {
            var rect = MakeRect(text, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = fill;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(.85f, 1f, 1f);
            colors.pressedColor = new Color(.67f, .85f, .85f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(text, rect, Vector2.zero, size, fontSize, FontStyle.Bold);
        }

        private static void Label(string text, RectTransform parent, Vector2 position, Vector2 size, int fontSize, FontStyle style)
        {
            var label = MakeRect(text + " Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private static RectTransform MakeRect(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void OnDisable() => SetOpen(false);

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
