using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 操作提示系统 — 在指定位置显示交互引导文字。
/// 挂载到一个带有 Collider (isTrigger) 的空物体上。
/// 玩家进入触发范围时显示提示，离开时隐藏。
///
/// 支持两种模式：
///   1. Proximity — 玩家走近时自动显示
///   2. Always — 始终显示（适合全局提示如"摇杆移动"）
///
/// 用法：
///   1. 创建空物体 → 加 BoxCollider (isTrigger, size 约 2x2x2)
///   2. 在其下创建 Canvas (World Space) → 加 Text 子物体
///   3. 把此脚本挂到空物体上
///   4. 把 Canvas 拖到 hintCanvas 引用
///   5. 设置 hintText 内容
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
[RequireComponent(typeof(Collider))]
public class OperationHint : MonoBehaviour
{
    public enum HintMode
    {
        Proximity,
        Always
    }

    [Header("Hint configuration")]
    [SerializeField] private HintMode mode = HintMode.Proximity;
    [TextArea(2, 4)]
    [SerializeField] private string hintText = "Point at the object and press trigger to interact";
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Color textColor = new Color(0.95f, 0.93f, 0.88f);

    [Header("Visual settings")]
    [SerializeField] private Canvas hintCanvas;
    [SerializeField] private Text hintTextComponent;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float hoverHeight = 0.3f;

    [Header("Optional — face camera")]
    [SerializeField] private bool faceCamera = true;

    private Transform camTransform;
    private CanvasGroup canvasGroup;
    private float currentAlpha = 0f;
    private bool playerInRange = false;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (hintCanvas == null)
            hintCanvas = GetComponentInChildren<Canvas>(true);

        if (hintCanvas != null)
        {
            canvasGroup = hintCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = hintCanvas.gameObject.AddComponent<CanvasGroup>();

            if (hintTextComponent == null)
                hintTextComponent = hintCanvas.GetComponentInChildren<Text>(true);

            ApplyText();
            hintCanvas.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;

        if (mode == HintMode.Always)
        {
            currentAlpha = 1f;
            if (hintCanvas != null)
                hintCanvas.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (mode == HintMode.Always)
        {
            HandleFacing();
            return;
        }

        float targetAlpha = playerInRange ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        if (canvasGroup != null)
            canvasGroup.alpha = currentAlpha;

        if (currentAlpha < 0.01f && hintCanvas != null && hintCanvas.gameObject.activeSelf)
            hintCanvas.gameObject.SetActive(false);
        else if (currentAlpha > 0.01f && hintCanvas != null && !hintCanvas.gameObject.activeSelf)
            hintCanvas.gameObject.SetActive(true);

        HandleFacing();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
            if (hintCanvas != null && !hintCanvas.gameObject.activeSelf)
                hintCanvas.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            playerInRange = false;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.CompareTag("MainCamera")
            || other.GetComponentInParent<CharacterController>() != null;
    }

    private void HandleFacing()
    {
        if (!faceCamera || camTransform == null || hintCanvas == null) return;

        Vector3 lookDir = hintCanvas.transform.position - camTransform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(lookDir);
            hintCanvas.transform.rotation = Quaternion.Slerp(
                hintCanvas.transform.rotation, target, Time.deltaTime * 6f);
        }
    }

    /// <summary>
    /// 运行时修改提示文本。
    /// </summary>
    public void SetHintText(string newText)
    {
        hintText = newText;
        ApplyText();
    }

    private void ApplyText()
    {
        if (hintTextComponent != null)
        {
            hintTextComponent.text = hintText;
            hintTextComponent.fontSize = fontSize;
            hintTextComponent.color = textColor;
            hintTextComponent.alignment = TextAnchor.MiddleCenter;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 0.5f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 center = transform.position + col.bounds.center - transform.position;
            Gizmos.DrawWireCube(center, col.bounds.size);
        }
    }
}
