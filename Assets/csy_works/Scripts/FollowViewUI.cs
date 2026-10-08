using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 跟随视角的 World Space Canvas。
/// 挂载到带有 Canvas + GraphicRaycaster 的 GameObject 上。
/// 会自动跟随 XR Origin 的 Main Camera，保持固定距离悬浮在视野下方。
///
/// 按下右手柄 B/Y 键（或配置的按键）切换显示/隐藏。
/// 内置退出按钮和返回按钮的回调。
///
/// 用法：
///   1. 在场景中创建空物体 → 加 Canvas (World Space) + GraphicRaycaster
///   2. 在 Canvas 下创建 Exit Button、Back Button（命名为 "ExitButton"/"BackButton"）
///   3. 把此脚本挂到 Canvas 物体上
///   4. 运行后 UI 会自动跟随玩家视角
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
[RequireComponent(typeof(Canvas))]
public class FollowViewUI : MonoBehaviour
{
    [Header("Follow settings")]
    [SerializeField] private float followDistance = 1.8f;
    [SerializeField] private float heightOffset = -0.35f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float lookAtSpeed = 8f;

    [Header("Toggle")]
    [SerializeField] private bool startVisible = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [Header("Button events")]
    public UnityEvent onExitPressed;
    public UnityEvent onBackPressed;

    private Canvas canvas;
    private GraphicRaycaster raycaster;
    private Transform camTransform;
    private bool isVisible;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = gameObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1f;
        scaler.referenceResolution = new Vector2(400, 200);
        scaler.dynamicPixelsPerUnit = 10f;
    }

    void Start()
    {
        camTransform = Camera.main.transform;
        canvas.worldCamera = Camera.main;
        isVisible = startVisible;
        gameObject.SetActive(isVisible);

        AutoBindButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleVisible();
    }

    void LateUpdate()
    {
        if (!isVisible || camTransform == null) return;

        Vector3 forward = camTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        Vector3 targetPos = camTransform.position
            + forward * followDistance
            + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(
            transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // 方向：从 Canvas 指向相机，让 Canvas 正面（文字面）朝向玩家
        Vector3 lookDir = camTransform.position - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            // 只用 Y 轴旋转，避免 Canvas 倾斜
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                // 让 Canvas 正面朝向相机：LookRotation 后绕本地 Y 轴转 180°
                Quaternion targetRot = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0, 180, 0);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, Time.deltaTime * lookAtSpeed);
            }
        }
    }

    /// <summary>
    /// 切换 UI 显示/隐藏。
    /// </summary>
    public void ToggleVisible()
    {
        isVisible = !isVisible;
        gameObject.SetActive(isVisible);

        if (isVisible && camTransform != null)
        {
            Vector3 forward = camTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f) forward.Normalize();
            transform.position = camTransform.position
                + forward * followDistance
                + Vector3.up * heightOffset;

            // 立即设置朝向，避免显示那一帧文字还是镜像
            Vector3 lookDir = camTransform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0, 180, 0);
        }
    }

    /// <summary>
    /// 退出按钮回调 — 退出应用程序。
    /// </summary>
    public void OnExitButton()
    {
        Debug.Log("[FollowViewUI] Exit pressed");
        onExitPressed?.Invoke();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 返回按钮回调 — 返回上一个站点或重置当前。
    /// </summary>
    public void OnBackButton()
    {
        Debug.Log("[FollowViewUI] Back pressed");
        onBackPressed?.Invoke();
    }

    /// <summary>
    /// 自动查找名为 ExitButton / BackButton 的子物体并绑定 onClick。
    /// </summary>
    private void AutoBindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string name = btn.gameObject.name.ToLower();
            if (name.Contains("exit"))
            {
                btn.onClick.AddListener(OnExitButton);
                Debug.Log("[FollowViewUI] bound Exit button");
            }
            else if (name.Contains("back"))
            {
                btn.onClick.AddListener(OnBackButton);
                Debug.Log("[FollowViewUI] bound Back button");
            }
        }
    }
}
