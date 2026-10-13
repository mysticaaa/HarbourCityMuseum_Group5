using UnityEngine;

namespace ZzxWorkspace
{
    public class CabinExhibit : InteractableObject
    {
        public enum ExhibitKind { Compass, Wheel, Painting, Chair, EngineControl, Layout }
        public ExhibitKind kind;
        public Transform movingPart;
        public Transform shipHeading;
        public Vector3 northDirection = Vector3.forward;
        public Vector3 wheelAxis = Vector3.forward;
        public float wheelStep = 10f;
        public float wheelLimit = 90f;
        public float inspectScale = 1.6f;
        public bool enableIdleRotation = true;
        [Min(0f)] public float idleDelay = 3f;
        public float idleRotationSpeed = 12f;
        public Vector3 idleRotationAxis = Vector3.up;
        public float inspectionDistance = 2.4f;
        public float dragSensitivity = 0.4f;
        public string title;
        [TextArea] public string information;
        public CabinNavigationSimulation navigation;
        public Camera inspectionCamera;
        public bool IsOpen { get; private set; }
        public bool IsInspecting => IsOpen;
        public bool IsOperating { get; private set; }
        public float WheelAngle { get; private set; }
        public float TargetWheelAngle => targetAngle;
        public static CabinExhibit OpenExhibit => openExhibit;
        private static CabinExhibit openExhibit;
        private Vector3 originalPosition, originalScale, visualCentre;
        private Quaternion originalRotation, partRotation;
        private float lastInteractionTime, targetAngle, inspectFactor;
        private bool initialized;

        protected override void Awake()
        {
            base.Awake();
            originalPosition = transform.localPosition;
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
            partRotation = movingPart != null ? movingPart.localRotation : Quaternion.identity;
            initialized = true;
            lastInteractionTime = Time.time;
        }

        public override void OnSelect()
        {
            if (!IsInteractable || IsOpen) return;
            if (openExhibit != null) openExhibit.Close();
            base.OnSelect();
            lastInteractionTime = Time.time;
            IsOpen = true;
            IsOperating = false;
            openExhibit = this;
            if (inspectionCamera == null) inspectionCamera = Camera.main;
            // Reset presentation spin before calculating a stable bounding sphere.
            transform.localRotation = originalRotation;
            var renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.one * 0.2f);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            visualCentre = transform.InverseTransformPoint(bounds.center);
            inspectFactor = Mathf.Max(1f, inspectScale);
            if (inspectionCamera != null && !inspectionCamera.orthographic)
            {
                float halfFov = Mathf.Atan(Mathf.Tan(inspectionCamera.fieldOfView * Mathf.Deg2Rad / 2f) *
                    Mathf.Min(1f, inspectionCamera.aspect));
                float distance = Mathf.Max(inspectionDistance, inspectionCamera.nearClipPlane + 1f);
                float maxRadius = distance * Mathf.Sin(halfFov) * 0.65f;
                inspectFactor = Mathf.Min(inspectFactor, maxRadius / Mathf.Max(0.05f, bounds.extents.magnitude));
            }
            transform.localScale = originalScale * inspectFactor;
            CentreInView();
        }

        private void CentreInView()
        {
            if (inspectionCamera == null) return;
            float distance = Mathf.Max(inspectionDistance, inspectionCamera.nearClipPlane + 1f);
            Vector3 centre = inspectionCamera.transform.position + inspectionCamera.transform.forward * distance;
            transform.position = centre - transform.TransformVector(visualCentre);
        }

        public void DragView(Vector2 delta)
        {
            if (!IsOpen || IsOperating || inspectionCamera == null) return;
            transform.Rotate(inspectionCamera.transform.up, -delta.x * dragSensitivity, Space.World);
            transform.Rotate(inspectionCamera.transform.right, delta.y * dragSensitivity, Space.World);
            CentreInView();
        }

        public void DragWheel(float horizontalDelta)
        {
            if (IsOpen && IsOperating) SetWheelAngle(targetAngle + horizontalDelta * dragSensitivity);
        }

        public void SetOperating(bool value)
        {
            if (kind != ExhibitKind.Wheel && kind != ExhibitKind.EngineControl) return;
            IsOperating = value;
            // Inspection rotation is independent of the actual wheel angle.
            transform.localRotation = originalRotation;
            CentreInView();
            if (!value && kind == ExhibitKind.Wheel) { SetWheelAngle(0); if (navigation != null) navigation.rudderAngle = 0; }
        }

        public override void OnDeselect() { Close(); }
        public override void OnHoverEnter() { lastInteractionTime = Time.time; base.OnHoverEnter(); }
        public override void OnHoverExit() { lastInteractionTime = Time.time; base.OnHoverExit(); }

