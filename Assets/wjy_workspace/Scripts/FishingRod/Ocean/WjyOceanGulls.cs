using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyOceanGulls : MonoBehaviour
    {
        private const int BirdCount = 4;
        private static readonly int WingPose = Shader.PropertyToID("_WingPose");
        private readonly System.Random calls = new System.Random();
        private readonly AudioSource[] birds = new AudioSource[BirdCount];
        private readonly MeshRenderer[] renderers = new MeshRenderer[BirdCount];
        private readonly AudioClip[] voices = new AudioClip[2];
        private MaterialPropertyBlock pose;
        private Mesh mesh;
        private Material plumage;
        private GameObject flock;
        private float flightTime, timeToCall;
        private int lastCaller = -1;

        private void Awake()
        {
            var shader = Resources.Load<Shader>("WjyOceanGullPlumage");
            if (!shader)
            {
                Debug.LogError("Missing the ocean gull plumage shader.", this);
                enabled = false;
                return;
            }
            voices[0] = Resources.Load<AudioClip>("WjyOceanGullMew");
            voices[1] = Resources.Load<AudioClip>("WjyOceanGullCalls");
            mesh = BuildGull();
            plumage = new Material(shader) { name = "Moonlit gull feathers" };
            pose = new MaterialPropertyBlock();
            flock = new GameObject("Circling sea gulls");
            flock.transform.SetParent(transform, false);
            for (int i = 0; i < BirdCount; i++)
            {
                var bird = new GameObject("Gull " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer), typeof(AudioSource));
                bird.transform.SetParent(flock.transform, false);
                bird.transform.localScale = Vector3.one * (1f + .05f * (i - 1));
                bird.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = renderers[i] = bird.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = plumage;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var source = birds[i] = bird.GetComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = .12f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 5f;
                source.maxDistance = 38f;
                source.volume = .48f;
                PoseBird(i);
            }
        }

        private void OnEnable()
        {
            if (flock) flock.SetActive(true);
            timeToCall = 3f + (float)calls.NextDouble() * 2f;
        }

        private void Update()
        {
            flightTime += Time.deltaTime;
            for (int i = 0; i < BirdCount; i++) PoseBird(i);
            timeToCall -= Time.deltaTime;
            if (timeToCall > 0f) return;

            int index = lastCaller < 0 ? calls.Next(BirdCount) : (lastCaller + 1 + calls.Next(BirdCount - 1)) % BirdCount;
            lastCaller = index;
            var source = birds[index];
            source.clip = voices[calls.Next(voices.Length)];
            source.pitch = .90f + .18f * (float)calls.NextDouble();
            if (source.clip) source.Play();
            timeToCall = 7f + (float)calls.NextDouble() * 6f;
        }

        private void PoseBird(int i)
        {
            // Different ellipses and speeds keep the flock crossing the bow without crossing the deck.
            float speed = .24f + .017f * i;
            float angle = flightTime * speed + i * 1.45f + .55f;
            float a = Mathf.Cos(angle), b = Mathf.Sin(angle);
            float width = 9f + i * 1.6f, depth = 6.8f + i * .8f;
            float lift = flightTime * .43f + i * 2.1f;
            var position = new Vector3(width * a + (i - 1.5f) * 1.9f, 6.1f + .72f * i + .72f * Mathf.Sin(lift), 4f + depth * b);
            var tangent = new Vector3(-width * b * speed, .31f * Mathf.Cos(lift), depth * a * speed);
            float bank = 17f + 6f * Mathf.Sin(angle * 2f);
            birds[i].transform.SetLocalPositionAndRotation(position, Quaternion.LookRotation(tangent) * Quaternion.AngleAxis(bank, Vector3.forward));

            // A burst of beats gives way to a longer, gently flexing glide.
            float beats = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Sin(flightTime * .85f + i * 1.9f) - .05f) * 2.8f));
            float stroke = flightTime * (15.5f + i * .65f) + i * 2.3f;
            float shoulder = .10f + beats * .53f * Mathf.Sin(stroke);
            float wrist = -.14f - beats * .43f * Mathf.Cos(stroke);
            pose.SetVector(WingPose, new Vector4(shoulder, wrist, 0f, 0f));
            renderers[i].SetPropertyBlock(pose);
        }

        private void OnDisable()
        {
            foreach (var source in birds) if (source) source.Stop();
            if (flock) flock.SetActive(false);
        }

        private void OnDestroy()
        {
            if (flock) Destroy(flock);
            if (mesh) Destroy(mesh);
            if (plumage) Destroy(plumage);
        }

        private static Mesh BuildGull()
        {
            // The breasts and tapered bill are rings about +Z. Each wing tapers from its
            // elbow to dark outer primaries; the underside is pale in the moonlight.
            var vertices = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            Color32 pearl = new Color32(234, 237, 229, 255), mantle = new Color32(150, 167, 186, 255);
            Color32 primaries = new Color32(55, 64, 81, 255), bill = new Color32(220, 169, 76, 255);

            // Cross section tuple: centre Z, centre Y, half width, half height.
            void Spindle(Vector4[] rings, Color32 belly, Color32 back)
            {
                const int sides = 10;
                int start = vertices.Count;
                for (int row = 0; row < rings.Length; row++)
                {
                    var r = rings[row];
                    for (int j = 0; j < sides; j++)
                    {
                        float phi = j * (2f * Mathf.PI / sides);
                        float y = Mathf.Sin(phi);
                        vertices.Add(new Vector3(r.z * Mathf.Cos(phi), r.y + r.w * y, r.x));
                        var tone = Color32.Lerp(belly, back, Mathf.Clamp01(y * 1.5f));
                        tone.a = 0; // A zero deformation weight keeps the breast and bill rigid.
                        colors.Add(tone);
                        if (row == 0) continue;
                        int prev = start + (row - 1) * sides + j, next = start + (row - 1) * sides + (j + 1) % sides;
                        triangles.AddRange(new[] { prev, next, prev + sides, next, next + sides, prev + sides });
                    }
                }
            }
            Spindle(new[] { new Vector4(-.37f,.015f,.009f,.009f),new Vector4(-.20f,-.008f,.066f,.055f),
                new Vector4(-.045f,0f,.09f,.084f),new Vector4(.085f,.018f,.073f,.070f),
                new Vector4(.145f,.055f,.053f,.056f),new Vector4(.192f,.072f,.059f,.055f),
                new Vector4(.238f,.067f,.043f,.035f),new Vector4(.25f,.064f,.008f,.008f) }, pearl, mantle);
            Spindle(new[] { new Vector4(.229f,.065f,.031f,.022f),new Vector4(.285f,.06f,.022f,.016f),
                new Vector4(.321f,.05f,.001f,.002f) }, bill, bill);

            void FeatherStrip(Vector3[] leading, Vector3[] trailing, Color32[] tones)
            {
                int start = vertices.Count;
                for (int j = 0; j < leading.Length; j++)
                {
                    vertices.Add(leading[j]); vertices.Add(trailing[j]);
                    colors.Add(tones[j]); colors.Add(tones[j]);
                    if (j == 0) continue;
                    int k = start + 2 * j;
                    triangles.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
                }
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 P(float span, float y, float z) => new Vector3(s * span, y, z);
                FeatherStrip(new[] {P(.065f,.04f,.065f), P(.22f,.084f,.09f), P(.39f,.092f,.108f),
                    P(.51f,.075f,.074f), P(.62f,.046f,.016f), P(.71f,.021f,-.044f), P(.79f,.012f,-.10f)},
                    new[] {P(.065f,.015f,-.145f), P(.22f,.048f,-.167f), P(.39f,.052f,-.124f),
                    P(.51f,.047f,-.103f), P(.62f,.031f,-.12f), P(.71f,.014f,-.126f), P(.79f,.012f,-.10f)},
                    new[] {pearl, pearl, mantle, mantle, primaries, primaries, pearl});
                FeatherStrip(new[] {P(0,.022f,-.215f), P(.065f,.023f,-.243f), P(.107f,.012f,-.313f)},
                    new[] {P(0,.018f,-.398f), P(.065f,.014f,-.382f), P(.107f,.012f,-.313f)},
                    new[] {pearl, pearl, mantle});
            }
            var result = new Mesh { name = "Ocean gull" };
            result.SetVertices(vertices);
            result.SetColors(colors);
            result.SetTriangles(triangles, 0);
            result.RecalculateNormals();
            result.bounds = new Bounds(Vector3.zero, new Vector3(1.65f, 1.25f, .84f));
            return result;
        }
    }
}
