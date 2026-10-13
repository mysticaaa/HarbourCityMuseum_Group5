using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 射线检测 / 选中控制器 —— Harbour City Museum (Group 5)
///
/// 功能：
///   1. 每帧从射线起点发出射线，实时检测命中的碰撞体
///   2. 只有“可选中物体”才能被选中；可选中物体在 Inspector 里自定义指定
///      （显式列表 selectableObjects，或按 Layer 过滤 selectableLayers）
///   3. 悬停 → 射线变黄；选中 → 射线变绿
///
/// 使用方法：
///   1. 把本脚本挂到“射线起点”的物体上（VR 手柄控制器 / XR Origin / 相机）
///   2. 在 Inspector 的 Selectable Objects 里拖入你想让玩家选中的物体
///   3. 桌面测试：鼠标左键；VR：手柄扳机键
///
/// 不戴头显怎么验证：
///   - 快速验证：勾上 Follow Mouse Cursor，脚本挂到 Main Camera 上，
///     射线就会跟着鼠标光标走，左键即可选中
///   - 验证手柄配置：脚本挂到 Right Controller，Play 时在 Inspector 里
///     拖动它的 Transform 让射线指向展品，再用鼠标左键选中
///
/// 注意：可被选中的物体必须带 Collider（MeshCollider / BoxCollider 等均可）。
/// </summary>
public class RadiographicInspection : MonoBehaviour
{
    [Header("Hover Target")]
    [SerializeField] private GameObject helmetModelPlaceholder;
    // ─────────────────────────── 射线来源 ───────────────────────────

    [Header("射线来源")]
    [Tooltip("射线发出的起点。留空 = 用本脚本所在的物体自身（一般留空即可）")]
    [SerializeField] private Transform rayOrigin;

    // ─────────────────────────── 射线参数 ───────────────────────────

    [Header("射线参数")]
    [Tooltip("射线最大长度（米）")]
    [SerializeField] private float maxDistance = 10f;

    [Tooltip("射线粗细")]
    [SerializeField] private float rayWidth = 0.01f;

    [Tooltip("参与射线检测的层（默认全部）")]
    [SerializeField] private LayerMask raycastLayers = ~0;

    // ─────────────────────── 可选中物体（自定义指定） ───────────────────────


    // ─────────────────────────── 射线颜色 ───────────────────────────

    [Header("射线颜色")]
    [Tooltip("自定义线条材质，留空则自动创建")]
    [SerializeField] private Material rayMaterial;

    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorHover = Color.green;
    [Tooltip("选中后的颜色（绿色）")]
    [SerializeField] private Color colorSelected = Color.red;

    // ─────────────────────────── 输入 ───────────────────────────

    [Header("输入")]
    [Tooltip("桌面调试：射线跟着鼠标光标走（从 Camera.main 发射），不用戴头显也能指哪打哪。正式 VR 请取消勾选")]
    [SerializeField] private bool followMouseCursor = false;

    [Tooltip("桌面测试：鼠标左键")]
    [SerializeField] private bool useMouseInput = true;

    [Tooltip("VR：手柄扳机键")]
    [SerializeField] private bool useXRInput = true;

