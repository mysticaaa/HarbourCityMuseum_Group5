using UnityEngine;

namespace ZxhMuseum
{
    /// <summary>One active inspection view; switching objects always cleans up the old copy.</summary>
    public sealed class ObservationService : MonoBehaviour
    {
        private WorldPanel panel;
        private InspectionReplica copy;
        public Transform Current { get; private set; }
        public void Open(Transform source, string title, string text)
        {
            Close(); Current = source;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            Vector3 at = camera.transform.position + forward * 0.9f + Vector3.down * 0.2f;
            panel = WorldPanel.Create(transform, title, transform.InverseTransformPoint(at),
                Quaternion.Inverse(transform.rotation) * Quaternion.LookRotation(forward), 650);
            panel.Status.text = text;
            copy = InspectionReplica.Create(source, transform, at + Vector3.up * 0.45f, 0.35f);
            panel.Button("Turn left", () => copy?.Turn(-30));
            panel.Button("Turn right", () => copy?.Turn(30));
            panel.Button("Close / restore", Close);
        }
        public void Close()
        {
            if (copy != null) Destroy(copy.gameObject);
            if (panel != null) Destroy(panel.gameObject);
            copy = null; panel = null; Current = null;
        }
        private void OnDisable() => Close();
    }
}
