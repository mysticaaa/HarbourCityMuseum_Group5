using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    [Header("跟随设置")]
    [Tooltip("距离相机的固定前方距离")]
    [SerializeField] private float distance = 1.5f;

    [Tooltip("上下或左右的偏移量")]
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.2f, 0f);

    [Tooltip("跟随的平滑度（值越小越平滑，随动越慢）")]
    [SerializeField] private float smoothSpeed = 5f;

    private Transform mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            // 初始化时直接瞬移到视线正前方，避免开局时 UI 从远处飞过来
            transform.position = mainCameraTransform.position + mainCameraTransform.forward * distance + offset;
            transform.rotation = mainCameraTransform.rotation;
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform == null)
        {
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;
            else return;
        }

        // 计算目标位置：相机位置 + 相机正前方 * 距离 + 偏移量
        Vector3 targetPosition = mainCameraTransform.position + mainCameraTransform.forward * distance + offset;

        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // 让 UI 始终面向摄像机，但保持正立
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}