    [Tooltip("读哪个手柄的扳机")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;

    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;

    // ─────────────────────────── 事件 ───────────────────────────

    /// <summary>射线开始指向某个可选中物体</summary>
    public event Action<GameObject> OnHoverEnter;
    /// <summary>射线离开该物体</summary>
    public event Action<GameObject> OnHoverExit;
    /// <summary>选中（按下扳机 / 左键时正指着可选中物体）</summary>
    public event Action<GameObject> OnSelect;
    /// <summary>取消选中</summary>
    public event Action<GameObject> OnDeselect;

    // ─────────────────────────── 对外属性 ───────────────────────────

    /// <summary>当前射线指向的可选中物体，没有则为 null</summary>
    public GameObject HoveredObject => hovered;

    /// <summary>当前已选中的物体，没有则为 null</summary>
    public GameObject SelectedObject => selected;

    /// <summary>当前是否有选中的物体</summary>
    public bool HasSelection => selected != null;

    // ─────────────────────────── 内部状态 ───────────────────────────

    private LineRenderer line;

    private GameObject hovered;
    private GameObject selected;
    private readonly List<MaterialColorState> highlightedMaterialColors = new List<MaterialColorState>();

    private struct MaterialColorState
    {
        public Material material;
        public string propertyName;
        public Color color;
    }

    private Vector3 rayStart;
    private Vector3 rayDirection;
    private Vector3 hitPoint;
    private bool hitSomething;
    private GameObject lastHit;          // 本帧射线打到的物体（不做可选中判断）

    private float nextStateLogTime;      // 心跳日志节流
    private RaycastHit[] raycastHitBuffer = new RaycastHit[16];

    private bool xrTriggerHeld;   // 上一帧扳机状态，用于做“按下瞬间”判定

    // ─────────────────────────── 生命周期 ───────────────────────────

    private void Awake()
    {
        if (followMouseCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        line = GetComponent<LineRenderer>();
        if (line == null)
            line = gameObject.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        // In desktop look mode the ray follows the camera's forward axis.
        // Keep the ribbon aligned to that axis and use a flat end to avoid a
        // camera-facing cap that appears to twist as the view rotates.
        line.alignment = LineAlignment.TransformZ;
        line.startWidth = rayWidth;
        line.endWidth = rayWidth;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        if (rayMaterial != null)
        {
            line.material = rayMaterial;
        }
        else
        {
            // Built-in 管线下的兜底材质
            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
        }

        line.startColor = colorNormal;
        line.endColor = colorNormal;
    }

    private void Update()
    {
        UpdateRay();        // 1. 检测命中的物体
        UpdateRayVisual();  // 3. 更新射线颜色与长度
        LogRayState();      // 4. 心跳日志（每秒一条，确认脚本在运行）
    }

    /// <summary>
    /// 每秒打印一次当前射线状态，用于确认脚本是否正常运行。
    /// 正式展出前把 Inspector 里的 Enable Debug Log 取消勾选即可关掉。
    /// </summary>
    private void LogRayState()
    {
        if (!enableDebugLog || Time.time < nextStateLogTime)
            return;

        nextStateLogTime = Time.time + 1f;

        string originName = (followMouseCursor && Camera.main != null)
            ? $"{Camera.main.name} (鼠标光标)"
            : ResolveRayOrigin().name;
        if (lastHit == null)
        {
            Debug.Log($"[RadiographicInspection] 运行中 | 起点: {originName} | 未命中任何物体（射线长度 {maxDistance}m）");
        }
        else
        {
            Debug.Log($"[RadiographicInspection] 运行中 | 起点: {originName} | 命中: {lastHit.name} | 可选中: {(IsSelectable(lastHit) ? "是" : "否")}");
        }
    }

    private void OnDisable()
    {
        RestoreHoveredObjectColor();
        if (line != null) line.enabled = false;
    }

    private void OnDestroy()
    {
        OnHoverEnter = null;
        OnHoverExit = null;
        OnSelect = null;
        OnDeselect = null;
    }

    // ─────────────────────────── 1. 射线检测 ───────────────────────────

    /// <summary>
    /// 每帧发出射线，找出当前指向的可选中物体（实时检测碰撞体）。
    /// </summary>
    private void UpdateRay()
    {
        if (followMouseCursor && Camera.main != null)
        {
            // 桌面调试模式：射线跟随鼠标光标
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            rayStart = mouseRay.origin;
            rayDirection = mouseRay.direction;
        }
        else
        {
            Transform origin = ResolveRayOrigin();
            rayStart = origin.position;
            rayDirection = origin.forward;
        }

        hitSomething = false;
        lastHit = null;
        GameObject target = null;

        // Collect all colliders in range so a nearer unrelated object does not
        // prevent detection of HelmetModel_Placeholder behind it.
        int hitCount;
        while (true)
        {
            hitCount = Physics.RaycastNonAlloc(
                rayStart, rayDirection, raycastHitBuffer, maxDistance,
                raycastLayers, QueryTriggerInteraction.Ignore);

            // A full buffer may mean Unity omitted some hits. Grow and retry.
            if (hitCount < raycastHitBuffer.Length)
                break;

            Array.Resize(ref raycastHitBuffer, raycastHitBuffer.Length * 2);
        }

        bool foundAnyHit = false;
        bool foundTargetHit = false;
        RaycastHit closestHit = default;
        RaycastHit closestTargetHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = raycastHitBuffer[i];
            if (candidate.collider == null)
                continue;

            if (!foundAnyHit || candidate.distance < closestHit.distance)
            {
                closestHit = candidate;
                foundAnyHit = true;
            }

            GameObject listedTarget = FindListedObject(candidate.collider.gameObject);
            if (listedTarget != null &&
                (!foundTargetHit || candidate.distance < closestTargetHit.distance))
            {
                closestTargetHit = candidate;
                target = listedTarget;
                foundTargetHit = true;
            }
        }

        hitSomething = foundAnyHit;
        if (foundTargetHit)
        {
            // Extend the inspection beam to the helmet that triggered the highlight.
            hitPoint = closestTargetHit.point;
            lastHit = closestTargetHit.collider.gameObject;
        }
        else if (foundAnyHit)
        {
            hitPoint = closestHit.point;
            lastHit = closestHit.collider.gameObject;
        }
        SetHovered(target);

        // 可选：不按键，碰到即选中
    }

    /// <summary>
    /// 判断某个物体（含其子物体）是否属于“可选中物体”。
    /// </summary>
    public bool IsSelectable(GameObject target)
    {
        if (target == null)
            return false;

        return FindListedObject(target) != null;
    }

    /// <summary>
    /// 在 selectableObjects 列表里查找命中的物体（会沿父级向上找）。
    /// 找不到返回 null。
    /// </summary>
    private GameObject FindListedObject(GameObject hitObject)
    {
        if (hitObject == null || helmetModelPlaceholder == null)
            return null;

        Transform hitTransform = hitObject.transform;
        Transform targetTransform = helmetModelPlaceholder.transform;
        return hitTransform == targetTransform || hitTransform.IsChildOf(targetTransform)
            ? helmetModelPlaceholder
            : null;
    }

    /// <summary>
    /// 解析射线起点：优先用 Inspector 指定的，留空则用本物体自身。
    /// </summary>
    private Transform ResolveRayOrigin()
    {
        return rayOrigin != null ? rayOrigin : transform;
    }

    // ─────────────────────────── 2. 选中逻辑 ───────────────────────────

    /// <summary>
    /// 读取选中输入：鼠标左键，或手柄扳机的“按下瞬间”。
    /// </summary>
    private void HandleInput()
    {
        if (!GetSelectDown())
            return;

        if (hovered != null)
        {
            if (selected != hovered)
                Select(hovered);
            else
                Deselect();     // 再按一次取消选中
        }
        else
        {
            Deselect();         // 指着空处时取消选中
        }
    }

    private bool GetSelectDown()
    {
        bool pressedThisFrame = false;

        if (useMouseInput && Input.GetMouseButtonDown(0))
            pressedThisFrame = true;

        if (useXRInput)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            bool held = device.isValid &&
                        device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed;

            // 扳机是“按住为真”的状态值，只在按下那一帧返回 true
            if (held && !xrTriggerHeld)
                pressedThisFrame = true;

            xrTriggerHeld = held;
        }

        return pressedThisFrame;
    }

