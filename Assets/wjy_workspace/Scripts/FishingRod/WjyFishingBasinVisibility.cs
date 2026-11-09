using UnityEngine;
using UnityEngine.Rendering;

namespace Wjy.Museum
{
    // Run after the rod's return animation so the delay starts when it reaches its stand.
    [DefaultExecutionOrder(10), DisallowMultipleComponent]
    public sealed class WjyFishingBasinVisibility : MonoBehaviour
    {
        [SerializeField] private WjyFishingRodCatchInteraction rod;
        [SerializeField] private MeshRenderer[] surfaces;
        [SerializeField] private Material woodFade;
        [SerializeField] private Material waterFade;

        private const float HideDelay = 5f;
        private const float FadeDuration = .65f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Surface[] parts;
        private MaterialPropertyBlock fadeProperties;
        private float opacity, remaining;
        private bool wasInUse;

        private struct Surface
        {
            public MeshRenderer renderer;
            public Collider collider;
            public Material original, fade;
            public ShadowCastingMode shadows;
        }

        private void Awake()
        {
            if (!rod || surfaces == null || surfaces.Length == 0 || !woodFade || !waterFade)
            {
                enabled = false;
                return;
            }
            fadeProperties = new MaterialPropertyBlock();
            parts = new Surface[surfaces.Length];
            for (int i = 0; i < surfaces.Length; i++)
            {
                var renderer = surfaces[i];
                if (!renderer) continue;
                parts[i] = new Surface
                {
                    renderer = renderer,
                    collider = renderer.GetComponent<Collider>(),
                    original = renderer.sharedMaterial,
                    fade = i == 0 ? waterFade : woodFade,
                    shadows = renderer.shadowCastingMode,
                };
            }
            ApplyOpacity(0f);
        }

        private void LateUpdate() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            if (parts == null) return;
            bool inUse = rod && rod.isActiveAndEnabled
                && rod.State != WjyFishingRodCatchInteraction.RodState.Display;
            float fadeTime = dt;
            if (inUse || wasInUse)
                remaining = HideDelay;
            else if (remaining > 0f)
            {
                float previousRemaining = remaining;
                remaining = Mathf.Max(0f, remaining - dt);
                if (remaining == 0f)
                    fadeTime = Mathf.Max(0f, dt - previousRemaining);
            }
            wasInUse = inUse;

            bool show = inUse || remaining > 0f;
            float next = Mathf.MoveTowards(opacity, show ? 1f : 0f, fadeTime / FadeDuration);
            if (next == opacity) return;
            ApplyOpacity(next);
            if (next == 0f && rod) rod.DismissBasinMessage();
        }

        private void ApplyOpacity(float value)
        {
            opacity = value;
            bool visible = value > 0f;
            bool fading = visible && value < 1f;
            foreach (var part in parts)
            {
                if (!part.renderer) continue;
                var renderer = part.renderer;
                renderer.sharedMaterial = fading ? part.fade : part.original;
                renderer.shadowCastingMode = fading ? ShadowCastingMode.Off : part.shadows;
                if (fading)
                {
                    var color = part.fade.color;
                    color.a *= Mathf.SmoothStep(0f, 1f, value);
                    fadeProperties.SetColor(ColorProperty, color);
                    renderer.SetPropertyBlock(fadeProperties);
                }
                else renderer.SetPropertyBlock(null);
                renderer.enabled = visible;
                if (part.collider) part.collider.enabled = visible;
            }
        }

        private void OnDisable()
        {
            if (parts == null) return;
            wasInUse = false;
            remaining = 0f;
            ApplyOpacity(0f);
        }
    }
}
