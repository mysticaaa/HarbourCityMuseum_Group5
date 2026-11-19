using UnityEngine;
using UnityEngine.Rendering;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyOceanMeteors : MonoBehaviour
    {
        private sealed class Meteor
        {
            public MeshRenderer renderer;
            public Vector3 start, travel;
            public float age, lifetime;
        }

        private static readonly int Strength = Shader.PropertyToID("_Strength");
        private readonly System.Random random = new System.Random();
        private readonly Meteor[] meteors = new Meteor[2];
        private MaterialPropertyBlock fade;
        private Mesh mesh;
        private Material material;
        private float untilNext, sideBearing;
        private int next;

        private void Awake()
        {
            var shader = Resources.Load<Shader>("WjyOceanReveals/SpectralWater");
            if (!shader)
            {
                Debug.LogError("Missing ocean meteor trail shader.", this);
                enabled = false;
                return;
            }

            mesh = BuildTrail();
            material = new Material(shader) { name = "Meteor light" };
            fade = new MaterialPropertyBlock();
            for (int i = 0; i < meteors.Length; i++)
            {
                var trail = new GameObject("Ocean meteor " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
                trail.transform.SetParent(transform, false);
                trail.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = trail.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meteors[i] = new Meteor { renderer = renderer };
            }
        }

        private void OnEnable()
        {
            if (!material) return;
            next = 0;
            sideBearing = Range(70f, 130f);
            foreach (var meteor in meteors) meteor.renderer.enabled = false;
            Launch(.13f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var meteor in meteors)
            {
                if (!meteor.renderer.enabled) continue;
                meteor.age += dt;
                if (meteor.age >= meteor.lifetime) meteor.renderer.enabled = false;
                else Draw(meteor);
            }

            untilNext -= dt;
            if (untilNext <= 0f) Launch(0f);
        }

        private void Launch(float initialAge)
        {
            var meteor = meteors[next];
            bool atBow = next == 0;
            next = (next + 1) % meteors.Length;
            untilNext = Range(.92f, 1.14f);
            float azimuth = atBow ? Range(-27f, 27f) : sideBearing;
            if (!atBow) sideBearing = Mathf.Repeat(sideBearing + Range(89f, 133f), 360f);
            var normal = Quaternion.Euler(-Range(23f, 48f), azimuth, 0f) * Vector3.forward;
            var east = Vector3.Cross(Vector3.up, normal).normalized;
            var skyUp = Vector3.Cross(normal, east);
            var heading = ((random.Next(2) == 0 ? east : -east) - skyUp * Range(.33f, .79f)).normalized;

            // A short chord of the sky sphere keeps the trail above the passing ship and inside the sky.
            meteor.travel = heading * Range(16f, 24f);
            meteor.start = normal * 54f - meteor.travel * .5f;
            meteor.lifetime = Range(1.32f, 1.65f);
            meteor.age = initialAge;
            var trail = meteor.renderer.transform;
            trail.localRotation = Quaternion.LookRotation(normal, Vector3.Cross(normal, heading));
            trail.localScale = Vector3.one * Range(.85f, 1.2f);
            meteor.renderer.enabled = true;
            Draw(meteor);
        }

        private void Draw(Meteor meteor)
        {
            float progress = meteor.age / meteor.lifetime;
            meteor.renderer.transform.localPosition = meteor.start + meteor.travel * progress;
            float brightness = Mathf.SmoothStep(0f, 1f, progress / .14f)
                             * Mathf.SmoothStep(0f, 1f, (1f - progress) / .32f);
            fade.SetFloat(Strength, brightness);
            meteor.renderer.SetPropertyBlock(fade);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private static Mesh BuildTrail()
        {
            // Two tapered ribbons, a diffuse blue wake and a white-hot core, in one draw.
            float[] x = { -6f, -3.3f, -1.2f, -.24f, 0f, .24f };
            float[] width = { .016f, .13f, .21f, .28f, .30f, .015f };
            float[] opacity = { 0f, .16f, .42f, .76f, 1f, 0f };
            int columns = x.Length;
            var vertices = new Vector3[columns * 4];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[(columns - 1) * 12];
            int index = 0;
            for (int band = 0; band < 2; band++)
            {
                for (int k = 0; k < columns; k++)
                {
                    int v = 2 * (band * columns + k);
                    float halfWidth = width[k] * (band == 0 ? 1.5f : .57f);
                    var color = band == 0 ? new Color(.30f, .63f, 1f, opacity[k] * .67f)
                                          : new Color(1f, .94f, .82f, opacity[k]);
                    vertices[v] = new Vector3(x[k], -halfWidth);
                    vertices[v + 1] = new Vector3(x[k], halfWidth);
                    colors[v] = colors[v + 1] = color;
                    // The spectral shader's radial falloff softens the ribbon edges.
                    uv[v] = new Vector2(.5f, 0f);
                    uv[v + 1] = new Vector2(.5f, 1f);
                    if (k == columns - 1) continue;
                    indices[index++] = v; indices[index++] = v + 1; indices[index++] = v + 2;
                    indices[index++] = v + 2; indices[index++] = v + 1; indices[index++] = v + 3;
                }
            }
            var result = new Mesh { name = "Tapered meteor with luminous core" };
            result.vertices = vertices;
            result.colors = colors;
            result.uv = uv;
            result.triangles = indices;
            result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        private void OnDisable()
        {
            foreach (var meteor in meteors)
                if (meteor != null && meteor.renderer) meteor.renderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (mesh) Destroy(mesh);
            if (material) Destroy(material);
        }
    }
}
