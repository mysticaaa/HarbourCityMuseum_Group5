using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Wjy.Museum
{
    // A short-lived projection above the sea. The actual collectible stays on its hook.
    public sealed class WjyOceanCatchReveal : MonoBehaviour
    {
        private const string EffectResources = "WjyOceanReveals/";
        private const float Tau = Mathf.PI * 2f;
        private static readonly Color[] Palette =
        {
            new Color(.29f, .86f, 1f), new Color(.32f, 1f, .52f),
            new Color(1f, .43f, .15f), new Color(.28f, 1f, .83f),
            new Color(1f, .20f, .48f), new Color(.58f, .75f, 1f),
            new Color(.31f, .87f, 1f)
        };

        private GameObject burst;
        private Transform[] echoes;
        private Material sprayMaterial, echoMaterial;
        private readonly Mesh[] echoMeshes = new Mesh[Palette.Length];
        private Quaternion bearing;
        private Vector3 stage;
        private float elapsed, duration;
        private WjyCatchKind kind;
        internal bool IsPlaying => burst != null;

        public void Play(int kind, Vector3 surfacePoint, Transform viewer)
        {
            if (!isActiveAndEnabled || !viewer || kind < 0 || kind >= Palette.Length) return;
            Stop();
            if (!sprayMaterial)
            {
                var shader = Resources.Load<Shader>(EffectResources + "SpectralWater");
                if (!shader) return;
                sprayMaterial = new Material(shader);
                echoMaterial = new Material(shader);
                echoMaterial.SetFloat("_Projection", 1f);
            }

            this.kind = (WjyCatchKind)kind;
            var fromViewer = Vector3.ProjectOnPlane(surfacePoint - viewer.position, Vector3.up);
            var horizontal = fromViewer.sqrMagnitude > .01f ? fromViewer.normalized :
                Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < .01f) horizontal = Vector3.forward;
            bearing = Quaternion.LookRotation(horizontal);

            // Keep the whale broadside and beyond the raft even when the hook exits beside the player.
            float minDistance = this.kind == WjyCatchKind.Whale ? 9f : 2.4f;
            stage = surfacePoint + horizontal * Mathf.Max(0f, minDistance - fromViewer.magnitude);
            duration = this.kind == WjyCatchKind.Whale ? 4.5f : 2.7f;
            elapsed = 0f;
            burst = new GameObject(this.kind + " ocean reveal");
            SceneManager.MoveGameObjectToScene(burst, gameObject.scene);
            burst.transform.SetPositionAndRotation(surfacePoint, bearing);
            EmitSurfaceSignature();
            CreateEchoes();
            PoseEchoes(0f);

            var clip = Resources.Load<AudioClip>(EffectResources + this.kind);
            if (clip)
            {
                var audio = burst.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = .75f;
                audio.dopplerLevel = 0f;
                audio.minDistance = 3f;
                audio.maxDistance = 35f;
                audio.volume = .72f;
                audio.clip = clip;
                audio.Play();
            }
        }

        private void CreateEchoes()
        {
            Mesh mesh = GetEchoMesh();
            if (!mesh) return;
            var bounds = mesh.bounds;
            float length = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float span = kind == WjyCatchKind.Whale ? 12f : kind == WjyCatchKind.Fish ? .68f : 1.35f;
            echoes = new Transform[kind == WjyCatchKind.Fish ? 4 : 1];
            echoMaterial.SetColor("_Tint", Palette[(int)kind]);
            for (int i = 0; i < echoes.Length; i++)
            {
                var pivot = new GameObject("Water silhouette").transform;
                pivot.SetParent(burst.transform, false);
                var copy = new GameObject(kind.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetParent(pivot, false);
                copy.transform.localPosition = -bounds.center;
                copy.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = copy.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = echoMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                pivot.localScale = Vector3.one * (span / Mathf.Max(.01f, length));
                echoes[i] = pivot;
            }
        }

        private Mesh GetEchoMesh()
        {
            // All parts use the same spectral material. Combine once so even the 63-part lobster
            // costs only one extra renderer when it appears on the headset.
            int index = (int)kind;
            if (echoMeshes[index]) return echoMeshes[index];
            var prefab = Resources.Load<GameObject>("WjyFishingCatches/" + kind);
            if (!prefab) return null;
            var filters = prefab.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0) return null;
            var parts = new CombineInstance[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                parts[i].mesh = filters[i].sharedMesh;
                parts[i].transform = prefab.transform.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
            }
            var mesh = new Mesh { name = kind + " water silhouette", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts);
            mesh.UploadMeshData(true);
            echoMeshes[index] = mesh;
            return mesh;
        }

        private void EmitSurfaceSignature()
        {
            var stars = burst.AddComponent<ParticleSystem>();
            stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = stars.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = duration;
            main.maxParticles = 280;
            var emission = stars.emission;
            emission.enabled = false;
            var shape = stars.shape;
            shape.enabled = false;
            var renderer = stars.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sprayMaterial;
            renderer.maxParticleSize = .13f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(.93f, 0f), new GradientAlphaKey(.85f, .55f), new GradientAlphaKey(0f, 1f) });
            var fading = stars.colorOverLifetime;
            fading.enabled = true;
            fading.color = gradient;
            stars.Play();

            Color color = Palette[(int)kind];
            for (int i = 0; i < 72; i++)
            {
                float angle = Tau * i / 72f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Emit(stars, radial * .10f + Vector3.up * .04f, radial * 1.4f, .075f, 1.1f, color);
            }

            for (int i = 0; i < 168; i++)
            {
                float t = i / 167f;
                float phase = i * 2.399963f; // Even distribution around a circle without using game randomness.
                float variation = Mathf.Repeat(i * .618034f, 1f);
                Vector3 pos, velocity;
                switch (kind)
                {
                    case WjyCatchKind.Fish: // Four ribbons follow the school.
                        pos = new Vector3((t - .5f) * 2f, .2f + (i % 4) * .15f + .14f * Mathf.Sin(8f*t), .15f*Mathf.Cos(8f*t));
                        velocity = new Vector3(1.05f, .45f + variation * .2f, 0f);
                        break;
                    case WjyCatchKind.Turtle: // Seven luminous scutes outline a shell.
                        int cell = i / 24, vertex = (i % 24) / 4;
                        Vector2 centre = cell == 0 ? Vector2.zero : Circle((cell - 1) * Tau / 6f) * .5f;
                        Vector2 edge = Vector2.Lerp(Circle((vertex+.5f)*Tau/6f), Circle((vertex+1.5f)*Tau/6f), (i%4)/4f);
                        Vector2 shell = centre + edge * .287f;
                        pos = new Vector3(shell.x, .85f + shell.y * .80f, .26f * (1f-shell.sqrMagnitude));
                        velocity = new Vector3(shell.x * .26f, .27f + shell.y * .22f, .05f);
                        break;
                    case WjyCatchKind.Crab: // Paired coral fans snap inwards.
                        float side = (i & 1) == 0 ? -1f : 1f;
                        pos = new Vector3(side*(.38f + .65f*Mathf.Cos(3.7f*t)), .12f + .75f*Mathf.Sin(Mathf.PI*t), .15f*variation);
                        velocity = new Vector3(side*(.52f - t), .55f + .18f*variation, .1f);
                        break;
                    case WjyCatchKind.Trash: // Two champagne spirals bubble up from the bottle.
                        float corkscrew = t * Tau * 2f + (i%2)*Mathf.PI;
                        pos = new Vector3(.35f*Mathf.Cos(corkscrew), .1f + t*1.3f, .35f*Mathf.Sin(corkscrew));
                        velocity = new Vector3(.14f*Mathf.Cos(corkscrew), .55f + .36f*t, .14f*Mathf.Sin(corkscrew));
                        break;
                    case WjyCatchKind.Lobster: // A red tail fan bursts into radial jets.
                        float ray = (i%9 - 4) * .29f;
                        pos = new Vector3(Mathf.Sin(ray)*t*.85f, .12f + Mathf.Cos(ray)*t, .12f*variation);
                        velocity = new Vector3(Mathf.Sin(ray)*1.35f, Mathf.Cos(ray)*(.65f + t), -.1f);
                        break;
                    case WjyCatchKind.Seal: // A pair of ice-blue arcs traces a porpoising leap.
                        pos = new Vector3((t-.5f)*2.1f, .16f + Mathf.Abs(Mathf.Sin(t*Tau))*.85f, (i%2)*.20f);
                        velocity = new Vector3(.45f, .38f, .14f*variation);
                        break;
                    default: // The whale's blow scatters a high crown of spray.
                        var radial = new Vector3(Mathf.Cos(phase), 0f, Mathf.Sin(phase));
                        pos = radial*.18f + Vector3.up*.06f;
                        velocity = radial*(.5f + variation*2.3f) + Vector3.up*(1.2f + (1f-variation)*2.8f);
                        break;
                }
                Emit(stars, pos, velocity, .055f + variation*.10f, 1.05f + variation*1.1f,
                    Color.Lerp(color, Color.white, variation*.70f));
            }
        }

        private static Vector2 Circle(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        private static void Emit(ParticleSystem system, Vector3 pos, Vector3 velocity, float size, float life, Color color)
        {
            system.Emit(new ParticleSystem.EmitParams
            {
                position = pos, velocity = velocity, startSize = size, startLifetime = life, startColor = color
            }, 1);
        }

        private void Update()
        {
            if (!burst) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) Stop();
            else PoseEchoes(elapsed / duration);
        }

        private void PoseEchoes(float t)
        {
            if (echoes == null) return;
            echoMaterial.SetFloat("_Strength", Mathf.SmoothStep(0, 1, t*6f) * Mathf.SmoothStep(0, 1, (1f-t)*4f));
            float arc = Mathf.Sin(Mathf.PI*t);
            for (int i = 0; i < echoes.Length; i++)
            {
                if (!echoes[i]) continue;
                Vector3 offset, angles;
                if (kind == WjyCatchKind.Whale)
                {
                    offset = new Vector3((t-.5f)*8f, -1.45f + arc*4.7f, .75f*arc);
                    angles = new Vector3(Mathf.Lerp(30f, -32f, t), -95f, 16f*arc);
                }
                else if (kind == WjyCatchKind.Fish)
                {
                    offset = new Vector3((t-.5f)*2.8f+(i%2)*.52f, .24f + arc*.9f+(i/2)*.29f, i*.19f);
                    angles = new Vector3(0, Mathf.Sin(t*12f+i)*10f, -78f);
                }
                else if (kind == WjyCatchKind.Seal)
                {
                    offset = new Vector3((t-.5f)*2.4f, .05f + arc*1.65f, 0f);
                    angles = new Vector3(Mathf.Lerp(50f,-50f,t), -112f, 18f);
                }
                else
                {
                    offset = new Vector3(.22f*Mathf.Sin(Tau*t), .05f + arc*1.2f, .1f*arc);
                    angles = new Vector3(-16f*arc, -25f + t*95f, 6f*arc);
                }
                echoes[i].SetPositionAndRotation(stage + bearing * offset, bearing * Quaternion.Euler(angles));
            }
        }

        public void Stop()
        {
            if (burst)
            {
                burst.SetActive(false);
                Destroy(burst);
            }
            burst = null;
            echoes = null;
        }

        private void OnDisable() => Stop();
        private void OnDestroy()
        {
            if (sprayMaterial) Destroy(sprayMaterial);
            if (echoMaterial) Destroy(echoMaterial);
            foreach (var mesh in echoMeshes)
                if (mesh) Destroy(mesh);
        }
    }
}