    private void SetHovered(GameObject target)
    {
        if (hovered == target)
            return;

        if (hovered != null)
        {
            OnHoverExit?.Invoke(hovered);
            RestoreHoveredObjectColor();
        }

        hovered = target;

        if (hovered != null)
        {
            ApplyHoveredObjectColor(hovered);
            OnHoverEnter?.Invoke(hovered);
        }
    }

    private void ApplyHoveredObjectColor(GameObject target)
    {
        RestoreHoveredObjectColor();

        foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in targetRenderer.materials)
            {
                if (material == null)
                    continue;

                string propertyName = material.HasProperty("_BaseColor") ? "_BaseColor" :
                    material.HasProperty("_Color") ? "_Color" : null;
                if (propertyName == null)
                    continue;

                highlightedMaterialColors.Add(new MaterialColorState
                {
                    material = material,
                    propertyName = propertyName,
                    color = material.GetColor(propertyName)
                });
                material.SetColor(propertyName, Color.green);
            }
        }
    }

    private void RestoreHoveredObjectColor()
    {
        foreach (MaterialColorState state in highlightedMaterialColors)
        {
            if (state.material != null)
                state.material.SetColor(state.propertyName, state.color);
        }
        highlightedMaterialColors.Clear();
    }

    /// <summary>
    /// 选中某个物体（会先取消上一个选中）。
    /// </summary>
    public void Select(GameObject target)
    {
        if (target == null || selected == target)
            return;

        Deselect();

        selected = target;
        OnSelect?.Invoke(selected);

        if (enableDebugLog) Debug.Log($"[RadiographicInspection] 选中: {selected.name}");
    }

    /// <summary>
    /// 取消当前选中（射线颜色随之恢复）。
    /// </summary>
    public void Deselect()
    {
        if (selected == null)
            return;

        GameObject previous = selected;
        selected = null;

        OnDeselect?.Invoke(previous);

        if (enableDebugLog) Debug.Log($"[RadiographicInspection] 取消选中: {previous.name}");
    }

    /// <summary>
    /// 对外提供的手动清空接口。
    /// </summary>
    public void ClearSelection()
    {
        Deselect();
    }

    // ─────────────────────────── 3. 射线表现 ───────────────────────────

    private void UpdateRayVisual()
    {
        if (line == null)
            return;

        line.enabled = true;

        // Only an allowed target (or one of its children) sets hovered.
        Color color = hovered != null ? colorHover : colorNormal;

        // Keep vertex color and material tint in sync so the effect also works
        // with materials whose shader does not use LineRenderer vertex colors.
        line.startColor = color;
        line.endColor = color;
        Material activeMaterial = line.material;
        if (activeMaterial != null)
        {
            if (activeMaterial.HasProperty("_BaseColor"))
                activeMaterial.SetColor("_BaseColor", color);
            if (activeMaterial.HasProperty("_Color"))
                activeMaterial.SetColor("_Color", color);
        }

        Vector3 end = hitSomething ? hitPoint : rayStart + rayDirection * maxDistance;
        line.SetPosition(0, rayStart);
        line.SetPosition(1, end);
    }
}
