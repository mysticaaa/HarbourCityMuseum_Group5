#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>Editor-only desktop preview for the personal UI scene.</summary>
[InitializeOnLoad]
public static class CSYDesktopUIPreview
{
    private const string ScenePath = "Assets/csy_workspace/Scenes/Exhibition_CSY_UI.unity";
    private const string EnabledKey = "CSY.UI.DesktopPreview.Enabled";
    private const string PendingKey = "CSY.UI.DesktopPreview.RestorePending";
    private const string OriginalKey = "CSY.UI.DesktopPreview.OriginalInit";
    private const string SettingsKey = "CSY.UI.DesktopPreview.SettingsPath";
    private const string MenuPath = "CSY Tools/Desktop UI Preview (personal scene only)";

    static CSYDesktopUIPreview()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += RecoverCancelledPlay;
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        bool enabled = !SessionState.GetBool(EnabledKey, true);
        SessionState.SetBool(EnabledKey, enabled);
        Debug.Log("[CSY UI] Desktop preview " + (enabled ? "enabled" : "disabled; Play will start XR"));
    }
    [MenuItem(MenuPath, true)]
    private static bool Validate()
    {
        Menu.SetChecked(MenuPath, SessionState.GetBool(EnabledKey, true));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (!SessionState.GetBool(EnabledKey, true)
                || (SceneManager.GetActiveScene().path != ScenePath
                    && SceneManager.GetActiveScene().path != "Assets/csy_workspace/Scenes/Timeline_CSY_Preview.unity")) return;
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget
                .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings == null) return;
            SessionState.SetString(SettingsKey, AssetDatabase.GetAssetPath(settings));
            SessionState.SetBool(OriginalKey, settings.InitManagerOnStart);
            SessionState.SetBool(PendingKey, true);
            settings.InitManagerOnStart = false;
            Debug.Log("[CSY UI] Desktop preview: XR startup skipped for this Play session. Click Game, then M.");
        }
        else if (state == PlayModeStateChange.EnteredEditMode) Restore();
    }
    private static void RecoverCancelledPlay()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) Restore();
    }
    private static void Restore()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        XRGeneralSettings settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettings>(
            SessionState.GetString(SettingsKey, ""));
        if (settings == null) return;
        settings.InitManagerOnStart = SessionState.GetBool(OriginalKey, true);
        SessionState.SetBool(PendingKey, false);
        Debug.Log("[CSY UI] Original XR startup setting restored.");
    }
}
#endif
