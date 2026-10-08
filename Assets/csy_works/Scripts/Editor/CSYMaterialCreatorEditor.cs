#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 给 CSYMaterialCreatorRuntime 在 Inspector 面板加一个显眼的"生成材质"按钮。
/// 放在 Editor 文件夹里，不需要手动挂载。
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
[CustomEditor(typeof(CSYMaterialCreatorRuntime))]
public class CSYMaterialCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
            DrawDefaultInspector();

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.5f);
            if (GUILayout.Button("Create Room Materials", GUILayout.Height(40)))
            {
                var creator = (CSYMaterialCreatorRuntime)target;
                creator.CreateAll();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("点击按钮会在 Assets/csy_works/Materials/ 下生成 9 个房间基础材质。", MessageType.Info);
        }
}
#endif
