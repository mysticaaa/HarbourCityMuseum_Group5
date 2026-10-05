using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Editor script that builds a grey-box museum scene from primitive shapes.
/// Run from menu: Harbour City Museum > Build Grey-Box Scene
/// 
/// Creates:
///   - Room environment (floor + 4 walls + ceiling)
///   - Ship model station (cube placeholder on platform)
///   - Timeline wall (plane placeholder)
///   - Helmet station (sphere placeholder on pedestal)
///   - Player area with SteamVR camera rig
///   - Basic lighting
///   - GameManager, AudioManager, UIManager, ExperienceFlow
///   - RaycastSelector on right controller
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
public class MuseumSceneBuilder : Editor
{
    [MenuItem("Harbour City Museum/Build Grey-Box Scene", false, 1)]
    public static void BuildScene()
    {
        // Create or use existing scene
        var existingScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        Debug.Log("[MuseumSceneBuilder] Building grey-box museum scene...");

        // Root container
        GameObject sceneRoot = new GameObject("--- Harbour City Museum Scene ---");

        // === Room Environment ===
        GameObject roomRoot = new GameObject("Room");
        roomRoot.transform.SetParent(sceneRoot.transform);

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(roomRoot.transform);
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        floor.transform.position = new Vector3(0f, 0f, 0f);
        SetStandardMaterial(floor, new Color(0.7f, 0.68f, 0.64f));

        // Ceiling
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(roomRoot.transform);
        ceiling.transform.localScale = new Vector3(3f, 1f, 3f);
        ceiling.transform.position = new Vector3(0f, 4f, 0f);
        ceiling.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        SetStandardMaterial(ceiling, new Color(0.85f, 0.83f, 0.80f));

        // Walls
        float wallHeight = 4f;
        float wallWidth = 15f;

        // Back wall
        GameObject backWall = CreateWall("Wall_Back", new Vector3(0f, wallHeight/2f, -7.5f),
            new Vector3(wallWidth, wallHeight, 0.5f), roomRoot.transform, new Color(0.8f, 0.78f, 0.74f));

        // Front wall
        GameObject frontWall = CreateWall("Wall_Front", new Vector3(0f, wallHeight/2f, 7.5f),
            new Vector3(wallWidth, wallHeight, 0.5f), roomRoot.transform, new Color(0.8f, 0.78f, 0.74f));

        // Left wall
        GameObject leftWall = CreateWall("Wall_Left", new Vector3(-7.5f, wallHeight/2f, 0f),
            new Vector3(0.5f, wallHeight, wallWidth), roomRoot.transform, new Color(0.78f, 0.76f, 0.72f));

        // Right wall
        GameObject rightWall = CreateWall("Wall_Right", new Vector3(7.5f, wallHeight/2f, 0f),
            new Vector3(0.5f, wallHeight, wallWidth), roomRoot.transform, new Color(0.78f, 0.76f, 0.72f));

        // === Ship Model Station ===
        GameObject shipStation = CreateShipStation(sceneRoot.transform);
        SetupStation(shipStation, "Ship Model Station", 0,
            "Ship Model", "Inspect the ship in 360, then enter to see the interior.");

        // === Timeline Wall Station ===
        GameObject timelineStation = CreateTimelineStation(sceneRoot.transform);
        SetupStation(timelineStation, "Timeline Station", 1,
            "Port-City Timeline", "Explore the maritime history of the port city.");

        // === Diving Helmet Station ===
        GameObject helmetStation = CreateHelmetStation(sceneRoot.transform);
        SetupStation(helmetStation, "Helmet Station", 2,
            "Diving Helmet", "Zoom in to examine the diving helmet's construction and history.");

        // === Lighting ===
        CreateLighting(sceneRoot.transform);

        // === Player Setup ===
        GameObject playerSetup = CreatePlayerSetup(sceneRoot.transform);

        // === Manager Setup ===
        CreateManagers(sceneRoot.transform, shipStation, timelineStation, helmetStation);

        // === UI Canvas ===
        CreateUICanvas(sceneRoot.transform);

        // Mark scene dirty
        EditorUtility.SetDirty(sceneRoot);

        Debug.Log("[MuseumSceneBuilder] Grey-box scene built successfully!");
        Debug.Log("[MuseumSceneBuilder] Next steps:");
        Debug.Log("  1. Save the scene as MainScene in Assets/Scenes/");
        Debug.Log("  2. Assign station references in GameManager inspector");
        Debug.Log("  3. Assign model references in each Station script");
        Debug.Log("  4. Press Play to test with mouse fallback");
        Debug.Log("  5. Connect Quest 3 to test in VR");
    }

