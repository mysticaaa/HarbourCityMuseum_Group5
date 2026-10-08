#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一键创建跟随视角 UI（Canvas + ExitButton + BackButton + FollowViewUI 脚本）。
/// 菜单: CSY Tools > Create Follow View UI
/// 也可以选中任意 GameObject，Inspector 面板齿轮里点。
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
public static class CSYFollowViewUICreator
{
    [MenuItem("CSY Tools/Create Follow View UI")]
    public static void Create()
    {
        Canvas canvas = SetupFollowViewUI();
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[CSYFollowViewUICreator] FollowViewUI created. Press M at runtime to toggle.");
    }

    public static Canvas SetupFollowViewUI()
    {
        GameObject root = new GameObject("FollowViewUI");
        Undo.RegisterCreatedObjectUndo(root, "Create FollowViewUI");

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        root.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1f;
        scaler.referenceResolution = new Vector2(400, 200);
        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 200);
        rt.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        // 放在玩家前方（玩家起点 (0,0,3.03) 面朝 -Z），高度约 1.5m
        rt.position = new Vector3(0, 1.5f, 1.2f);
        // +Z 朝向玩家方向，文字不会镜像
        rt.rotation = Quaternion.Euler(0, 0, 0);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        CreateButton(root.transform, "ExitButton", "Exit", new Vector2(0, -50));
        CreateButton(root.transform, "BackButton", "Back", new Vector2(0, 50));

        FollowViewUI fvu = root.AddComponent<FollowViewUI>();

        return canvas;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(140, 60);
        btnRt.anchoredPosition = position;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.45f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 0.45f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.7f, 0.55f, 1f);
        colors.pressedColor = new Color(0.15f, 0.45f, 0.35f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(0, 0);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        return btn;
    }
}
#endif
