using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Wjy.Museum
{
    // The floor supplies the ray hit: this marker adds no collider to the walkway.
    [DisallowMultipleComponent]
    public sealed class WjyFishingTeleportRing : MonoBehaviour
    {
        [SerializeField] private WjyFishingRodCatchInteraction rod;
        [SerializeField] private Shader markerShader;
        [SerializeField] private Vector3 localLanding = new Vector3(0f, 0f, 1.25f);
        [SerializeField, Min(.1f)] private float radius = .43f;
        [SerializeField] private ExhibitLanding[] exhibitLandings;

        [System.Serializable]
        private struct ExhibitLanding
        {
            public string name;
            public Vector3 position;
        }

        private sealed class Destination
        {
            public Vector3 position;
            public Material material;
            public GameObject marker;
        }

        private XROrigin rig;
        private XRRayInteractor[] rays;
        private readonly List<XRInteractorLineVisual> adjustedLines = new List<XRInteractorLineVisual>();
        private readonly List<Destination> destinations = new List<Destination>();
        private TeleportationProvider provider;
        private Mesh mesh;
        private WjyMuseumPauseMenu menu;
        private bool leftTriggerWasDown = true, rightTriggerWasDown = true;

        private void Start()
        {
            rig = rod ? rod.SharedRig : null;
            if (!rig || !markerShader) { enabled = false; return; }

            Physics.SyncTransforms();
            mesh = CreateMarkerMesh();
            AddDestination("Fishing", transform.TransformPoint(localLanding), transform.forward);
            if (gameObject.scene.name == "Museum_261122_1")
            {
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    if (root.name == "--- Harbour City Museum Scene ---")
                    {
                        var floor = root.transform.Find("Room/Floor");
                        if (floor && floor.TryGetComponent<Collider>(out var collider) && exhibitLandings != null)
                            foreach (var exhibit in exhibitLandings)
                                AddExhibitDestination(exhibit, collider);
                    }
                    if (root.name != "Video Players") continue;
                    foreach (var player in root.GetComponentsInChildren<VideoPlayer>())
                    {
                        if (!player.targetMaterialRenderer) continue;
                        var screen = player.targetMaterialRenderer.transform;
                        var front = Vector3.ProjectOnPlane(-screen.forward, Vector3.up).normalized;
                        var expected = screen.position + front * 1.55f;
                        expected.y = transform.position.y;
                        AddDestination(player.name, expected, front);
                    }
                }
            }

            rays = rig.GetComponentsInChildren<XRRayInteractor>(true);
            menu = GetComponent<WjyMuseumPauseMenu>();
            KeepControllerRaysExtended();
        }

        private void AddExhibitDestination(ExhibitLanding exhibit, Collider floor)
        {
            // Keep the ring centered on the exhibit, stepping into the aisle if the display blocks it.
            for (int step = 0; step <= 10; step++)
            {
                var sample = exhibit.position + Vector3.forward * (.25f * step);
                var down = new Ray(sample + Vector3.up, Vector3.down);
                if (!floor.Raycast(down, out var hit, 2f)) continue;

                // The room's visible floor sits slightly above its base plane.
                var surfaceRay = new Ray(hit.point + Vector3.up * .06f, Vector3.down);
                if (!Physics.Raycast(surfaceRay, out var surface, .075f, ~0, QueryTriggerInteraction.Ignore)
                    || surface.normal.y < .85f || !HasStandingRoom(surface.point)) continue;
                CreateMarker(exhibit.name, surface.point);
                return;
            }
            Debug.LogWarning("[WJY] No clear floor for teleport destination: " + exhibit.name, this);
        }

        private void AddDestination(string label, Vector3 expected, Vector3 towardAisle)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Vector3 sample = expected + towardAisle * (.35f * attempt);
                Vector3 landing = default;
                float bestHeightError = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(sample + Vector3.up * .35f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.normal.y < .85f || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(rig.transform)) continue;
                    float error = Mathf.Abs(hit.point.y - sample.y);
                    if (error >= bestHeightError) continue;
                    landing = hit.point;
                    bestHeightError = error;
                }
                if (float.IsPositiveInfinity(bestHeightError) || !HasStandingRoom(landing)) continue;
                CreateMarker(label, landing);
                return;
            }
            Debug.LogWarning("[WJY] No clear floor for teleport destination: " + label, this);
        }

        private void OnEnable()
        {
            leftTriggerWasDown = rightTriggerWasDown = true;
            foreach (var destination in destinations) destination.marker.SetActive(true);
            if (rays != null) KeepControllerRaysExtended();
        }

        private void LateUpdate()
        {
            if (destinations.Count == 0) return;
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            UpdateAimAndTeleport(left.isValid, IsTriggerDown(left), right.isValid, IsTriggerDown(right));
        }

        private static bool IsTriggerDown(InputDevice device)
        {
            return device.TryGetFeatureValue(CommonUsages.triggerButton, out bool button) ? button :
                device.TryGetFeatureValue(CommonUsages.trigger, out float value) && value >= .65f;
        }

        private void UpdateAimAndTeleport(bool leftConnected, bool leftPressed, bool rightConnected, bool rightPressed)
        {
            bool leftJustPressed = leftConnected && leftPressed && !leftTriggerWasDown;
            bool rightJustPressed = rightConnected && rightPressed && !rightTriggerWasDown;
            leftTriggerWasDown = !leftConnected || leftPressed;
            rightTriggerWasDown = !rightConnected || rightPressed;

            bool canAim = rod && !rod.HoldingRay && (!menu || !menu.IsOpen);
            var left = canAim && leftConnected ? FindAimedDestination("Left Controller") : null;
            var right = canAim && rightConnected ? FindAimedDestination("Right Controller") : null;
            var selected = (leftJustPressed ? left : null) ?? (rightJustPressed ? right : null);
            if (selected != null) TryTeleport(selected.position);
            foreach (var destination in destinations)
                destination.material.SetColor("_Tint", destination == left || destination == right
                    ? new Color(.55f, 1f, .75f) : new Color(.07f, .8f, 1f));
        }

        private Destination FindAimedDestination(string controller)
        {
            foreach (var ray in rays)
            {
                if (!ray || !ray.isActiveAndEnabled || ray.hasSelection || !IsControllerRay(ray, controller)) continue;
                if (!ray.TryGetCurrentRaycast(out var hit, out _, out _, out _, out bool uiClosest)
                    || uiClosest || !hit.HasValue || hit.Value.normal.y < .85f) continue;
                foreach (var destination in destinations)
                {
                    var offset = hit.Value.point - destination.position;
                    if (Mathf.Abs(offset.y) <= .05f && offset.x * offset.x + offset.z * offset.z <= radius * radius)
                        return destination;
                }
            }
            return null;
        }

        private bool IsControllerRay(XRRayInteractor ray, string controller)
        {
            for (var parent = ray.transform; parent && parent != rig.transform; parent = parent.parent)
                if (parent.name.Contains(controller)) return true;
            return false;
        }

        private void KeepControllerRaysExtended()
        {
            // The ring reads a physics floor hit, not an XRI interactable, so the
            // line visual would otherwise retract even when that floor hit is usable.
            foreach (var ray in rays)
            {
                if (!ray || !(IsControllerRay(ray, "Left Controller") || IsControllerRay(ray, "Right Controller")))
                    continue;
                if (!ray.TryGetComponent<XRInteractorLineVisual>(out var line) || !line.autoAdjustLineLength)
                    continue;
                line.autoAdjustLineLength = false;
                adjustedLines.Add(line);
            }
        }

        private bool HasStandingRoom(Vector3 landing)
        {
            foreach (var obstacle in Physics.OverlapCapsule(landing + Vector3.up * .32f,
                landing + Vector3.up * 1.45f, .23f, ~0, QueryTriggerInteraction.Ignore))
                if (!obstacle.transform.IsChildOf(rig.transform)) return false;
            return true;
        }

        private void TryTeleport(Vector3 landing)
        {
            if (!rig || !rig.Camera || !HasStandingRoom(landing)) return;

            if (!provider || !provider.isActiveAndEnabled || !provider.system
                || !provider.system.isActiveAndEnabled || provider.system.xrOrigin != rig)
                provider = ResolveProvider();
            if (!provider || provider.system.busy) return;

            provider.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = landing,
                destinationRotation = Quaternion.identity,
                matchOrientation = MatchOrientation.None,
                requestTime = Time.time,
            });
        }

        private TeleportationProvider ResolveProvider()
        {
            foreach (var candidate in FindObjectsOfType<TeleportationProvider>())
                if (candidate.isActiveAndEnabled && candidate.system && candidate.system.isActiveAndEnabled
                    && candidate.system.xrOrigin == rig) return candidate;

            LocomotionSystem system = null;
            foreach (var candidate in FindObjectsOfType<LocomotionSystem>())
                if (candidate.isActiveAndEnabled && candidate.xrOrigin == rig) { system = candidate; break; }
            if (!system)
            {
                system = gameObject.AddComponent<LocomotionSystem>();
                system.xrOrigin = rig;
            }
            var created = system.gameObject.AddComponent<TeleportationProvider>();
            created.system = system;
            return created;
        }

        private Mesh CreateMarkerMesh()
        {
            const int segments = 64;
            var vertices = new Vector3[1 + segments * 3];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments * 9];
            colors[0] = new Color(1, 1, 1, .22f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                int disk = 1 + i * 3, inner = disk + 1, outer = disk + 2;
                int next = 1 + ((i + 1) % segments) * 3;
                vertices[disk] = vertices[inner] = direction * (radius - .035f);
                vertices[outer] = direction * radius;
                colors[disk] = colors[0];
                colors[inner] = colors[outer] = Color.white;
                int t = i * 9;
                triangles[t] = 0; triangles[t + 1] = next; triangles[t + 2] = disk;
                triangles[t + 3] = inner; triangles[t + 4] = next + 1; triangles[t + 5] = outer;
                triangles[t + 6] = outer; triangles[t + 7] = next + 1; triangles[t + 8] = next + 2;
            }
            return new Mesh { name = "Museum teleport marker", vertices = vertices, colors = colors, triangles = triangles };
        }

        private void CreateMarker(string label, Vector3 landing)
        {
            var material = new Material(markerShader);
            var marker = new GameObject(label + " Teleport Ring");
            marker.transform.SetParent(transform, false);
            marker.transform.SetPositionAndRotation(landing + Vector3.up * .012f, Quaternion.identity);
            marker.AddComponent<MeshFilter>().sharedMesh = mesh;
            marker.AddComponent<MeshRenderer>().sharedMaterial = material;
            destinations.Add(new Destination { position = landing, marker = marker, material = material });
        }

        private void OnDisable()
        {
            foreach (var destination in destinations)
                if (destination.marker) destination.marker.SetActive(false);
            foreach (var line in adjustedLines)
                if (line) line.autoAdjustLineLength = true;
            adjustedLines.Clear();
        }
        private void OnDestroy()
        {
            foreach (var destination in destinations)
            {
                Destroy(destination.marker);
                Destroy(destination.material);
            }
            Destroy(mesh);
        }
    }
}