    private static GameObject CreateWall(string name, Vector3 pos, Vector3 scale,
        Transform parent, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        SetStandardMaterial(wall, color);
        return wall;
    }

    private static GameObject CreateShipStation(Transform parent)
    {
        GameObject station = new GameObject("ShipStation");
        station.transform.SetParent(parent);
        station.transform.position = new Vector3(-4f, 0f, -3f);

        // Platform
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "DisplayPlatform";
        platform.transform.SetParent(station.transform);
        platform.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
        platform.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        SetStandardMaterial(platform, new Color(0.3f, 0.3f, 0.32f));

        // Ship placeholder (cube)
        GameObject ship = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ship.name = "ShipModel_Placeholder";
        ship.transform.SetParent(station.transform);
        ship.transform.localScale = new Vector3(0.8f, 0.5f, 0.3f);
        ship.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        SetStandardMaterial(ship, new Color(0.55f, 0.4f, 0.25f));

        // Add ShipStation component
        var shipScript = ship.AddComponent<ShipStation>();

        // Add collider trigger for station entry
        var triggerZone = station.AddComponent<BoxCollider>();
        triggerZone.size = new Vector3(3f, 2f, 3f);
        triggerZone.isTrigger = true;
        triggerZone.center = new Vector3(0f, 1f, 0f);

        return station;
    }

    private static GameObject CreateTimelineStation(Transform parent)
    {
        GameObject station = new GameObject("TimelineStation");
        station.transform.SetParent(parent);
        station.transform.position = new Vector3(0f, 0f, -6.5f);

        // Wall panel
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wall.name = "TimelineWall_Placeholder";
        wall.transform.SetParent(station.transform);
        wall.transform.localPosition = new Vector3(0f, 2f, 0f);
        wall.transform.localScale = new Vector3(4f, 1f, 0.1f);
        wall.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        SetStandardMaterial(wall, new Color(0.4f, 0.42f, 0.45f));

        // Add TimelineStation component
        var timelineScript = wall.AddComponent<TimelineStation>();

        // Trigger zone
        var triggerZone = station.AddComponent<BoxCollider>();
        triggerZone.size = new Vector3(4f, 2f, 1.5f);
        triggerZone.isTrigger = true;
        triggerZone.center = new Vector3(0f, 1f, 1f);

        return station;
    }

    private static GameObject CreateHelmetStation(Transform parent)
    {
        GameObject station = new GameObject("HelmetStation");
        station.transform.SetParent(parent);
        station.transform.position = new Vector3(4f, 0f, -3f);

        // Pedestal
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Pedestal";
        pedestal.transform.SetParent(station.transform);
        pedestal.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);
        pedestal.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        SetStandardMaterial(pedestal, new Color(0.3f, 0.3f, 0.32f));

        // Helmet placeholder (sphere)
        GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        helmet.name = "HelmetModel_Placeholder";
        helmet.transform.SetParent(station.transform);
        helmet.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
        helmet.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        SetStandardMaterial(helmet, new Color(0.72f, 0.55f, 0.2f));

        // Add HelmetStation component
        var helmetScript = helmet.AddComponent<HelmetStation>();

        // Trigger zone
        var triggerZone = station.AddComponent<BoxCollider>();
        triggerZone.size = new Vector3(2.5f, 2f, 2.5f);
        triggerZone.isTrigger = true;
        triggerZone.center = new Vector3(0f, 1f, 0f);

