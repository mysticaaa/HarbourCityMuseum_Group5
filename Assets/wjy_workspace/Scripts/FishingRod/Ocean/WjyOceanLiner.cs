using UnityEngine;
using UnityEngine.Rendering;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyOceanLiner : MonoBehaviour
    {
        // The 26.92 m hull must stay inside the 66 m water mesh on both headings.
        private const float RouteEnd = 42f;
        private const float Lane = 32f;
        private const float Speed = 1.65f;
        private readonly System.Random schedule = new System.Random();
        private Transform vessel;
        private AudioSource horn;
        private ParticleSystem[] funnels;
        private Material paint, exhaust;
        private float trackPosition, wait, rollTime, hornPosition;
        private float silhouetteHeight;
        private bool reversing, sounded;

        private void Awake()
        {
            var mesh = Resources.Load<Mesh>("WjyPassengerLiner");
            var hullShader = Resources.Load<Shader>("WjyOceanRaftSurface");
            var smokeShader = Resources.Load<Shader>("WjyLinerSmoke");
            var hornClip = Resources.Load<AudioClip>("WjyLinerHorn");
            if (!mesh || !hullShader || !smokeShader || !hornClip)
            {
                Debug.LogError("The passing ocean liner is missing a mesh, shader or horn.", this);
                enabled = false;
                return;
            }

            var hull = new GameObject("RMS Titanic", typeof(MeshFilter), typeof(MeshRenderer));
            vessel = hull.transform;
            vessel.SetParent(transform, false);
            hull.GetComponent<MeshFilter>().sharedMesh = mesh;
            silhouetteHeight = mesh.bounds.max.y + .35f;
            paint = new Material(hullShader);
            paint.SetFloat("_Surface", 2f);
            var renderer = hull.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = paint;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            exhaust = new Material(smokeShader);
            // Three boiler uptakes. Titanic's aftmost funnel was used for ventilation.
            funnels = new[] {
                CreateFunnel("No. 1 funnel smoke", new Vector3(4.89f, 4.10f, 0f)),
                CreateFunnel("No. 2 funnel smoke", new Vector3(2.16f, 4.10f, 0f)),
                CreateFunnel("No. 3 funnel smoke", new Vector3(-.57f, 4.10f, 0f))
            };
            horn = hull.AddComponent<AudioSource>();
            horn.playOnAwake = false;
            horn.spatialBlend = 1f;
            horn.rolloffMode = AudioRolloffMode.Linear;
            horn.minDistance = 8f;
            horn.maxDistance = 84f;
            horn.dopplerLevel = .22f;
            horn.volume = .52f;
            horn.clip = hornClip;
        }

        private ParticleSystem CreateFunnel(string label, Vector3 nozzle)
        {
            var outlet = new GameObject(label);
            outlet.transform.SetParent(vessel, false);
            outlet.transform.localPosition = nozzle;
            var smoke = outlet.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main;
            main.playOnAwake = false;
            main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.3f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.55f, .76f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = smoke.shape;
            shape.enabled = false;
            var emission = smoke.emission;
            emission.rateOverTime = 7f;
            var wind = smoke.velocityOverLifetime;
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.Local;
            wind.x = new ParticleSystem.MinMaxCurve(-.75f, -.43f);
            wind.y = new ParticleSystem.MinMaxCurve(.65f, .9f);
            wind.z = new ParticleSystem.MinMaxCurve(-.12f, .17f);
            var size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, .8f, 1f, 2.9f));
            var fade = smoke.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(.29f,.30f,.35f), 0f),
                                     new GradientColorKey(new Color(.65f,.62f,.68f), 1f) },
                             new[] { new GradientAlphaKey(.72f, 0f), new GradientAlphaKey(.48f, .42f),
                                     new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = smoke.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = exhaust;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return smoke;
        }

        private void OnEnable()
        {
            if (!vessel) return;
            reversing = false;
            rollTime = 0f;
            BeginPass(-23f);
        }

        private void BeginPass(float start)
        {
            trackPosition = start;
            hornPosition = -11f + (float)schedule.NextDouble() * 4f;
            sounded = false;
            wait = 0f;
            vessel.gameObject.SetActive(true);
            Pose();
        }

        private void Update()
        {
            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                if (wait <= 0f) BeginPass(-RouteEnd);
                return;
            }

            trackPosition += Speed * Time.deltaTime;
            rollTime += Time.deltaTime;
            if (trackPosition >= RouteEnd)
            {
                StopPass();
                reversing = !reversing;
                wait = 19f + (float)schedule.NextDouble() * 18f;
                return;
            }
            Pose();
            if (!sounded && trackPosition >= hornPosition)
            {
                sounded = true;
                horn.Play();
            }
        }

        private void Pose()
        {
            // The hull gradually passes below the far horizon at both ends of its lane.
            float outward = Mathf.InverseLerp(RouteEnd - 16f, RouteEnd, Mathf.Abs(trackPosition));
            float horizonDrop = Mathf.SmoothStep(0f, silhouetteHeight, outward);
            float x = reversing ? -trackPosition : trackPosition;
            var point = new Vector3(x, 0f, Lane);
            point.y = WjyOceanEnvironment.WaveHeight(point, Time.time) - horizonDrop;
            vessel.localPosition = point;
            vessel.localRotation = Quaternion.Euler(.65f * Mathf.Sin(rollTime*.68f),
                reversing ? 180f : 0f, .28f * Mathf.Sin(rollTime*.9f));

            foreach (var smoke in funnels)
            {
                bool visible = outward < .58f;
                if (visible && !smoke.isEmitting) smoke.Play(false);
                if (!visible && smoke.isEmitting) smoke.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void StopPass()
        {
            horn.Stop();
            foreach (var smoke in funnels)
                smoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            vessel.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (horn) StopPass();
        }

        private void OnDestroy()
        {
            if (paint) Destroy(paint);
            if (exhaust) Destroy(exhaust);
        }
    }
}