        private void Update()
        {
            if (IsOpen) CentreInView();
            else if (enableIdleRotation && IsInteractable && !IsHovered &&
                Time.time - lastInteractionTime >= Mathf.Max(0, idleDelay) && idleRotationAxis.sqrMagnitude > 0.001f)
                transform.Rotate(idleRotationAxis.normalized, idleRotationSpeed * Time.deltaTime, Space.Self);
            if (movingPart == null) return;
            if (kind == ExhibitKind.Wheel)
            {
                WheelAngle = Mathf.MoveTowards(WheelAngle, targetAngle, 90f * Time.deltaTime);
                Vector3 axis = wheelAxis.sqrMagnitude > 0.001f ? wheelAxis.normalized : Vector3.forward;
                movingPart.localRotation = partRotation * Quaternion.AngleAxis(WheelAngle, axis);
                if (navigation != null && IsOperating && IsOpen) navigation.rudderAngle = WheelAngle;
            }
            else if (kind == ExhibitKind.Compass)
            {
                // Presentation rotation never changes the shared simulated heading.
                float angle = navigation != null ? -navigation.Heading : 0f;
                if (navigation == null)
                {
                    Vector3 north = Vector3.ProjectOnPlane(northDirection, Vector3.up);
                    Vector3 forward = Vector3.ProjectOnPlane(shipHeading != null ? shipHeading.forward : Vector3.forward, Vector3.up);
                    if (north.sqrMagnitude > 0.001f && forward.sqrMagnitude > 0.001f)
                        angle = Vector3.SignedAngle(forward, north, Vector3.up);
                }
                movingPart.localRotation = partRotation * Quaternion.AngleAxis(angle, Vector3.up);
            }
            else if (kind == ExhibitKind.EngineControl && navigation != null)
                movingPart.localRotation = partRotation * Quaternion.AngleAxis(navigation.Gear * 35f * navigation.Throttle, Vector3.right);
        }

        public void Close()
        {
            if (!initialized) return;
            if (navigation != null && IsOperating && kind == ExhibitKind.Wheel) navigation.rudderAngle = 0f;
            var layout = GetComponent<CabinLayoutView>();
            if (layout != null) layout.ClearHighlight();
            IsOpen = IsOperating = false;
            lastInteractionTime = Time.time;
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;
            base.OnDeselect();
            if (openExhibit == this) openExhibit = null;
        }

        public void ResetExhibit()
        {
            Close();
            targetAngle = WheelAngle = 0;
            if (movingPart != null) movingPart.localRotation = partRotation;
            if (navigation != null) navigation.ResetNavigation();
        }
        public void SetWheelAngle(float angle) { targetAngle = Mathf.Clamp(angle, -Mathf.Abs(wheelLimit), Mathf.Abs(wheelLimit)); }
        private void OnDisable() { Close(); }
        private static Rect PanelRect => new Rect(10, 10, Mathf.Min(270f, Screen.width * 0.27f), Mathf.Min(430f, Screen.height - 20f));
        public static bool PointerOverPanel(Vector2 point)
        {
            return openExhibit != null && PanelRect.Contains(new Vector2(point.x, Screen.height - point.y));
        }
        private void OnGUI()
        {
            if (!IsOpen) return;
            GUILayout.BeginArea(PanelRect, GUI.skin.box);
            GUILayout.Label(title);
            GUILayout.Label(information);
            GUILayout.Label(IsOperating ? "OPERATION: hold left mouse and drag left/right." : "INSPECTION: hold left mouse and drag to view all sides.");
            if (kind == ExhibitKind.Wheel)
            {
                if (GUILayout.Button(IsOperating ? "Return to inspection" : "Operate wheel")) SetOperating(!IsOperating);
                if (IsOperating)
                {
                    GUILayout.Label("Wheel angle: " + WheelAngle.ToString("F0") + " degrees");
                    SetWheelAngle(GUILayout.HorizontalSlider(targetAngle, -Mathf.Abs(wheelLimit), Mathf.Abs(wheelLimit)));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Left -10")) SetWheelAngle(targetAngle - 10);
                    if (GUILayout.Button("Centre")) SetWheelAngle(0);
                    if (GUILayout.Button("Right +10")) SetWheelAngle(targetAngle + 10);
                    GUILayout.EndHorizontal();
                    if (Mathf.Abs(targetAngle) >= Mathf.Abs(wheelLimit)) GUILayout.Label("Rotation limit reached.");
                    GUILayout.Label("Heading changes only in this teaching simulation. Visitor camera stays fixed.");
                }
            }
            if (kind == ExhibitKind.EngineControl)
            {
                if (GUILayout.Button(IsOperating ? "Return to inspection" : "Operate engine lever")) SetOperating(!IsOperating);
                if (IsOperating && navigation != null)
                {
                    if (GUILayout.Button("Neutral / Stop propulsion")) navigation.SetGear(0);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Ahead")) navigation.SetGear(1);
                    if (GUILayout.Button("Astern")) navigation.SetGear(-1);
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Power (reduce to zero before changing gear)");
                    navigation.SetThrottle(GUILayout.HorizontalSlider(navigation.Throttle, 0f, 1f));
                    GUILayout.Label(navigation.Instruction);
                    GUILayout.Label("Neutral lets simulated speed decay; it is not an instant brake.");
                }
            }
            var layoutView = GetComponent<CabinLayoutView>();
            if (layoutView != null) layoutView.DrawControls();
            if (GUILayout.Button("Close / Return")) Close();
            if (GUILayout.Button("Reset object + heading")) ResetExhibit();
            GUILayout.EndArea();
        }
    }
}