        return station;
    }

    private static void SetupStation(GameObject station, string name, int index,
        string title, string description)
    {
        var baseStation = station.GetComponentInChildren<StationBase>();
        if (baseStation != null)
        {
            var serialized = new SerializedObject(baseStation);
            serialized.FindProperty("stationIndex").intValue = index;
            serialized.FindProperty("stationTitle").stringValue = title;
            serialized.FindProperty("stationDescription").stringValue = description;
            serialized.FindProperty("displayName").stringValue = title;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedProperties();
        }
    }

    private static void CreateLighting(Transform parent)
    {
        GameObject lighting = new GameObject("Lighting");
        lighting.transform.SetParent(parent);

        // Main directional light
        GameObject mainLight = new GameObject("MainLight");
        mainLight.transform.SetParent(lighting.transform);
        mainLight.transform.position = new Vector3(0f, 5f, 0f);
        mainLight.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        var dirLight = mainLight.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 0.8f;
        dirLight.color = new Color(0.95f, 0.93f, 0.88f);

        // Fill light
        GameObject fillLight = new GameObject("FillLight");
        fillLight.transform.SetParent(lighting.transform);
        fillLight.transform.position = new Vector3(-3f, 3f, 2f);

        var pointLight = fillLight.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.intensity = 0.5f;
        pointLight.range = 8f;
        pointLight.color = new Color(0.9f, 0.85f, 0.8f);

        // Render settings
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f, 1f);
        RenderSettings.ambientIntensity = 1.2f;
    }

    private static GameObject CreatePlayerSetup(Transform parent)
    {
        GameObject player = new GameObject("--- Player ---");
        player.transform.SetParent(parent);
        player.transform.position = new Vector3(0f, 0f, 3f);

        // Main camera (fallback for non-VR testing)
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(player.transform);
        camObj.transform.position = new Vector3(0f, 1.7f, 0f);
        camObj.tag = "MainCamera";

        var camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
        camera.fieldOfView = 90f;

        // RaycastSelector (on camera for mouse fallback)
        var lineRenderer = camObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 2;

        var raySelector = camObj.AddComponent<RaycastSelector>();
        raySelector.enableMouseFallback = true;

        // CharacterController for movement (desktop testing)
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        return player;
    }

    private static void CreateManagers(Transform parent,
        GameObject shipStation, GameObject timelineStation, GameObject helmetStation)
    {
        GameObject managers = new GameObject("--- Managers ---");
        managers.transform.SetParent(parent);

        // GameManager
        var gameMgr = managers.AddComponent<GameManager>();
        var shipBase = shipStation.GetComponentInChildren<StationBase>();
        var timelineBase = timelineStation.GetComponentInChildren<StationBase>();
        var helmetBase = helmetStation.GetComponentInChildren<StationBase>();

        if (shipBase != null && timelineBase != null && helmetBase != null)
        {
            var serialized = new SerializedObject(gameMgr);
            var stationsArray = serialized.FindProperty("stations");
            stationsArray.arraySize = 3;
            stationsArray.GetArrayElementAtIndex(0).objectReferenceValue = shipBase;
            stationsArray.GetArrayElementAtIndex(1).objectReferenceValue = timelineBase;
            stationsArray.GetArrayElementAtIndex(2).objectReferenceValue = helmetBase;
            serialized.ApplyModifiedProperties();
        }

        // AudioManager
        managers.AddComponent<AudioManager>();

        // ExperienceFlow
        managers.AddComponent<ExperienceFlow>();
    }

    private static void CreateUICanvas(Transform parent)
    {
        GameObject uiRoot = new GameObject("--- UI ---");
        uiRoot.transform.SetParent(parent);

        // Canvas
        GameObject canvasObj = new GameObject("MainCanvas");
        canvasObj.transform.SetParent(uiRoot.transform);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // UIManager
        var uiMgr = canvasObj.AddComponent<UIManager>();
    }

    private static void SetStandardMaterial(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }
    }

    [MenuItem("Harbour City Museum/Clear All Player Prefs", false, 20)]
    public static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[MuseumSceneBuilder] Player prefs cleared.");
    }
}
