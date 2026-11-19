using UnityEngine;

[DisallowMultipleComponent]
public sealed class MuseumOpening : MonoBehaviour
{
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform leftHinge;
    [SerializeField] private Transform rightHinge;
    [SerializeField, Min(0.1f)] private float openDistance = 2.1f;
    [SerializeField, Min(0.1f)] private float closeDistance = 3f;
    [SerializeField, Min(0.1f)] private float openingDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float closingDuration = 2.5f;

    private Camera view;
    private Quaternion leftClosed;
    private Quaternion rightClosed;
    private bool openRequested;
    private float progress;

    private void Awake()
    {
        if (entrance == null || leftHinge == null || rightHinge == null)
        {
            enabled = false;
            return;
        }

        leftClosed = leftHinge.localRotation;
        rightClosed = rightHinge.localRotation;
    }

    private void LateUpdate()
    {
        if (view == null || !view.isActiveAndEnabled)
            view = Camera.main;
        if (view == null || view.gameObject.scene != gameObject.scene)
            return;

        Vector3 fromEntrance = Vector3.ProjectOnPlane(view.transform.position - entrance.position, Vector3.up);
        Vector3 across = Vector3.ProjectOnPlane(rightHinge.position - leftHinge.position, Vector3.up);
        float halfWidth = across.magnitude * 0.5f;
        Vector3 axis = across.normalized;
        float along = Mathf.Clamp(Vector3.Dot(fromEntrance, axis), -halfWidth, halfWidth);
        float distanceSquared = (fromEntrance - axis * along).sqrMagnitude;
        float threshold = openRequested ? Mathf.Max(openDistance, closeDistance) : openDistance;
        openRequested = distanceSquared <= threshold * threshold;

        float target = openRequested ? 1f : 0f;
        float duration = openRequested ? openingDuration : closingDuration;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);

        float angle = Mathf.SmoothStep(0f, 104f, progress);
        leftHinge.localRotation = leftClosed * Quaternion.Euler(0f, -angle, 0f);
        rightHinge.localRotation = rightClosed * Quaternion.Euler(0f, angle, 0f);
    }
}
