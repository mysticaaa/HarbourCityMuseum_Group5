using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ZzxWorkspace
{
    public static class MotorFishingBoatBuilder
    {
        private const string Folder = "Assets/zzx_workspace/Generated";
        [MenuItem("Harbour City Museum/ZZX/Create Motor Fishing Boat Preview")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play first."); return; }
            StageUtility.GoToMainStage();
            const string scenePath = Folder + "/MotorFishingBoatPreview.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            { Debug.Log("Motor preview already exists. Open " + scenePath); return; }
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/CabinPrototype.prefab");
            if (original == null) { Debug.LogError("Create Cabin Prototype Assets first."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var root = Object.Instantiate(original);
                root.name = "MotorFishingBoat_TeachingPrototype";
                SceneManager.MoveGameObjectToScene(root, scene);
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Placeholder.mat");
                var engine = Exhibit(root.transform, "EngineLever", new Vector3(-1.7f, 1, -1.6f), CabinExhibit.ExhibitKind.EngineControl,
                    "Motor control (teaching model)", "Inspect the lever, then Operate. Gear positions and dimensions are schematic; not a documented historical installation.");
                Shape(engine.transform, "Base", Vector3.zero, new Vector3(0.6f, 0.15f, 0.5f), material);
                var pivot = new GameObject("LeverPivot");
                pivot.transform.SetParent(engine.transform, false);
                Shape(pivot.transform, "Handle", new Vector3(0, 0.3f, 0), new Vector3(0.08f, 0.6f, 0.08f), material);
                engine.movingPart = pivot.transform;
                var layout = Exhibit(root.transform, "CabinLayout", new Vector3(1.7f, 0.8f, -1.6f), CabinExhibit.ExhibitKind.Layout,
                    "Cabin and seating layout", "Select a zone to explore access and work space. Seat count and arrangement are placeholders awaiting vessel evidence.");
                Shape(layout.transform, "Deck", Vector3.zero, new Vector3(1.3f, 0.06f, 2f), material);
                var view = layout.gameObject.AddComponent<CabinLayoutView>();
                string[] labels = { "Helm/work position", "Living/seating area", "Fishing/work area", "Access route", "Entrance / exit" };
                string[] descriptions = {
                    "Conceptual control position. Confirm sight lines and helm location using vessel records.",
                    "Schematic seating/rest area. No historical seat count is asserted.",
                    "Conceptual fishing workspace. Gear and location depend on vessel type.",
                    "Example clear access route between areas; verify against the actual vessel layout.",
                    "Illustrative entrance marker, not a certified emergency exit plan." };
                Vector3[] positions = { new Vector3(-0.3f, 0.12f, -0.65f), new Vector3(0.3f, 0.12f, -0.2f),
                    new Vector3(-0.3f, 0.12f, 0.45f), new Vector3(0, 0.08f, 0), new Vector3(0.45f, 0.1f, 0.85f) };
                view.zones = new CabinLayoutView.Zone[labels.Length];
                for (int i = 0; i < labels.Length; i++)
                {
                    Vector3 size = i == 3 ? new Vector3(0.12f, 0.04f, 1.6f) : new Vector3(0.3f, 0.18f, 0.3f);
                    var part = Shape(layout.transform, labels[i], positions[i], size, material);
                    view.zones[i] = new CabinLayoutView.Zone { label = labels[i], description = descriptions[i], renderers = new[] { part.GetComponent<Renderer>() } };
                }
                string prefab = Folder + "/MotorFishingBoatPrototype.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null) PrefabUtility.SaveAsPrefabAsset(root, prefab);
                var cameraObject = new GameObject("DesktopPreviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.transform.position = new Vector3(0, 3, -7);
                cameraObject.transform.LookAt(new Vector3(0, 1, 0));
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                root.AddComponent<CabinDesktopPreview>().previewCamera = camera;
                var lightObject = new GameObject("PreviewLight");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
                lightObject.AddComponent<Light>().type = LightType.Directional;
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new System.InvalidOperationException("Scene save failed.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Open " + scenePath + ", then Play. Start with engine ahead and power, then operate the wheel.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        private static CabinExhibit Exhibit(Transform parent, string name, Vector3 position, CabinExhibit.ExhibitKind kind, string title, string info)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var script = root.AddComponent<CabinExhibit>();
            script.kind = kind; script.title = title; script.information = info;
            var serialized = new SerializedObject(script);
            serialized.FindProperty("displayName").stringValue = title;
            serialized.FindProperty("description").stringValue = info;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return script;
        }
        private static GameObject Shape(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shape.name = name; shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position; shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            return shape;
        }
    }
}
