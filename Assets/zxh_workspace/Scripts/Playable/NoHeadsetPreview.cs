using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace ZxhMuseum
{
    /// <summary>Desktop preview; never overrides a tracked headset.</summary>
    public sealed class NoHeadsetPreview : MonoBehaviour
    {
        private XROrigin origin;
        private QuestExhibitSetup exhibits;
        private bool preview;
        private float pitch;
        private Vector3 cameraPosition, floorPosition;
        private Quaternion cameraRotation;
        private float nearClip, farClip, fieldOfView;
        private bool driverEnabled;
        private readonly List<Behaviour> legacyRenderers = new List<Behaviour>();
        public void Initialize(QuestExhibitSetup setup) { exhibits = setup; origin = setup.xrOrigin; }
        public void RecenterLook()
        {
            if (!preview || origin == null) return;
            pitch = 0; origin.Camera.transform.localRotation = Quaternion.identity;
        }
        private bool HeadTracked()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked;
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            if (origin == null || HeadTracked() || Application.isBatchMode) yield break;
            if (!Application.isEditor && !Debug.isDebugBuild) yield break;
            foreach (var legacy in FindObjectsOfType<Valve.VR.SteamVR_Render>())
                if (legacy.enabled) { legacyRenderers.Add(legacy); legacy.enabled = false; }
            if (legacyRenderers.Count > 0 && Time.timeScale == 0) Time.timeScale = 1;
            var driver = origin.Camera.GetComponent<TrackedPoseDriver>();
            cameraPosition = origin.Camera.transform.localPosition;
            cameraRotation = origin.Camera.transform.localRotation;
            floorPosition = origin.CameraFloorOffsetObject != null ? origin.CameraFloorOffsetObject.transform.localPosition : Vector3.zero;
            nearClip = origin.Camera.nearClipPlane;
            farClip = origin.Camera.farClipPlane;
            fieldOfView = origin.Camera.fieldOfView;
            driverEnabled = driver != null && driver.enabled;
            if (driver != null) driver.enabled = false;
            preview = true;
            origin.Camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            // Remove the extra camera-floor offset in desktop preview; the camera carries eye height.
            if (origin.CameraFloorOffsetObject != null) origin.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
            origin.Camera.transform.localRotation = Quaternion.identity;
            Bounds bounds = ExhibitInspectionHint.GetBounds(exhibits.ship);
            bounds.Encapsulate(ExhibitInspectionHint.GetBounds(exhibits.helmet));
            float distance = Mathf.Max(3f, bounds.extents.x / Mathf.Tan(35 * Mathf.Deg2Rad) + 1f);
            origin.transform.position = new Vector3(bounds.center.x, 0, bounds.center.z + distance);
            origin.transform.rotation = Quaternion.Euler(0, 180, 0);
            origin.Camera.nearClipPlane = 0.03f; origin.Camera.farClipPlane = 1000;
            origin.Camera.fieldOfView = 70;
            if (MuseumSession.Instance != null && MuseumSession.Instance.InCabin)
            {
                var deck = FindObjectOfType<ShipDeckEnvironment>();
                if (deck != null) MuseumSession.Instance.MoveTo(deck.SpawnPoint, 0);
            }
            else MuseumSession.Instance?.RepositionWelcome();
            Debug.Log("[ZXH] No tracked headset: desktop preview enabled. WASD move; hold right mouse to look. XR hardware interactions still need Quest.");
        }
        private void Update()
        {
            WorldPanel.UpdateInputMode();
            if (!preview || origin == null) return;
            if (HeadTracked())
            {
                preview = false;
                foreach (var legacy in legacyRenderers) if (legacy != null) legacy.enabled = true;
                legacyRenderers.Clear();
                origin.Camera.transform.localPosition = cameraPosition;
                origin.Camera.transform.localRotation = cameraRotation;
                if (origin.CameraFloorOffsetObject != null)
                {
                    var offset = floorPosition;
                    if (origin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Floor) offset.y = 0;
                    else if (origin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Device) offset.y = origin.CameraYOffset;
                    origin.CameraFloorOffsetObject.transform.localPosition = offset;
                }
                origin.Camera.nearClipPlane = nearClip;
                origin.Camera.farClipPlane = farClip;
                origin.Camera.fieldOfView = fieldOfView;
                var driver = origin.Camera.GetComponent<TrackedPoseDriver>();
                if (driver != null) driver.enabled = driverEnabled;
                return;
            }
            if (MuseumSession.Instance != null && (MuseumSession.Instance.Busy || MuseumSession.Instance.Seated)) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector3 input = Vector3.zero;
                if (keyboard.wKey.isPressed) input.z++; if (keyboard.sKey.isPressed) input.z--;
                if (keyboard.dKey.isPressed) input.x++; if (keyboard.aKey.isPressed) input.x--;
                if (MuseumSession.Instance == null || !MuseumSession.Instance.InCabin)
                    origin.transform.position += origin.transform.TransformDirection(input.normalized) * Time.deltaTime * 2;
            }
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue() * 0.1f;
                origin.transform.Rotate(Vector3.up, delta.x, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -70, 70);
                origin.Camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
        }
    }
}
