using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class QuestExhibitProjectSetup
{
    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        if (SessionState.GetBool("ZXH.QuestSetup", false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Configure();
            SessionState.SetBool("ZXH.QuestSetup", true);
        };
    }

    [MenuItem("ZXH/Quest 3/Configure Museum OpenXR")]
    public static void Configure()
    {
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
            out XRGeneralSettingsPerBuildTarget store))
            throw new System.InvalidOperationException("XR settings store is missing.");
        const string folder = "Assets/zxh_workspace/QuestSettings";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/zxh_workspace", "QuestSettings");
        foreach (BuildTargetGroup target in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            if (target == BuildTargetGroup.Android &&
                !BuildPipeline.IsBuildTargetSupported(target, BuildTarget.Android))
            {
                Debug.LogWarning("[ZXH] Android Build Support is not installed. Quest Link is configured; install Android SDK/NDK/OpenJDK and run this menu again before building an APK.");
                continue;
            }
            if (!store.HasSettingsForBuildTarget(target)) store.CreateDefaultSettingsForBuildTarget(target);
            XRGeneralSettings settings = store.SettingsForBuildTarget(target);
            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                AssetDatabase.CreateAsset(manager, folder + "/" + target + "XRManager.asset");
                settings.Manager = manager;
            }
            if (!XRPackageMetadataStore.AssignLoader(settings.Manager,
                "UnityEngine.XR.OpenXR.OpenXRLoader", target))
                throw new System.InvalidOperationException("Could not assign OpenXR to " + target);
            // Prefer OpenXR for Quest Link; keep other providers available to the team.
            settings.Manager.TrySetLoaders(settings.Manager.activeLoaders
                .OrderBy(loader => loader is OpenXRLoader ? 0 : 1).ToList());
            settings.InitManagerOnStart = true;
            settings.Manager.automaticLoading = true;
            settings.Manager.automaticRunning = true;
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(target);
            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (openXR == null) throw new System.InvalidOperationException("OpenXR settings unavailable for " + target);
            Enable<OculusTouchControllerProfile>(openXR);
            Enable<MetaQuestTouchPlusControllerProfile>(openXR);
            if (target == BuildTargetGroup.Android) Enable<MetaQuestFeature>(openXR);
            EditorUtility.SetDirty(openXR);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
        }
        EditorUtility.SetDirty(store);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == "Assets/Scenes/Museum.unity"))
            scenes.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/Museum.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[ZXH] Museum OpenXR configuration finished for installed build platforms. Test with Quest 3 before release.");
    }

    private static void Enable<T>(OpenXRSettings settings)
        where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
    {
        var feature = settings.GetFeature<T>();
        if (feature == null) throw new System.InvalidOperationException("OpenXR feature missing: " + typeof(T).Name);
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }
}
