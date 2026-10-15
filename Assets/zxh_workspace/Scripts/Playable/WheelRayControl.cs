using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    public sealed class WheelRayControl : XRSimpleInteractable
    {
        public Transform wheelVisual;
        public VoyageState navigation;
        private IXRSelectInteractor holder;
        private float previous;
        private bool havePrevious;
        private Quaternion initialRotation;
        protected override void Awake()
        {
            base.Awake();
            selectMode = InteractableSelectMode.Single;
        }
        private void Start() { initialRotation = wheelVisual.localRotation; }
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args); holder = args.interactorObject; havePrevious = false;
        }
        protected override void OnSelectExited(SelectExitEventArgs args)
        { base.OnSelectExited(args); holder = null; havePrevious = false; }
        private void Update()
        {
            if (wheelVisual == null || navigation == null) return;
            if (holder != null)
            {
                Vector3 point;
                if (holder is XRRayInteractor ray)
                {
                    var controller = ray.GetComponent<ActionBasedController>();
                    if (controller != null && controller.enabled &&
                        (controller.currentControllerState.inputTrackingState & (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation))
                            != (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation))
                    { havePrevious = false; return; }
                    Transform origin = ray.rayOriginTransform != null ? ray.rayOriginTransform : ray.transform;
                    var plane = new Plane(transform.forward, transform.position);
                    var inputRay = new Ray(origin.position, origin.forward);
                    if (!plane.Raycast(inputRay, out float distance) || distance > 10)
                    { havePrevious = false; return; }
                    point = inputRay.GetPoint(distance);
                }
                else point = holder.GetAttachTransform(this).position;
                Vector3 local = transform.InverseTransformPoint(point);
                if (new Vector2(local.x, local.y).magnitude < 0.06f) { havePrevious = false; return; }
                float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
                // Visitor stands on the wheel's +Z side: increasing local angle appears clockwise.
                if (havePrevious) navigation.SetWheel(navigation.WheelAngle + Mathf.DeltaAngle(previous, angle));
                previous = angle; havePrevious = true;
            }
            wheelVisual.localRotation = initialRotation * Quaternion.AngleAxis(navigation.WheelAngle, Vector3.forward);
        }
    }
}
