using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace CsyMuseum
{
    [DisallowMultipleComponent]
    public sealed class CSYExhibitGuide : MonoBehaviour
    {
        [Min(.1f)] public float enterRadius = 1.6f;
        [Min(.1f)] public float exitRadius = 1.85f;
        [Min(0f)] public float dwellSeconds = .6f;
        public Transform visitor;
        public Transform rangeAnchor;
        public AudioClip narration;
        public XRGrabInteractable inspectionObject;
        public string exhibitName = "Diving helmet";
        [TextArea] public string instruction = "Find the viewing window. Pick up the helmet for a closer look.";
        [TextArea] public string handlingHint = "Turn the helmet to find its window. Release to return it.";
        [TextArea] public string interpretation = "The viewing window lets a diver see while keeping water out of the helmet.";
        [Range(0f, 1f)] public float volume = .7f;

        private static CSYExhibitGuide playing;
        private AudioSource speaker;
        private Text body;
        private Camera cachedCamera;
        private bool inside, played, holding;
        private float arrived;

        private void Awake()
        {
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.loop = false;
            speaker.spatialBlend = 0f;
            MakePanel();
        }

        private void OnEnable()
        {
            if (!inspectionObject) return;
            inspectionObject.selectEntered.AddListener(OnGrab);
            inspectionObject.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            if (inspectionObject)
            {
                inspectionObject.selectEntered.RemoveListener(OnGrab);
                inspectionObject.selectExited.RemoveListener(OnRelease);
            }
            inside = played = holding = false;
            StopNarration();
            Refresh();
        }

        private void Update()
        {
            if (!visitor && (!cachedCamera || !cachedCamera.isActiveAndEnabled)) cachedCamera = Camera.main;
            Transform eye = visitor ? visitor : cachedCamera ? cachedCamera.transform : null;
            if (!eye || !eye.gameObject.activeInHierarchy)
            {
                inside = played = false;
                StopNarration();
                return;
            }

            // Floor-plan distance keeps the same entry region for seated and standing visitors.
            Vector3 delta = eye.position - (rangeAnchor ? rangeAnchor.position : transform.position);
            delta.y = 0f;
            float distance = delta.magnitude;
            if (inside && distance > Mathf.Max(enterRadius, exitRadius))
            {
                inside = played = false;
                StopNarration();
            }
            if (!inside && distance <= enterRadius)
            {
                inside = true;
                arrived = Time.unscaledTime;
            }
            if (!inside || played || !narration || Time.unscaledTime - arrived < dwellSeconds) return;
            if (playing && playing != this && playing.speaker.isPlaying) return;
            PlayNarration();
        }

        private void PlayNarration()
        {
            played = true;
            playing = this;
            speaker.clip = narration;
            speaker.volume = volume;
            speaker.Play();
        }

        private void StopNarration()
        {
            if (speaker && speaker.isPlaying) speaker.Stop();
            if (playing == this) playing = null;
        }

        private void OnGrab(SelectEnterEventArgs _) { holding = true; Refresh(); }
        private void OnRelease(SelectExitEventArgs _) { holding = inspectionObject && inspectionObject.interactorsSelecting.Count > 0; Refresh(); }
        private void Refresh()
        {
            if (body) body.text = (holding ? handlingHint : instruction) + "\n" + interpretation;
        }

        private void MakePanel()
        {
            var panel = new GameObject("Readable exhibit card", typeof(RectTransform), typeof(Canvas), typeof(Image));
            panel.transform.SetParent(transform, false);
            panel.transform.localScale = Vector3.one * .0015f;
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(530, 230);
            var image = panel.GetComponent<Image>();
            image.color = new Color(.045f, .08f, .10f, 1f);
            image.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MakeText(panel.transform, font, exhibitName, 36, 160, 218);
            body = MakeText(panel.transform, font, "", 27, 12, 154);
            Refresh();
        }

        private static Text MakeText(Transform parent, Font font, string value, int size, float low, float high)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(20f, low);
            rect.offsetMax = new Vector2(-20f, high);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }
    }
}
