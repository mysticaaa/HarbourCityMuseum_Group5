using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ZzxWorkspace
{
    public static class CabinPrototypeBuilder
    {
        private const string Folder = "Assets/zzx_workspace/Generated";
        [MenuItem("Harbour City Museum/ZZX/Create Cabin Prototype Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before generating the cabin preview.");
                return;
            }
            // Scene creation must run outside Prefab isolation mode.
            StageUtility.GoToMainStage();
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            string prefabPath = Folder + "/CabinPrototype.prefab";
            string scenePath = Folder + "/CabinPreview.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                Debug.LogWarning("ZZX prototype already exists. Open the existing preview; generation skipped.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                CreatePreview(prefabPath, scenePath);
                return;
            }
            var root = new GameObject("ZZX_CabinPrototype");
            try
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Placeholder.mat");
                if (mat == null)
                {
                mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.55f, 0.42f, 0.28f);
                AssetDatabase.CreateAsset(mat, Folder + "/Placeholder.mat");
                }
                Make(root.transform, mat, CabinExhibit.ExhibitKind.Compass, "Compass", new Vector3(-3, 1, 0),
                    PrimitiveType.Cylinder, new Vector3(0.8f, 0.1f, 0.8f),
                    "The needle points toward the configured north. Rotate the exhibit root to test heading. Placeholder interpretation.");
                Make(root.transform, mat, CabinExhibit.ExhibitKind.Wheel, "Wheel", new Vector3(-1, 1.5f, 0),
                    PrimitiveType.Cube, new Vector3(0.8f, 0.12f, 0.12f),
                    "Click to turn the wheel in steps. This prototype does not steer or move the ship.");
                Make(root.transform, mat, CabinExhibit.ExhibitKind.Painting, "Painting", new Vector3(1, 1.5f, 0),
                    PrimitiveType.Cube, new Vector3(1.1f, 0.8f, 0.08f),
                    "Placeholder wall picture. Replace this text with sourced museum interpretation; click again to close.");
                Make(root.transform, mat, CabinExhibit.ExhibitKind.Chair, "Chair", new Vector3(3, 0.6f, 0),
                    PrimitiveType.Cube, new Vector3(0.7f, 0.15f, 0.7f),
                    "Inspect the seat. No automatic sitting, teleportation or camera movement is performed.");
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
                CreatePreview(prefabPath, scenePath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void CreatePreview(string prefabPath, string scenePath)
        {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
                    var camObj = new GameObject("PreviewCamera");
                    SceneManager.MoveGameObjectToScene(camObj, scene);
                    camObj.transform.position = new Vector3(0, 2.8f, -6);
                    camObj.transform.LookAt(new Vector3(0, 1, 0));
                    var cam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                    var input = instance.AddComponent<CabinDesktopPreview>();
                    input.previewCamera = cam;
                    var lightObj = new GameObject("PreviewLight");
                    SceneManager.MoveGameObjectToScene(lightObj, scene);
                    lightObj.transform.rotation = Quaternion.Euler(45, -30, 0);
                    lightObj.AddComponent<Light>().type = LightType.Directional;
                    if (!EditorSceneManager.SaveScene(scene, scenePath))
                        throw new System.InvalidOperationException("Unable to save cabin preview: " + scenePath);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("ZZX assets saved. Open Assets/zzx_workspace/Generated/CabinPreview.unity and press Play.");
        }

        private static void Make(Transform parent, Material mat, CabinExhibit.ExhibitKind kind,
            string name, Vector3 position, PrimitiveType shape, Vector3 scale, string text)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "ReplaceableVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = mat;
            var script = root.AddComponent<CabinExhibit>();
            script.kind = kind;
            script.title = name;
            script.information = text;
            if (kind == CabinExhibit.ExhibitKind.Wheel) script.movingPart = visual.transform;
            if (kind == CabinExhibit.ExhibitKind.Compass)
            {
                var pivot = new GameObject("NeedlePivot");
                pivot.transform.SetParent(root.transform, false);
                pivot.transform.localPosition = new Vector3(0, 0.15f, 0);
                var needle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                needle.name = "NorthNeedle";
                needle.transform.SetParent(pivot.transform, false);
                needle.transform.localPosition = new Vector3(0, 0, 0.15f);
                needle.transform.localScale = new Vector3(0.05f, 0.05f, 0.35f);
                needle.GetComponent<Renderer>().sharedMaterial = mat;
                script.movingPart = pivot.transform;
                script.shipHeading = root.transform;
            }
            var serialized = new SerializedObject(script);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("description").stringValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
