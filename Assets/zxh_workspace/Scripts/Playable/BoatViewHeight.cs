using UnityEngine;
using Unity.XR.CoreUtils;

namespace ZxhMuseum
{
    // Lift the tracked head AND controllers together only aboard. Never overwrite head poses.
    public sealed class BoatViewHeight : MonoBehaviour
    {
        private XROrigin origin;
        private Vector3 savedOffset;
        private float savedHeight, raisedOffset;
        private bool applied;
        public void Apply(XROrigin rig)
        {
            if (applied || rig.CameraFloorOffsetObject == null) return;
            origin = rig; savedHeight = rig.CameraYOffset;
            savedOffset = rig.CameraFloorOffsetObject.transform.localPosition;
            float eyeHeight = rig.transform.InverseTransformPoint(rig.Camera.transform.position).y;
            float lift = Mathf.Clamp(1.65f - eyeHeight, .15f, .65f);
            rig.CameraYOffset = savedHeight + lift;
            raisedOffset = savedOffset.y + lift;
            applied = true; LateUpdate();
        }
        private void LateUpdate()
        {
            if (!applied || origin == null || origin.CameraFloorOffsetObject == null) return;
            var offset = origin.CameraFloorOffsetObject.transform.localPosition;
            offset.y = raisedOffset;
            origin.CameraFloorOffsetObject.transform.localPosition = offset;
        }
        public void Restore()
        {
            if (!applied || origin == null) return;
            applied = false; origin.CameraYOffset = savedHeight;
            if (origin.CameraFloorOffsetObject != null) origin.CameraFloorOffsetObject.transform.localPosition = savedOffset;
        }
        private void OnDestroy() => Restore();
    }
}
