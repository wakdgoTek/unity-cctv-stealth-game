using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CctvDemoSceneBuilder
{
    private const string CctvHeadPivotName = "Head_Pivot";
    private const string LegacyCctvViewPivotName = "View_Pivot";
    private const string LegacyCctvYawPivotName = "Yaw_Pivot";
    private const float HouseWidth = 190f;
    private const float HouseDepth = 130f;
    private const float SecondFloorY = 3.25f;
    private const float RoomWallHeight = 3.05f;
    private const float ExteriorWallHeight = 6.8f;
    private const float BlueprintWallThickness = 0.8f;
    private const float BlueprintWallOverlap = 0.06f;
    private const float SecurityWallHeight = 6.2f;
    private const float WallTopTrimY = 6.42f;
    private const float CeilingBeamY = 6.75f;
    private const float CeilingLightY = 7f;
    private const float DoorHeaderY = 4.95f;
    private const float ServicePipeY = 5.05f;
    private const float WallSignY = 3.55f;
    private const bool ForceSafeImportedMaterials = true;

    [MenuItem("Tools/CCTV Starter/Create Stealth Mini Game")]
    public static void CreateStealthMiniGame()
    {
        CleanupGeneratedObjects();

        GameObject root = new GameObject("StealthMiniGame");

        GameObject floor = CreateCube("Estate_Grounds_Foundation", new Vector3(0f, -0.06f, 0f), new Vector3(HouseWidth, 0.12f, HouseDepth), "M_EstateGrass", new Color(0.18f, 0.28f, 0.12f));
        floor.transform.SetParent(root.transform);

        BuildSecurityComplex(root.transform);

        Vector3 startPosition = new Vector3(-82f, 1f, 56f);
        GameObject player = CreatePlayer(root.transform, startPosition);

        Text statusText;
        Text hintText;
        CreateGameUi(out statusText, out hintText);

        GameObject gameManagerObject = new GameObject("Stealth_Game_Manager");
        gameManagerObject.transform.SetParent(root.transform);
        StealthGameManager gameManager = gameManagerObject.AddComponent<StealthGameManager>();
        gameManager.Configure(player.transform, player.GetComponent<SimplePlayerController>(), statusText, hintText, startPosition);

        CreateBlueprintDefaultCctvs(root.transform, player.GetComponent<CctvDetectionTarget>(), gameManager);
        CreateGoal(root.transform, gameManager, new Vector3(1f, 0.08f, -21f), new Vector3(10f, 0.16f, 5f));

        AttachMainCameraToPlayer(player.transform);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/CCTV Starter/Delete All CCTVs")]
    public static void DeleteAllCctvs()
    {
        CctvDetector[] detectors = FindAllCctvDetectors();
        int deletedCount = 0;

        foreach (CctvDetector detector in detectors)
        {
            if (detector == null)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(detector.gameObject);
            deletedCount++;
        }

        Selection.activeGameObject = null;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Deleted {deletedCount} CCTV object(s). Use Tools > CCTV Starter > Create Placeable CCTV to add cameras where you want them.");
    }

    [MenuItem("Tools/CCTV Starter/Organize Existing CCTVs")]
    public static void OrganizeExistingCctvs()
    {
        CctvDetector[] detectors = FindAllCctvDetectors();

        foreach (CctvDetector detector in detectors)
        {
            if (detector == null)
            {
                continue;
            }

            Transform pivot = EnsureCctvHeadPivot(detector);
            CctvPatrol patrol = detector.GetComponent<CctvPatrol>();
            if (patrol == null)
            {
                patrol = detector.gameObject.AddComponent<CctvPatrol>();
            }

            patrol.SetHeadPivot(pivot);
            EditorUtility.SetDirty(detector);
            EditorUtility.SetDirty(patrol);
            EditorUtility.SetDirty(detector.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/CCTV Starter/Add Wall Box Colliders")]
    public static void AddWallBoxCollidersToScene()
    {
        int colliderCount = EnsureBlockingCollidersForArchitecture(null);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Added or refreshed {colliderCount} wall/building BoxCollider(s) for CCTV line-of-sight blocking.");
    }

    [MenuItem("Tools/CCTV Starter/Fix Pink Imported Materials")]
    public static void FixPinkImportedMaterials()
    {
        int rendererCount = ReplaceImportedSceneMaterials();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Replaced {rendererCount} imported asset renderer material(s) with Unity-safe CCTVStarter materials.");
    }

    [MenuItem("Tools/CCTV Starter/Create Placeable CCTV")]
    public static void CreatePlaceableCctv()
    {
#if UNITY_2023_1_OR_NEWER
        CctvDetectionTarget target = Object.FindFirstObjectByType<CctvDetectionTarget>();
        StealthGameManager gameManager = Object.FindFirstObjectByType<StealthGameManager>();
#else
        CctvDetectionTarget target = Object.FindObjectOfType<CctvDetectionTarget>();
        StealthGameManager gameManager = Object.FindObjectOfType<StealthGameManager>();
#endif

        Vector3 position = new Vector3(0f, 2.5f, 0f);
        Vector3 lookDirection = Vector3.forward;
        Transform parent = null;

        if (TryGetSelectedWallMountPose(out Vector3 wallPosition, out Vector3 wallForward, out Transform wallParent))
        {
            position = wallPosition;
            lookDirection = wallForward;
            parent = wallParent;
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.pivot + Vector3.up * 2f;
            lookDirection = Vector3.ProjectOnPlane(SceneView.lastActiveSceneView.camera.transform.forward, Vector3.up);
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                lookDirection = Vector3.forward;
            }
        }

        GameObject cctv = CreateCctv(parent, "Placeable_CCTV", position, lookDirection, 16f, 58f, 0.45f, 85f, 32f, target, gameManager);
        Selection.activeGameObject = cctv;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/CCTV Starter/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(16f, 0.1f, 16f);
        floor.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Floor", new Color(0.25f, 0.28f, 0.3f));

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall_Blocking_Line_Of_Sight";
        wall.transform.position = new Vector3(2.25f, 1f, -0.5f);
        wall.transform.localScale = new Vector3(0.35f, 2f, 4f);
        wall.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Wall", new Color(0.55f, 0.55f, 0.55f));

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 3.5f);
        Renderer playerRenderer = player.GetComponent<Renderer>();
        playerRenderer.sharedMaterial = CreateMaterial("M_Player", new Color(0.2f, 0.55f, 1f));
        playerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        Rigidbody playerBody = player.AddComponent<Rigidbody>();
        playerBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        player.AddComponent<CctvDetectionTarget>();
        player.AddComponent<SimplePlayerController>();
        DetectionAlertExample alert = player.AddComponent<DetectionAlertExample>();
        Text alertText = CreateDetectionUi();
        alert.Configure(alertText);

        GameObject cctvRoot = new GameObject("CCTV");
        cctvRoot.transform.position = new Vector3(0f, 2.5f, -5f);
        cctvRoot.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        Transform headPivot = CreateHeadPivot(cctvRoot.transform);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Camera_Body";
        body.transform.SetParent(headPivot, false);
        body.transform.localScale = new Vector3(0.7f, 0.35f, 0.55f);
        body.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.SetParent(headPivot, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.32f);
        lens.transform.localScale = Vector3.one * 0.22f;
        lens.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Lens", new Color(0.05f, 0.45f, 0.65f));
        Object.DestroyImmediate(lens.GetComponent<Collider>());

        GameObject origin = new GameObject("Detection_Origin");
        origin.transform.SetParent(headPivot, false);
        origin.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        origin.transform.localRotation = Quaternion.identity;

        Light spot = new GameObject("View_Light").AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 12f;
        spot.spotAngle = 65f;
        spot.intensity = 2.5f;
        spot.color = new Color(1f, 0.92f, 0.65f);
        spot.transform.SetParent(origin.transform, false);

        CctvDetector detector = cctvRoot.AddComponent<CctvDetector>();
        detector.Configure(player.GetComponent<CctvDetectionTarget>(), LayerMask.GetMask("Default"), origin.transform);
        cctvRoot.AddComponent<CctvViewVisualizer>();
        CctvPatrol patrol = cctvRoot.AddComponent<CctvPatrol>();
        patrol.SetHeadPivot(headPivot);

        UnityEventTools.AddPersistentListener(detector.onDetected, alert.SetDetected);
        UnityEventTools.AddPersistentListener(detector.onLost, alert.SetLost);

        AttachMainCameraToPlayer(player.transform);

        Selection.activeGameObject = cctvRoot;
        EditorUtility.SetDirty(detector);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void CleanupGeneratedObjects()
    {
        string[] names =
        {
            "StealthMiniGame",
            "Floor",
            "Wall_Blocking_Line_Of_Sight",
            "Player",
            "CCTV",
            "Detection_UI",
            "Stealth_Game_UI",
            "Stealth_Game_Manager"
        };

        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }
    }

    private static CctvDetector[] FindAllCctvDetectors()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<CctvDetector>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<CctvDetector>();
#endif
    }

    private static GameObject CreatePlayer(Transform parent, Vector3 position)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.SetParent(parent);
        player.transform.position = position;

        Renderer playerRenderer = player.GetComponent<Renderer>();
        playerRenderer.sharedMaterial = CreateMaterial("M_Player", new Color(0.2f, 0.55f, 1f));
        playerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        Rigidbody playerBody = player.AddComponent<Rigidbody>();
        playerBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        player.AddComponent<CctvDetectionTarget>();
        player.AddComponent<SimplePlayerController>();

        return player;
    }

    private static void BuildSecurityComplex(Transform parent)
    {
        CreateCleanEstateGrounds(parent);
        CreateCleanEstatePerimeter(parent);
        CreateCleanEstateRoutes(parent);
        CreateBlueprintFoundation(parent);
        CreateBlueprintRoomShells(parent);
        CreateBlueprintDoorAndSeamFillers(parent);
        CreateCleanMansionEnvelope(parent);
        CreateCleanExteriorZones(parent);
        CreateCleanEstateProps(parent);
        CreateBlueprintEquipment(parent);
        CreateBlueprintLighting(parent);
        EnsureBlockingCollidersForArchitecture(parent);
    }

    private static void CreateCleanEstateGrounds(Transform parent)
    {
        CreateDecorCube(parent, "Clean_Main_Lawn_North", new Vector3(0f, 0.006f, 47f), new Vector3(170f, 0.04f, 30f), "M_LawnDark", new Color(0.14f, 0.25f, 0.09f));
        CreateDecorCube(parent, "Clean_Main_Estate_Plaza", new Vector3(0f, 0.008f, -14f), new Vector3(120f, 0.04f, 86f), "M_Stone", new Color(0.52f, 0.5f, 0.44f));
        CreateDecorCube(parent, "Clean_Airfield_Apron", new Vector3(28f, 0.01f, -52f), new Vector3(115f, 0.05f, 22f), "M_Asphalt", new Color(0.18f, 0.18f, 0.17f));
        CreateDecorCube(parent, "Clean_Left_Service_Lawn", new Vector3(-74f, 0.008f, -12f), new Vector3(30f, 0.04f, 54f), "M_EstateGrass", new Color(0.18f, 0.28f, 0.12f));
        CreateDecorCube(parent, "Clean_Right_Service_Paving", new Vector3(72f, 0.012f, 4f), new Vector3(36f, 0.04f, 66f), "M_DarkFloor", new Color(0.16f, 0.17f, 0.18f));
    }

    private static void CreateCleanEstatePerimeter(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;

        CreateEstateWallX(parent, "Clean_Perimeter_North_Left", -halfWidth, -11f, halfDepth);
        CreateEstateWallX(parent, "Clean_Perimeter_North_Right", 11f, halfWidth, halfDepth);
        CreateEstateWallX(parent, "Clean_Perimeter_South", -halfWidth, halfWidth, -halfDepth);
        CreateEstateWallZ(parent, "Clean_Perimeter_West", -halfWidth, -halfDepth, halfDepth);
        CreateEstateWallZ(parent, "Clean_Perimeter_East", halfWidth, -halfDepth, halfDepth);

        CreateEstateCornerPost(parent, "Clean_Perimeter_NW_Post", new Vector3(-halfWidth, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Clean_Perimeter_NE_Post", new Vector3(halfWidth, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Clean_Perimeter_SW_Post", new Vector3(-halfWidth, 2.1f, -halfDepth));
        CreateEstateCornerPost(parent, "Clean_Perimeter_SE_Post", new Vector3(halfWidth, 2.1f, -halfDepth));
        CreateEstateCornerPost(parent, "Clean_Main_Gate_Left_Post", new Vector3(-11f, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Clean_Main_Gate_Right_Post", new Vector3(11f, 2.1f, halfDepth));

        CreateDecorCube(parent, "Clean_Main_Gate_Header", new Vector3(0f, 4.75f, halfDepth - 0.28f), new Vector3(23f, 0.55f, 1.15f), "M_StoneWall", new Color(0.64f, 0.62f, 0.56f));
        CreateDecorCube(parent, "Clean_Main_Gate_Left_Leaf", new Vector3(-5.3f, 1.38f, halfDepth - 0.72f), new Vector3(9.2f, 2.5f, 0.18f), "M_Door", new Color(0.2f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Clean_Main_Gate_Right_Leaf", new Vector3(5.3f, 1.38f, halfDepth - 0.72f), new Vector3(9.2f, 2.5f, 0.18f), "M_Door", new Color(0.2f, 0.12f, 0.07f));
    }

    private static void CreateCleanEstateRoutes(Transform parent)
    {
        CreateDecorCube(parent, "Clean_Entry_Driveway", new Vector3(0f, 0.045f, 49f), new Vector3(18f, 0.05f, 32f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Clean_Courtyard_Horizontal_Road", new Vector3(0f, 0.046f, 35f), new Vector3(78f, 0.05f, 9f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Clean_Courtyard_Vertical_Road", new Vector3(0f, 0.047f, 18f), new Vector3(14f, 0.05f, 35f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Clean_Right_Service_Road", new Vector3(60f, 0.048f, -10f), new Vector3(14f, 0.05f, 72f), "M_Asphalt", new Color(0.17f, 0.17f, 0.16f));
        CreateDecorCube(parent, "Clean_Airfield_Return_Road", new Vector3(19f, 0.049f, -61f), new Vector3(118f, 0.05f, 7f), "M_Asphalt", new Color(0.16f, 0.16f, 0.15f));

        CreateFountain(parent, new Vector3(0f, 0.1f, 35f), 2.5f);

        CreateDecorCube(parent, "Clean_Route_Arrow_Entry_Left", new Vector3(-39f, 0.095f, 55f), new Vector3(18f, 0.04f, 0.25f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
        CreateDecorCube(parent, "Clean_Route_Arrow_Entry_Right", new Vector3(29f, 0.095f, 55f), new Vector3(18f, 0.04f, 0.25f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
        CreateDecorCube(parent, "Clean_Route_Arrow_Airfield", new Vector3(31f, 0.095f, -48f), new Vector3(58f, 0.04f, 0.25f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
    }

    private static void CreateCleanMansionEnvelope(Transform parent)
    {
        Color wall = new Color(0.62f, 0.59f, 0.52f);
        Color roof = new Color(0.1f, 0.095f, 0.09f);
        Color trim = new Color(0.34f, 0.31f, 0.26f);

        CreateBlockingDecorCube(parent, "Clean_Mansion_Front_Facade", new Vector3(-3f, 1.72f, 27.6f), new Vector3(54f, 3.35f, 0.9f), "M_StoneWall", wall);
        CreateBlockingDecorCube(parent, "Clean_Mansion_Back_Facade", new Vector3(-3f, 1.72f, -8.3f), new Vector3(54f, 3.35f, 0.9f), "M_StoneWall", wall);
        CreateBlockingDecorCube(parent, "Clean_Mansion_Left_Facade", new Vector3(-30.4f, 1.72f, 9.6f), new Vector3(0.9f, 3.35f, 36.8f), "M_StoneWall", wall);
        CreateBlockingDecorCube(parent, "Clean_Mansion_Right_Facade", new Vector3(24.4f, 1.72f, 9.6f), new Vector3(0.9f, 3.35f, 36.8f), "M_StoneWall", wall);

        CreateBlockingDecorCube(parent, "Clean_Mansion_Second_Floor_Core", new Vector3(-3f, SecondFloorY + 1.45f, 9.5f), new Vector3(41f, 2.65f, 27f), "M_StoneWall", new Color(0.66f, 0.63f, 0.56f));
        CreateBlockingDecorCube(parent, "Clean_Mansion_Second_Floor_Roof", new Vector3(-3f, SecondFloorY + 3.03f, 9.5f), new Vector3(45f, 0.45f, 31f), "M_RoofDark", roof);
        CreateBlockingDecorCube(parent, "Clean_Mansion_Roof_Main_Slab", new Vector3(-3f, 3.58f, 9.5f), new Vector3(57f, 0.34f, 39f), "M_RoofDark", roof);

        CreateDecorCube(parent, "Clean_Mansion_Front_Plinth", new Vector3(-3f, 0.38f, 28.1f), new Vector3(55f, 0.34f, 0.3f), "M_Stone", new Color(0.48f, 0.46f, 0.4f));
        CreateDecorCube(parent, "Clean_Mansion_Front_First_Band", new Vector3(-3f, 3.34f, 28.12f), new Vector3(55f, 0.2f, 0.35f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Clean_Mansion_Upper_Band", new Vector3(-3f, SecondFloorY + 2.6f, 23.15f), new Vector3(42f, 0.22f, 0.35f), "M_WoodTrim", trim);

        CreateCleanMainDoorAssembly(parent, new Vector3(-3f, 0f, 28.18f));
        CreateStairs(parent, new Vector3(-3f, 0.12f, 21.2f), 5, 12f);

        for (float x = -24f; x <= 18f; x += 6f)
        {
            CreateCleanWindowAssembly(parent, $"Clean_Mansion_Front_Window_{x}", new Vector3(x, 1.95f, 28.18f), new Vector3(2.5f, 1.05f, 0.08f));
            CreateCleanWindowAssembly(parent, $"Clean_Mansion_Upper_Window_{x}", new Vector3(x, SecondFloorY + 1.45f, 23.18f), new Vector3(2.3f, 1f, 0.08f));
        }

        for (float x = -23f; x <= 17f; x += 5f)
        {
            CreateBlockingDecorCube(parent, $"Clean_Balcony_Post_{x}", new Vector3(x, SecondFloorY + 0.58f, 27.9f), new Vector3(0.34f, 1f, 0.34f), "M_StoneWall", wall);
        }

        CreateDecorCube(parent, "Clean_Balcony_Floor", new Vector3(-3f, SecondFloorY + 0.06f, 26.7f), new Vector3(42f, 0.14f, 3f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, "Clean_Balcony_Front_Rail", new Vector3(-3f, SecondFloorY + 0.98f, 28.15f), new Vector3(42f, 0.18f, 0.2f), "M_WoodTrim", trim);
    }

    private static void CreateCleanMainDoorAssembly(Transform parent, Vector3 basePosition)
    {
        Color door = new Color(0.19f, 0.105f, 0.055f);
        Color panel = new Color(0.27f, 0.15f, 0.075f);
        Color frame = new Color(0.16f, 0.11f, 0.07f);
        Color brass = new Color(0.95f, 0.68f, 0.22f);
        Color stone = new Color(0.52f, 0.49f, 0.43f);

        CreateDecorCube(parent, "Clean_Mansion_Door_Recess_Shadow", basePosition + new Vector3(0f, 1.58f, -0.035f), new Vector3(6.4f, 3.15f, 0.1f), "M_DarkMetal", new Color(0.045f, 0.04f, 0.035f));
        CreateDecorCube(parent, "Clean_Mansion_Door_Left_Leaf", basePosition + new Vector3(-1.22f, 1.46f, 0.035f), new Vector3(2.32f, 2.72f, 0.16f), "M_Door", door);
        CreateDecorCube(parent, "Clean_Mansion_Door_Right_Leaf", basePosition + new Vector3(1.22f, 1.46f, 0.035f), new Vector3(2.32f, 2.72f, 0.16f), "M_Door", door);

        CreateDecorCube(parent, "Clean_Mansion_Door_Center_Seam", basePosition + new Vector3(0f, 1.46f, 0.14f), new Vector3(0.08f, 2.75f, 0.08f), "M_DoorFrame", frame);
        CreateDecorCube(parent, "Clean_Mansion_Door_Left_Panel_Upper", basePosition + new Vector3(-1.22f, 1.95f, 0.16f), new Vector3(1.48f, 0.82f, 0.08f), "M_Door", panel);
        CreateDecorCube(parent, "Clean_Mansion_Door_Left_Panel_Lower", basePosition + new Vector3(-1.22f, 0.92f, 0.16f), new Vector3(1.48f, 0.82f, 0.08f), "M_Door", panel);
        CreateDecorCube(parent, "Clean_Mansion_Door_Right_Panel_Upper", basePosition + new Vector3(1.22f, 1.95f, 0.16f), new Vector3(1.48f, 0.82f, 0.08f), "M_Door", panel);
        CreateDecorCube(parent, "Clean_Mansion_Door_Right_Panel_Lower", basePosition + new Vector3(1.22f, 0.92f, 0.16f), new Vector3(1.48f, 0.82f, 0.08f), "M_Door", panel);

        CreateDecorCube(parent, "Clean_Mansion_Door_Frame_Left", basePosition + new Vector3(-2.78f, 1.58f, 0.09f), new Vector3(0.28f, 3.16f, 0.28f), "M_DoorFrame", frame);
        CreateDecorCube(parent, "Clean_Mansion_Door_Frame_Right", basePosition + new Vector3(2.78f, 1.58f, 0.09f), new Vector3(0.28f, 3.16f, 0.28f), "M_DoorFrame", frame);
        CreateDecorCube(parent, "Clean_Mansion_Door_Frame_Top", basePosition + new Vector3(0f, 3.16f, 0.09f), new Vector3(5.85f, 0.28f, 0.28f), "M_DoorFrame", frame);
        CreateDecorCube(parent, "Clean_Mansion_Door_Stone_Header", basePosition + new Vector3(0f, 3.55f, 0.02f), new Vector3(6.6f, 0.42f, 0.55f), "M_Stone", stone);
        CreateDecorCube(parent, "Clean_Mansion_Door_Threshold", basePosition + new Vector3(0f, 0.12f, 0.28f), new Vector3(6.9f, 0.18f, 0.65f), "M_Stone", stone);
        CreateDecorCube(parent, "Clean_Mansion_Door_Canopy", basePosition + new Vector3(0f, 3.95f, -0.12f), new Vector3(7.2f, 0.28f, 1.35f), "M_RoofDark", new Color(0.11f, 0.1f, 0.09f));

        CreateDecorCube(parent, "Clean_Mansion_Door_Left_Handle", basePosition + new Vector3(-0.22f, 1.48f, 0.24f), new Vector3(0.11f, 0.42f, 0.08f), "M_Lamp", brass);
        CreateDecorCube(parent, "Clean_Mansion_Door_Right_Handle", basePosition + new Vector3(0.22f, 1.48f, 0.24f), new Vector3(0.11f, 0.42f, 0.08f), "M_Lamp", brass);
    }

    private static void CreateCleanWindowAssembly(Transform parent, string name, Vector3 position, Vector3 glassScale)
    {
        Color glass = new Color(0.18f, 0.34f, 0.47f);
        Color frame = new Color(0.12f, 0.11f, 0.1f);
        float width = glassScale.x;
        float height = glassScale.y;
        float depth = glassScale.z;

        CreateDecorCube(parent, $"{name}_Glass", position, glassScale, "M_WindowGlass", glass);
        CreateDecorCube(parent, $"{name}_Frame_Top", position + new Vector3(0f, height * 0.5f + 0.12f, 0.035f), new Vector3(width + 0.34f, 0.12f, depth + 0.08f), "M_DoorFrame", frame);
        CreateDecorCube(parent, $"{name}_Frame_Bottom", position + new Vector3(0f, -height * 0.5f - 0.12f, 0.035f), new Vector3(width + 0.34f, 0.12f, depth + 0.08f), "M_DoorFrame", frame);
        CreateDecorCube(parent, $"{name}_Frame_Left", position + new Vector3(-width * 0.5f - 0.12f, 0f, 0.035f), new Vector3(0.12f, height + 0.34f, depth + 0.08f), "M_DoorFrame", frame);
        CreateDecorCube(parent, $"{name}_Frame_Right", position + new Vector3(width * 0.5f + 0.12f, 0f, 0.035f), new Vector3(0.12f, height + 0.34f, depth + 0.08f), "M_DoorFrame", frame);
        CreateDecorCube(parent, $"{name}_Center_Mullion", position + new Vector3(0f, 0f, 0.045f), new Vector3(0.09f, height + 0.18f, depth + 0.08f), "M_DoorFrame", frame);
    }

    private static void CreateCleanExteriorZones(Transform parent)
    {
        CreateBlockingDecorCube(parent, "Clean_Left_Pool_House_West_Wall", new Vector3(-86f, 1.65f, -16f), new Vector3(0.8f, 3.3f, 25f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Left_Pool_House_East_Wall", new Vector3(-59f, 1.65f, -16f), new Vector3(0.8f, 3.3f, 25f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Left_Pool_House_North_Wall", new Vector3(-72.5f, 1.65f, -3.5f), new Vector3(27f, 3.3f, 0.8f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Left_Pool_House_South_Wall", new Vector3(-72.5f, 1.65f, -28.5f), new Vector3(27f, 3.3f, 0.8f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Left_Pool_House_Roof", new Vector3(-72.5f, 3.55f, -16f), new Vector3(29f, 0.35f, 27f), "M_RoofDark", new Color(0.1f, 0.095f, 0.09f));
        CreatePool(parent, new Vector3(-72.5f, 0.08f, -17f), new Vector3(11f, 0.12f, 7f));

        CreateBlockingDecorCube(parent, "Clean_Right_Service_West_Wall", new Vector3(64f, 1.65f, 7f), new Vector3(0.8f, 3.3f, 58f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Right_Service_East_Wall", new Vector3(90f, 1.65f, 7f), new Vector3(0.8f, 3.3f, 58f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Right_Service_North_Wall", new Vector3(77f, 1.65f, 36f), new Vector3(26f, 3.3f, 0.8f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Right_Service_South_Wall", new Vector3(77f, 1.65f, -22f), new Vector3(26f, 3.3f, 0.8f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Clean_Right_Service_Second_Floor", new Vector3(77f, SecondFloorY + 1.35f, 7f), new Vector3(24f, 2.45f, 52f), "M_StoneWall", new Color(0.64f, 0.61f, 0.54f));
        CreateBlockingDecorCube(parent, "Clean_Right_Service_Roof", new Vector3(77f, SecondFloorY + 2.85f, 7f), new Vector3(28f, 0.4f, 61f), "M_RoofDark", new Color(0.1f, 0.095f, 0.09f));

        CreateDecorCube(parent, "Clean_Airplane_Body", new Vector3(-34f, 0.55f, -50f), new Vector3(14f, 0.9f, 2.1f), "M_ImportedMetal", new Color(0.36f, 0.38f, 0.39f));
        CreateDecorCube(parent, "Clean_Airplane_Left_Wing", new Vector3(-34f, 0.5f, -46f), new Vector3(6f, 0.18f, 6f), "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        CreateDecorCube(parent, "Clean_Airplane_Right_Wing", new Vector3(-34f, 0.5f, -54f), new Vector3(6f, 0.18f, 6f), "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        CreateDecorCube(parent, "Clean_Hangar_Block", new Vector3(72f, 2.1f, -48f), new Vector3(28f, 4.2f, 18f), "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        CreateDecorCube(parent, "Clean_Hangar_Door_Cutout_Dark", new Vector3(58f, 1.9f, -48f), new Vector3(0.16f, 3.2f, 10f), "M_DarkMetal", new Color(0.04f, 0.045f, 0.05f));
    }

    private static void CreateCleanEstateProps(Transform parent)
    {
        for (int i = 0; i < 14; i++)
        {
            float x = -80f + i * 12f;
            CreateTree(parent, new Vector3(x, 0.6f, 52f + (i % 2) * 4f));
        }

        for (int i = 0; i < 10; i++)
        {
            float z = -50f + i * 9f;
            CreateTree(parent, new Vector3(-88f, 0.6f, z));
            CreateTree(parent, new Vector3(88f, 0.6f, z + 2f));
        }

        CreateTennisCourt(parent, new Vector3(-74f, 0.07f, 38f));
        CreateParkingLot(parent, new Vector3(-72f, 0.07f, 14f));
        CreateFurnitureCube(parent, "Clean_Right_Service_Desk", new Vector3(74f, 0.42f, -8f), new Vector3(5.5f, 0.8f, 2.5f), "M_DarkWood", new Color(0.2f, 0.12f, 0.06f));
        CreateFurnitureCube(parent, "Clean_Right_Service_Sofa", new Vector3(78f, 0.42f, 18f), new Vector3(7f, 0.8f, 2.5f), "M_SofaFabric", new Color(0.27f, 0.31f, 0.34f));
        CreateFurnitureCube(parent, "Clean_Left_Pool_Lounge", new Vector3(-75f, 0.34f, -9f), new Vector3(5f, 0.55f, 1.6f), "M_SofaFabric", new Color(0.7f, 0.68f, 0.58f));

        for (int i = 0; i < 12; i++)
        {
            float x = -45f + i * 8f;
            CreateDecorCube(parent, $"Clean_Front_Garden_Planter_{i:00}", new Vector3(x, 0.18f, -30f), new Vector3(4.5f, 0.32f, 1.2f), "M_PlantLeaves", new Color(0.08f, 0.34f, 0.15f));
        }
    }

    private static void CreateEstateGrounds(Transform parent)
    {
        CreateDecorCube(parent, "Estate_Back_Lawn", new Vector3(0f, 0.006f, 45f), new Vector3(184f, 0.04f, 35f), "M_LawnDark", new Color(0.14f, 0.25f, 0.09f));
        CreateDecorCube(parent, "Estate_Front_Plaza_Base", new Vector3(0f, 0.01f, -42f), new Vector3(150f, 0.05f, 42f), "M_Asphalt", new Color(0.18f, 0.18f, 0.17f));
        CreateDecorCube(parent, "Mansion_Stone_Terrace", new Vector3(0f, 0.035f, -3f), new Vector3(70f, 0.07f, 55f), "M_Stone", new Color(0.55f, 0.52f, 0.45f));
        CreateDecorCube(parent, "Central_Garden_Base", new Vector3(-3f, 0.045f, -38f), new Vector3(44f, 0.08f, 22f), "M_GardenStone", new Color(0.48f, 0.46f, 0.38f));
    }

    private static void CreateEstatePerimeter(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;
        CreateEstateWallX(parent, "Estate_North_Wall_Left", -halfWidth, -10f, halfDepth);
        CreateEstateWallX(parent, "Estate_North_Wall_Right", 10f, halfWidth, halfDepth);
        CreateEstateWallX(parent, "Estate_South_Wall", -halfWidth, halfWidth, -halfDepth);
        CreateEstateWallZ(parent, "Estate_West_Wall", -halfWidth, -halfDepth, halfDepth);
        CreateEstateWallZ(parent, "Estate_East_Wall", halfWidth, -halfDepth, halfDepth);
        CreateEstateCornerPost(parent, "Estate_Corner_NW", new Vector3(-halfWidth, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Estate_Corner_NE", new Vector3(halfWidth, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Estate_Corner_SW", new Vector3(-halfWidth, 2.1f, -halfDepth));
        CreateEstateCornerPost(parent, "Estate_Corner_SE", new Vector3(halfWidth, 2.1f, -halfDepth));
        CreateEstateCornerPost(parent, "Estate_Gate_Left_Jamb", new Vector3(-10f, 2.1f, halfDepth));
        CreateEstateCornerPost(parent, "Estate_Gate_Right_Jamb", new Vector3(10f, 2.1f, halfDepth));

        CreateDecorCube(parent, "Main_Gate_Left_Pillar", new Vector3(-10f, 2.6f, halfDepth - 0.35f), new Vector3(1.8f, 5.2f, 1.8f), "M_StoneWall", new Color(0.58f, 0.56f, 0.5f));
        CreateDecorCube(parent, "Main_Gate_Right_Pillar", new Vector3(10f, 2.6f, halfDepth - 0.35f), new Vector3(1.8f, 5.2f, 1.8f), "M_StoneWall", new Color(0.58f, 0.56f, 0.5f));
        CreateDecorCube(parent, "Main_Gate_Arch", new Vector3(0f, 4.9f, halfDepth - 0.35f), new Vector3(21f, 0.8f, 1.3f), "M_StoneWall", new Color(0.58f, 0.56f, 0.5f));
        CreateDecorCube(parent, "Main_Gate_Left_Door", new Vector3(-4.7f, 1.45f, halfDepth - 0.65f), new Vector3(8.5f, 2.7f, 0.18f), "M_Door", new Color(0.22f, 0.13f, 0.07f));
        CreateDecorCube(parent, "Main_Gate_Right_Door", new Vector3(4.7f, 1.45f, halfDepth - 0.65f), new Vector3(8.5f, 2.7f, 0.18f), "M_Door", new Color(0.22f, 0.13f, 0.07f));
    }

    private static void CreateEstateRoadsAndCourtyards(Transform parent)
    {
        float halfDepth = HouseDepth * 0.5f;
        CreateDecorCube(parent, "North_Entry_Drive", new Vector3(0f, 0.04f, 49f), new Vector3(17f, 0.06f, 31f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Roundabout_Road_North", new Vector3(0f, 0.045f, 34f), new Vector3(44f, 0.06f, 13f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Roundabout_Road_South", new Vector3(0f, 0.045f, 19f), new Vector3(44f, 0.06f, 13f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Roundabout_Road_West", new Vector3(-21f, 0.045f, 26.5f), new Vector3(13f, 0.06f, 25f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));
        CreateDecorCube(parent, "Roundabout_Road_East", new Vector3(21f, 0.045f, 26.5f), new Vector3(13f, 0.06f, 25f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));

        CreateFountain(parent, new Vector3(0f, 0.1f, 26.5f), 3.2f);
        CreateFountain(parent, new Vector3(-3f, 0.1f, -37f), 2.5f);

        CreateDecorCube(parent, "Right_Service_Road", new Vector3(61f, 0.05f, -12f), new Vector3(16f, 0.08f, 74f), "M_Asphalt", new Color(0.17f, 0.17f, 0.16f));
        CreateDecorCube(parent, "Airfield_Taxiway", new Vector3(29f, 0.055f, -49f), new Vector3(99f, 0.08f, 23f), "M_Asphalt", new Color(0.17f, 0.17f, 0.16f));
        CreateDecorCube(parent, "Airfield_Return_Lane", new Vector3(21f, 0.06f, -60f), new Vector3(118f, 0.08f, 7f), "M_Asphalt", new Color(0.16f, 0.16f, 0.15f));
        CreateDecorCube(parent, "Entry_Road_To_Gate", new Vector3(0f, 0.045f, halfDepth - 9f), new Vector3(20f, 0.06f, 17f), "M_Asphalt", new Color(0.2f, 0.2f, 0.19f));

        CreateDecorCube(parent, "Route_Line_01", new Vector3(-42f, 0.09f, 55.5f), new Vector3(18f, 0.05f, 0.32f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
        CreateDecorCube(parent, "Route_Line_02", new Vector3(26f, 0.09f, 55.5f), new Vector3(22f, 0.05f, 0.32f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
        CreateDecorCube(parent, "Route_Line_05", new Vector3(47f, 0.09f, -38f), new Vector3(34f, 0.05f, 0.32f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
        CreateDecorCube(parent, "Route_Line_06", new Vector3(5f, 0.09f, -59f), new Vector3(82f, 0.05f, 0.32f), "M_StartStripe", new Color(0.05f, 0.25f, 1f));
    }

    private static void CreateEstateBuildings(Transform parent)
    {
        CreateMansionEnvelope(parent);
        CreateEstateSecondFloorDetails(parent);
        CreateLeftExteriorBuilding(parent);
        CreateRightExteriorBuilding(parent);
        CreatePoolHouse(parent);
    }

    private static void CreateMansionEnvelope(Transform parent)
    {
        CreateDecorCube(parent, "Mansion_Main_Facade", new Vector3(-3f, 1.65f, 24.9f), new Vector3(50f, 3.3f, 1.2f), "M_StoneWall", new Color(0.62f, 0.59f, 0.52f));
        CreateDecorCube(parent, "Mansion_Left_Wing_Facade", new Vector3(-34f, 1.65f, 4f), new Vector3(18f, 3.3f, 47f), "M_StoneWall", new Color(0.62f, 0.59f, 0.52f));
        CreateDecorCube(parent, "Mansion_Right_Wing_Facade", new Vector3(20f, 1.65f, 4f), new Vector3(18f, 3.3f, 47f), "M_StoneWall", new Color(0.62f, 0.59f, 0.52f));
        CreateDecorCube(parent, "Mansion_First_Floor_Ceiling_Slab", new Vector3(-3f, 3.38f, 7f), new Vector3(42f, 0.18f, 34f), "M_StoneWall", new Color(0.56f, 0.53f, 0.47f));
        CreateDecorCube(parent, "Mansion_Left_First_Floor_Ceiling_Slab", new Vector3(-32f, 3.34f, -4.5f), new Vector3(19f, 0.18f, 46f), "M_StoneWall", new Color(0.56f, 0.53f, 0.47f));
        CreateDecorCube(parent, "Mansion_Right_First_Floor_Ceiling_Slab", new Vector3(29f, 3.34f, 8f), new Vector3(24f, 0.18f, 31f), "M_StoneWall", new Color(0.56f, 0.53f, 0.47f));
        CreateMansionLayerBands(parent);
        CreateStairs(parent, new Vector3(-3f, 0.15f, 17f), 7, 17f);
        CreateDecorCube(parent, "Mansion_Entrance_Door", new Vector3(-3f, 1.45f, 22.75f), new Vector3(5.8f, 2.5f, 0.18f), "M_Door", new Color(0.24f, 0.14f, 0.08f));
    }

    private static void CreateMansionLayerBands(Transform parent)
    {
        Color plinth = new Color(0.48f, 0.46f, 0.4f);
        Color trim = new Color(0.34f, 0.31f, 0.26f);

        CreateDecorCube(parent, "Mansion_Front_Stone_Plinth", new Vector3(-3f, 0.34f, 25.62f), new Vector3(51.2f, 0.42f, 0.18f), "M_Stone", plinth);
        CreateDecorCube(parent, "Mansion_Front_First_Floor_Band", new Vector3(-3f, 3.35f, 25.64f), new Vector3(51.6f, 0.28f, 0.2f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Front_Second_Floor_Band", new Vector3(-3f, 6.18f, 22.66f), new Vector3(37.6f, 0.3f, 0.2f), "M_WoodTrim", trim);

        CreateDecorCube(parent, "Mansion_Left_Wing_Plinth", new Vector3(-43.12f, 0.34f, 4f), new Vector3(0.18f, 0.42f, 48f), "M_Stone", plinth);
        CreateDecorCube(parent, "Mansion_Left_Wing_First_Floor_Band", new Vector3(-43.14f, 3.35f, 4f), new Vector3(0.2f, 0.28f, 48.2f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Right_Wing_Plinth", new Vector3(29.12f, 0.34f, 4f), new Vector3(0.18f, 0.42f, 48f), "M_Stone", plinth);
        CreateDecorCube(parent, "Mansion_Right_Wing_First_Floor_Band", new Vector3(29.14f, 3.35f, 4f), new Vector3(0.2f, 0.28f, 48.2f), "M_WoodTrim", trim);
    }

    private static void CreateEstateSecondFloorDetails(Transform parent)
    {
        float upperY = SecondFloorY;
        Color stone = new Color(0.66f, 0.63f, 0.56f);
        Color trim = new Color(0.28f, 0.25f, 0.2f);
        Color glass = new Color(0.25f, 0.5f, 0.68f);

        CreateBlockingDecorCube(parent, "Mansion_Second_Floor_Core_Facade", new Vector3(-3f, upperY + 1.56f, 12f), new Vector3(33f, 2.62f, 19.4f), "M_StoneWall", stone);
        CreateBlockingDecorCube(parent, "Mansion_Second_Floor_Left_Tower", new Vector3(-30f, upperY + 1.5f, 8f), new Vector3(12.6f, 2.5f, 27.4f), "M_StoneWall", stone);
        CreateBlockingDecorCube(parent, "Mansion_Second_Floor_Right_Tower", new Vector3(25f, upperY + 1.5f, 8f), new Vector3(12.6f, 2.5f, 27.4f), "M_StoneWall", stone);

        CreateBlockingDecorCube(parent, "Mansion_Upper_Roof_Core", new Vector3(-3f, upperY + 3.08f, 12f), new Vector3(36f, 0.5f, 22f), "M_RoofDark", new Color(0.1f, 0.09f, 0.08f));
        CreateBlockingDecorCube(parent, "Mansion_Upper_Roof_Left", new Vector3(-30f, upperY + 2.98f, 8f), new Vector3(14.6f, 0.48f, 29f), "M_RoofDark", new Color(0.1f, 0.09f, 0.08f));
        CreateBlockingDecorCube(parent, "Mansion_Upper_Roof_Right", new Vector3(25f, upperY + 2.98f, 8f), new Vector3(14.6f, 0.48f, 29f), "M_RoofDark", new Color(0.1f, 0.09f, 0.08f));
        CreateBlockingDecorCube(parent, "Mansion_Attic_Center_Wall", new Vector3(-3f, upperY + 3.86f, 12f), new Vector3(16f, 0.9f, 7.6f), "M_StoneWall", new Color(0.61f, 0.58f, 0.51f));
        CreateBlockingDecorCube(parent, "Mansion_Attic_Roof_Cap", new Vector3(-3f, upperY + 4.46f, 12f), new Vector3(18f, 0.36f, 9f), "M_RoofDark", new Color(0.09f, 0.08f, 0.07f));

        CreateDecorCube(parent, "Mansion_Second_Floor_Balcony_Floor", new Vector3(-3f, upperY + 0.08f, 26.8f), new Vector3(39f, 0.18f, 4.2f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, "Mansion_Balcony_Front_Rail", new Vector3(-3f, upperY + 0.78f, 28.8f), new Vector3(40f, 0.18f, 0.22f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Balcony_Left_Rail", new Vector3(-23f, upperY + 0.78f, 26.8f), new Vector3(0.22f, 0.18f, 4f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Balcony_Right_Rail", new Vector3(17f, upperY + 0.78f, 26.8f), new Vector3(0.22f, 0.18f, 4f), "M_WoodTrim", trim);

        for (float x = -21f; x <= 15f; x += 6f)
        {
            CreateBlockingDecorCube(parent, $"Mansion_Balcony_Stone_Post_{x}", new Vector3(x, upperY + 0.65f, 28.8f), new Vector3(0.45f, 1.1f, 0.45f), "M_StoneWall", stone);
            CreateDecorCube(parent, $"Mansion_Upper_Front_Window_{x}", new Vector3(x, upperY + 1.58f, 22.05f), new Vector3(2.6f, 1.15f, 0.08f), "M_WindowGlass", glass);
            CreateDecorCube(parent, $"Mansion_Lower_Front_Window_{x}", new Vector3(x, 2.05f, 25.62f), new Vector3(2.6f, 1.15f, 0.08f), "M_WindowGlass", glass);
        }

        for (float z = -2f; z <= 18f; z += 6.5f)
        {
            CreateDecorCube(parent, $"Mansion_Left_Tower_Window_{z}", new Vector3(-23.45f, upperY + 1.5f, z), new Vector3(0.08f, 1.1f, 2.4f), "M_WindowGlass", glass);
            CreateDecorCube(parent, $"Mansion_Right_Tower_Window_{z}", new Vector3(18.45f, upperY + 1.5f, z), new Vector3(0.08f, 1.1f, 2.4f), "M_WindowGlass", glass);
        }

        CreateDecorCube(parent, "Mansion_Upper_Cornice_Front", new Vector3(-3f, upperY + 2.75f, 22.1f), new Vector3(39f, 0.28f, 0.5f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Upper_Cornice_Back", new Vector3(-3f, upperY + 2.75f, 1.9f), new Vector3(34f, 0.28f, 0.5f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Left_Tower_Cornice", new Vector3(-30f, upperY + 2.62f, -6.2f), new Vector3(15f, 0.28f, 0.5f), "M_WoodTrim", trim);
        CreateDecorCube(parent, "Mansion_Right_Tower_Cornice", new Vector3(25f, upperY + 2.62f, -6.2f), new Vector3(15f, 0.28f, 0.5f), "M_WoodTrim", trim);

        CreateBlockingDecorCube(parent, "Left_Exterior_Second_Floor_Block", new Vector3(-72f, upperY + 1.5f, -16f), new Vector3(20.4f, 2.45f, 19.4f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Left_Exterior_Second_Roof", new Vector3(-72f, upperY + 2.98f, -16f), new Vector3(23f, 0.42f, 22f), "M_RoofDark", new Color(0.1f, 0.09f, 0.08f));
        CreateBlockingDecorCube(parent, "Right_Exterior_Second_Floor_Block", new Vector3(77f, upperY + 1.55f, 7f), new Vector3(21.4f, 2.55f, 51.4f), "M_StoneWall", new Color(0.6f, 0.57f, 0.5f));
        CreateBlockingDecorCube(parent, "Right_Exterior_Second_Roof", new Vector3(77f, upperY + 3.08f, 7f), new Vector3(24f, 0.45f, 54f), "M_RoofDark", new Color(0.1f, 0.09f, 0.08f));

        for (float z = -12f; z <= 26f; z += 9.5f)
        {
            CreateDecorCube(parent, $"Right_Exterior_Upper_Window_{z}", new Vector3(64.45f, upperY + 1.55f, z), new Vector3(0.08f, 1.1f, 3f), "M_WindowGlass", glass);
        }

        for (float x = -80f; x <= -64f; x += 5.5f)
        {
            CreateDecorCube(parent, $"Left_Exterior_Upper_Window_{x}", new Vector3(x, upperY + 1.5f, -4.45f), new Vector3(2.4f, 1.1f, 0.08f), "M_WindowGlass", glass);
        }
    }

    private static void CreateLeftExteriorBuilding(Transform parent)
    {
        CreateDecorCube(parent, "Left_Exterior_Building_Floor", new Vector3(-72f, 0.04f, -16f), new Vector3(25f, 0.08f, 24f), "M_WoodFloor", new Color(0.42f, 0.33f, 0.24f));
        CreateEstateWallX(parent, "Left_Exterior_Building_North", -84f, -60f, -4f);
        CreateEstateWallX(parent, "Left_Exterior_Building_South", -84f, -60f, -28f);
        CreateEstateWallZ(parent, "Left_Exterior_Building_West", -84f, -28f, -4f);
        CreateEstateWallZ(parent, "Left_Exterior_Building_East", -60f, -28f, -4f);
        CreateEstateCornerPost(parent, "Left_Exterior_Corner_NW", new Vector3(-84f, 2.1f, -4f));
        CreateEstateCornerPost(parent, "Left_Exterior_Corner_NE", new Vector3(-60f, 2.1f, -4f));
        CreateEstateCornerPost(parent, "Left_Exterior_Corner_SW", new Vector3(-84f, 2.1f, -28f));
        CreateEstateCornerPost(parent, "Left_Exterior_Corner_SE", new Vector3(-60f, 2.1f, -28f));
        CreateDecorCube(parent, "Left_Exterior_Building_Roof", new Vector3(-72f, 3.55f, -16f), new Vector3(25f, 0.42f, 24f), "M_RoofDark", new Color(0.12f, 0.11f, 0.1f));
        CreatePool(parent, new Vector3(-72f, 0.09f, -18f), new Vector3(11f, 0.12f, 7f));
    }

    private static void CreateRightExteriorBuilding(Transform parent)
    {
        CreateDecorCube(parent, "Right_Long_Building_Floor", new Vector3(77f, 0.04f, 7f), new Vector3(26f, 0.08f, 60f), "M_WoodFloor", new Color(0.42f, 0.33f, 0.24f));
        CreateEstateWallX(parent, "Right_Long_Building_North", 64f, 90f, 37f);
        CreateEstateWallX(parent, "Right_Long_Building_South", 64f, 90f, -23f);
        CreateEstateWallZ(parent, "Right_Long_Building_West", 64f, -23f, 37f);
        CreateEstateWallZ(parent, "Right_Long_Building_East", 90f, -23f, 37f);
        CreateEstateCornerPost(parent, "Right_Long_Corner_NW", new Vector3(64f, 2.1f, 37f));
        CreateEstateCornerPost(parent, "Right_Long_Corner_NE", new Vector3(90f, 2.1f, 37f));
        CreateEstateCornerPost(parent, "Right_Long_Corner_SW", new Vector3(64f, 2.1f, -23f));
        CreateEstateCornerPost(parent, "Right_Long_Corner_SE", new Vector3(90f, 2.1f, -23f));
        CreateDecorCube(parent, "Right_Long_Building_Roof", new Vector3(77f, 3.65f, 7f), new Vector3(27f, 0.45f, 61f), "M_RoofDark", new Color(0.12f, 0.11f, 0.1f));
        CreateFurnitureCube(parent, "Right_Building_Lounge_Table", new Vector3(77f, 0.4f, 18f), new Vector3(6f, 0.8f, 3f), "M_DarkWood", new Color(0.2f, 0.12f, 0.06f));
        CreateFurnitureCube(parent, "Right_Building_Office_Table", new Vector3(77f, 0.4f, -10f), new Vector3(6f, 0.8f, 3f), "M_DarkWood", new Color(0.2f, 0.12f, 0.06f));
    }

    private static void CreatePoolHouse(Transform parent)
    {
        CreatePool(parent, new Vector3(32f, 0.08f, -18f), new Vector3(17f, 0.12f, 11f));
        CreateDecorCube(parent, "Pool_Stone_Deck", new Vector3(32f, 0.055f, -18f), new Vector3(25f, 0.06f, 18f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, "Pool_Cabana_Roof", new Vector3(44f, 2.4f, -18f), new Vector3(8f, 0.35f, 8f), "M_RoofDark", new Color(0.12f, 0.11f, 0.1f));
    }

    private static void CreateEstateLandscapeAndProps(Transform parent)
    {
        CreateTennisCourt(parent, new Vector3(-75f, 0.07f, 37f));
        CreateParkingLot(parent, new Vector3(-70f, 0.07f, 12f));
        CreateAirplane(parent, new Vector3(-34f, 0.55f, -50f), 12f);
        CreateHelicopter(parent, new Vector3(73f, 0.5f, -46f), 0f);
        CreateGardenBeds(parent);

        for (int i = 0; i < 15; i++)
        {
            float x = -84f + i * 12f;
            CreateTree(parent, new Vector3(x, 0.6f, 52f + (i % 2) * 5f));
        }

        for (int i = 0; i < 12; i++)
        {
            float z = -55f + i * 9f;
            CreateTree(parent, new Vector3(-88f, 0.6f, z));
            CreateTree(parent, new Vector3(91f, 0.6f, z + 2f));
        }
    }

    private static void CreateImportedAssetEnhancements(Transform parent)
    {
        GameObject root = new GameObject("Imported_Asset_Details");
        root.transform.SetParent(parent, false);

        CreateMansionPrefabDetails(root.transform);
        CreateHangarPrefabDetails(root.transform);

        if (root.transform.childCount == 0)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateMansionPrefabDetails(Transform parent)
    {
        const string prefabRoot = "Assets/Medieval Village Building Pack/Prefabs/";

        string windowPath = prefabRoot + "Window_5.prefab";
        string shutterPath = prefabRoot + "Window_5_Shutter_L.prefab";
        string doorPath = prefabRoot + "Door_4.prefab";
        string roofPath = prefabRoot + "Roof_Tile_Med_2x1.prefab";
        string railingPath = prefabRoot + "Railing_Tall.prefab";
        string pillarPath = prefabRoot + "Wood_Pillar_WBase_WTop_Thick_Tall.prefab";
        string stairPath = prefabRoot + "Stairs_Stone_2x1.prefab";
        string chimneyPath = prefabRoot + "Chimney_Stack.prefab";

        float[] frontWindowX = { -21f, -15f, -9f, -3f, 3f, 9f, 15f };
        for (int i = 0; i < frontWindowX.Length; i++)
        {
            Vector3 lower = new Vector3(frontWindowX[i], 1.35f, 25.95f);
            Vector3 upper = new Vector3(frontWindowX[i], SecondFloorY + 1.28f, 22.45f);
            InstantiateDetailPrefab(parent, windowPath, $"Asset_Lower_Window_{i:00}", lower, Vector3.zero, Vector3.one * 1.25f, false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f));
            InstantiateDetailPrefab(parent, windowPath, $"Asset_Upper_Window_{i:00}", upper, Vector3.zero, Vector3.one * 1.1f, false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f));
            InstantiateDetailPrefab(parent, shutterPath, $"Asset_Upper_Shutter_{i:00}", upper + new Vector3(0.82f, 0f, 0.02f), Vector3.zero, Vector3.one * 1.1f, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f));
        }

        InstantiateDetailPrefab(parent, doorPath, "Asset_Mansion_Main_Door", new Vector3(-3f, 0.15f, 26.08f), Vector3.zero, new Vector3(1.7f, 1.7f, 1.7f), false, "M_ImportedDoor", new Color(0.18f, 0.11f, 0.06f));
        InstantiateDetailPrefab(parent, stairPath, "Asset_Mansion_Front_Stone_Stairs", new Vector3(-3f, 0.1f, 18.8f), Vector3.zero, new Vector3(3.6f, 0.9f, 2.4f), true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f));

        for (float x = -21f; x <= 15f; x += 6f)
        {
            InstantiateDetailPrefab(parent, pillarPath, $"Asset_Balcony_Pillar_{x}", new Vector3(x, SecondFloorY + 0.08f, 28.85f), Vector3.zero, new Vector3(0.7f, 0.9f, 0.7f), true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f));
        }

        for (float x = -19f; x <= 13f; x += 4f)
        {
            InstantiateDetailPrefab(parent, railingPath, $"Asset_Balcony_Railing_{x}", new Vector3(x, SecondFloorY + 0.45f, 29.1f), Vector3.zero, new Vector3(1.25f, 0.9f, 0.65f), false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f));
        }

        for (int i = 0; i < 7; i++)
        {
            float x = -21f + i * 7f;
            InstantiateDetailPrefab(parent, roofPath, $"Asset_Main_Roof_Tile_Front_{i:00}", new Vector3(x, SecondFloorY + 3.45f, 23.3f), new Vector3(0f, 0f, 0f), new Vector3(1.8f, 1f, 1.35f), false, "M_ImportedRoof", new Color(0.13f, 0.13f, 0.14f));
            InstantiateDetailPrefab(parent, roofPath, $"Asset_Main_Roof_Tile_Back_{i:00}", new Vector3(x, SecondFloorY + 3.45f, 1.2f), new Vector3(0f, 180f, 0f), new Vector3(1.8f, 1f, 1.35f), false, "M_ImportedRoof", new Color(0.13f, 0.13f, 0.14f));
        }

        InstantiateDetailPrefab(parent, chimneyPath, "Asset_Mansion_Chimney_Left", new Vector3(-30f, SecondFloorY + 3.25f, -2f), Vector3.zero, new Vector3(1.1f, 1.2f, 1.1f), true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f));
        InstantiateDetailPrefab(parent, chimneyPath, "Asset_Mansion_Chimney_Right", new Vector3(24f, SecondFloorY + 3.25f, 2f), Vector3.zero, new Vector3(1.1f, 1.2f, 1.1f), true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f));
    }

    private static void CreateHangarPrefabDetails(Transform parent)
    {
        const string prefabRoot = "Assets/HQ Hangar Free/Prefabs/";

        InstantiateDetailPrefab(parent, prefabRoot + "Floor.prefab", "Asset_Hangar_Concrete_Floor", new Vector3(68f, 0.04f, -47f), Vector3.zero, new Vector3(5.2f, 1f, 5.2f), false, "M_ImportedConcrete", new Color(0.42f, 0.42f, 0.4f));
        InstantiateDetailPrefab(parent, prefabRoot + "Hangar_part_01.prefab", "Asset_Hangar_Back_Wall", new Vector3(84f, 0f, -47f), new Vector3(0f, -90f, 0f), new Vector3(1.6f, 1.55f, 1.6f), true, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        InstantiateDetailPrefab(parent, prefabRoot + "Hangar_part_02.prefab", "Asset_Hangar_Left_Wall", new Vector3(68f, 0f, -60f), Vector3.zero, new Vector3(1.6f, 1.55f, 1.6f), true, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        InstantiateDetailPrefab(parent, prefabRoot + "Hangar_part_03.prefab", "Asset_Hangar_Right_Wall", new Vector3(68f, 0f, -34f), new Vector3(0f, 180f, 0f), new Vector3(1.6f, 1.55f, 1.6f), true, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        InstantiateDetailPrefab(parent, prefabRoot + "Hangar_part_04.prefab", "Asset_Hangar_Door_Frame", new Vector3(51.5f, 0f, -47f), new Vector3(0f, 90f, 0f), new Vector3(1.6f, 1.55f, 1.6f), true, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f));
        InstantiateDetailPrefab(parent, prefabRoot + "Hangar_part_05.prefab", "Asset_Hangar_Roof", new Vector3(68f, 0f, -47f), Vector3.zero, new Vector3(1.6f, 1.55f, 1.6f), true, "M_ImportedRoof", new Color(0.13f, 0.13f, 0.14f));
    }

    private static void CreateHighQualityImportedEstate(Transform parent)
    {
        GameObject root = new GameObject("High_Quality_Imported_Estate");
        root.transform.SetParent(parent, false);

        CreateModularHouseExteriorLayer(root.transform);
        CreateHouseInteriorLayer(root.transform);
        CreateGardenAssetLayer(root.transform);
        CreateAirfieldAssetLayer(root.transform);
        CreateSecurityAndRouteAssetLayer(root.transform);

        if (root.transform.childCount == 0)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateModularHouseExteriorLayer(Transform parent)
    {
        const string houseRoot = "Assets/ModularHousePack1/Prefabs/Modules/";
        string door = houseRoot + "Doors/Door_1A.prefab";
        string windowA = houseRoot + "Window/Window_1A.prefab";
        string windowB = houseRoot + "Window/Window_1B.prefab";
        string curtain = houseRoot + "Curtains/Curtain_2.prefab";
        string roofA = houseRoot + "Roofs/RoofA.prefab";
        string roofFlat = houseRoot + "Roofs/RoofFlat.prefab";
        string corner = houseRoot + "Corners/Corner_4A.prefab";
        string fence = houseRoot + "Fences/Fence_4.prefab";
        string streetlight = houseRoot + "Roads/Streetlight.prefab";
        string pavement = houseRoot + "Roads/Parts/Pavement_3.prefab";
        string road = houseRoot + "Roads/Road_2.prefab";
        string stair = houseRoot + "Stairs/Stairs_1A.prefab";

        InstantiateDetailPrefab(parent, door, "HQ_Main_Entrance_Door_Module", new Vector3(-3f, 0.05f, 26.4f), Vector3.zero, new Vector3(2.2f, 2.2f, 2.2f), true, "M_ImportedDoor", new Color(0.18f, 0.11f, 0.06f), false);
        InstantiateDetailPrefab(parent, stair, "HQ_Main_Entrance_Stairs_Module", new Vector3(-3f, 0.05f, 18.2f), Vector3.zero, new Vector3(2.5f, 1.2f, 2.5f), true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);

        float[] mansionWindowX = { -24f, -18f, -12f, -6f, 0f, 6f, 12f, 18f };
        for (int i = 0; i < mansionWindowX.Length; i++)
        {
            string path = i % 2 == 0 ? windowA : windowB;
            InstantiateDetailPrefab(parent, path, $"HQ_Mansion_Window_Lower_{i:00}", new Vector3(mansionWindowX[i], 1.15f, 26.35f), Vector3.zero, new Vector3(1.45f, 1.45f, 1.45f), false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f), false);
            InstantiateDetailPrefab(parent, curtain, $"HQ_Mansion_Curtain_Lower_{i:00}", new Vector3(mansionWindowX[i], 1.2f, 26.42f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
            InstantiateDetailPrefab(parent, path, $"HQ_Mansion_Window_Upper_{i:00}", new Vector3(mansionWindowX[i], SecondFloorY + 1.15f, 22.65f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f), false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f), false);
        }

        for (float z = -2f; z <= 18f; z += 6f)
        {
            InstantiateDetailPrefab(parent, windowA, $"HQ_Left_Wing_Window_{z:00}", new Vector3(-43.5f, 1.25f, z), new Vector3(0f, 90f, 0f), new Vector3(1.25f, 1.25f, 1.25f), false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f), false);
            InstantiateDetailPrefab(parent, windowB, $"HQ_Right_Wing_Window_{z:00}", new Vector3(29.5f, 1.25f, z), new Vector3(0f, -90f, 0f), new Vector3(1.25f, 1.25f, 1.25f), false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f), false);
        }

        for (int i = 0; i < 8; i++)
        {
            float x = -29f + i * 8f;
            InstantiateDetailPrefab(parent, roofA, $"HQ_Mansion_Roof_Module_{i:00}", new Vector3(x, SecondFloorY + 3.62f, 12f), Vector3.zero, new Vector3(1.45f, 1.1f, 1.3f), true, "M_ImportedRoof", new Color(0.13f, 0.13f, 0.14f), false);
        }

        Vector3[] cornerPositions =
        {
            new Vector3(-29f, SecondFloorY + 3.35f, 23.5f),
            new Vector3(23f, SecondFloorY + 3.35f, 23.5f),
            new Vector3(-29f, SecondFloorY + 3.35f, 0.5f),
            new Vector3(23f, SecondFloorY + 3.35f, 0.5f),
            new Vector3(-84f, SecondFloorY + 2.9f, -4f),
            new Vector3(-60f, SecondFloorY + 2.9f, -28f),
            new Vector3(64f, SecondFloorY + 2.9f, 37f),
            new Vector3(90f, SecondFloorY + 2.9f, -23f)
        };

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            InstantiateDetailPrefab(parent, corner, $"HQ_Roof_Corner_Detail_{i:00}", cornerPositions[i], new Vector3(0f, i % 2 == 0 ? 0f : 180f, 0f), Vector3.one * 1.15f, true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        }

        for (int i = 0; i < 10; i++)
        {
            float x = -42f + i * 9f;
            InstantiateDetailPrefab(parent, fence, $"HQ_Front_Garden_Fence_{i:00}", new Vector3(x, 0.08f, -28f), Vector3.zero, Vector3.one * 1.55f, false, "M_ImportedTrim", new Color(0.55f, 0.48f, 0.38f), false);
            InstantiateDetailPrefab(parent, pavement, $"HQ_Promenade_Pavement_{i:00}", new Vector3(x, 0.1f, -34.5f), Vector3.zero, Vector3.one * 1.8f, false, "M_ImportedConcrete", new Color(0.42f, 0.42f, 0.4f), false);
        }

        for (int i = 0; i < 9; i++)
        {
            float z = -48f + i * 10f;
            InstantiateDetailPrefab(parent, streetlight, $"HQ_Service_Road_Light_{i:00}", new Vector3(51f, 0.05f, z), Vector3.zero, Vector3.one * 1.8f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
            InstantiateDetailPrefab(parent, road, $"HQ_Service_Road_Surface_{i:00}", new Vector3(61f, 0.09f, z), new Vector3(0f, 90f, 0f), new Vector3(2.1f, 1f, 2.1f), false, "M_ImportedConcrete", new Color(0.42f, 0.42f, 0.4f), false);
        }

        InstantiateDetailPrefab(parent, roofFlat, "HQ_Right_Building_Flat_Roof_Module", new Vector3(77f, SecondFloorY + 3.4f, 7f), Vector3.zero, new Vector3(3.6f, 1f, 7.3f), true, "M_ImportedRoof", new Color(0.13f, 0.13f, 0.14f), false);
    }

    private static void CreateHouseInteriorLayer(Transform parent)
    {
        const string interior = "Assets/nappin/HouseInteriorPack/Prefabs/";

        PlaceInteriorPrefab(parent, interior + "(Prb)CornerSofa.prefab", "HQ_Lounge_Corner_Sofa", new Vector3(-72f, 0.15f, -20f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)CoffeTable_smallPlant.prefab", "HQ_Lounge_Coffee_Table", new Vector3(-72f, 0.15f, -15f), Vector3.zero, Vector3.one * 1.3f);
        PlaceInteriorPrefab(parent, interior + "(Prb)MediaConsole.prefab", "HQ_Lounge_Media_Console", new Vector3(-62.5f, 0.15f, -14f), new Vector3(0f, -90f, 0f), Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Lamp.prefab", "HQ_Lounge_Floor_Lamp", new Vector3(-81f, 0.15f, -6.8f), Vector3.zero, Vector3.one * 1.3f);

        PlaceInteriorPrefab(parent, interior + "(Prb)Desk.prefab", "HQ_Security_Desk", new Vector3(74f, 0.12f, -8f), new Vector3(0f, 180f, 0f), Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)DeskLight.prefab", "HQ_Security_Desk_Light", new Vector3(74f, 0.92f, -8f), Vector3.zero, Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Shelf1.prefab", "HQ_Security_Shelf", new Vector3(88.2f, 0.12f, -16f), new Vector3(0f, -90f, 0f), Vector3.one * 1.4f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Storage1.prefab", "HQ_Security_Storage_A", new Vector3(66.5f, 0.12f, -17f), new Vector3(0f, 90f, 0f), Vector3.one * 1.35f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Storage2.prefab", "HQ_Security_Storage_B", new Vector3(66.5f, 0.12f, -11f), new Vector3(0f, 90f, 0f), Vector3.one * 1.35f);

        PlaceInteriorPrefab(parent, interior + "(Prb)KitchenIsland.prefab", "HQ_Kitchen_Island", new Vector3(17f, 0.14f, 7f), Vector3.zero, Vector3.one * 1.35f);
        PlaceInteriorPrefab(parent, interior + "(Prb)KitchenSink.prefab", "HQ_Kitchen_Sink", new Vector3(25.5f, 0.14f, 9f), new Vector3(0f, -90f, 0f), Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Fridge.prefab", "HQ_Kitchen_Fridge", new Vector3(25.5f, 0.14f, 16f), new Vector3(0f, -90f, 0f), Vector3.one * 1.15f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Stove.prefab", "HQ_Kitchen_Stove", new Vector3(25.5f, 0.14f, 2f), new Vector3(0f, -90f, 0f), Vector3.one * 1.18f);

        PlaceInteriorPrefab(parent, interior + "(Prb)DoubleBed.prefab", "HQ_Master_Bed", new Vector3(-32f, 0.12f, 6f), new Vector3(0f, 180f, 0f), Vector3.one * 1.25f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Wardrobe.prefab", "HQ_Master_Wardrobe", new Vector3(-42.1f, 0.12f, 17f), new Vector3(0f, 90f, 0f), Vector3.one * 1.3f);
        PlaceInteriorPrefab(parent, interior + "(Prb)BedsideTable.prefab", "HQ_Master_Bedside_Left", new Vector3(-36f, 0.12f, 3f), Vector3.zero, Vector3.one * 1.2f);
        PlaceInteriorPrefab(parent, interior + "(Prb)BedsideTable.prefab", "HQ_Master_Bedside_Right", new Vector3(-28f, 0.12f, 3f), Vector3.zero, Vector3.one * 1.2f);

        PlaceInteriorPrefab(parent, interior + "(Prb)BathroomSink.prefab", "HQ_Bathroom_Sink", new Vector3(-18f, 0.12f, 0f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f);
        PlaceInteriorPrefab(parent, interior + "(Prb)Toilet.prefab", "HQ_Bathroom_Toilet", new Vector3(-13f, 0.12f, 0f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f);
        PlaceInteriorPrefab(parent, interior + "(Prb)ShowerFace.prefab", "HQ_Bathroom_Shower", new Vector3(-8f, 0.12f, 0f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f);

        for (int i = 0; i < 8; i++)
        {
            float x = -24f + i * 7f;
            PlaceInteriorPrefab(parent, interior + "(Prb)CeilingLight.prefab", $"HQ_Mansion_Ceiling_Light_{i:00}", new Vector3(x, 2.85f, 8f), Vector3.zero, Vector3.one * 1.2f);
            PlaceInteriorPrefab(parent, interior + "(Prb)RoomLight.prefab", $"HQ_Second_Floor_Room_Light_{i:00}", new Vector3(x, SecondFloorY + 2.45f, 10f), Vector3.zero, Vector3.one * 1.1f);
        }
    }

    private static void PlaceInteriorPrefab(Transform parent, string path, string name, Vector3 position, Vector3 eulerAngles, Vector3 scale)
    {
        InstantiateDetailPrefab(parent, path, name, position, eulerAngles, scale, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
    }

    private static void CreateGardenAssetLayer(Transform parent)
    {
        const string garden = "Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/";
        const string fountain = "Assets/GVOZDY/Round Four-Tier Water Fountain/Prefabs/";

        InstantiateDetailPrefab(parent, fountain + "fountain_4_light.prefab", "HQ_Main_Courtyard_Fountain", new Vector3(0f, 0.12f, 26.5f), Vector3.zero, Vector3.one * 2.6f, false, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        InstantiateDetailPrefab(parent, fountain + "fountain_4_dark.prefab", "HQ_Garden_Fountain", new Vector3(-3f, 0.12f, -37f), Vector3.zero, Vector3.one * 1.7f, false, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);

        string[] flowerPaths =
        {
            garden + "Hydrangea.prefab",
            garden + "TulipRed.prefab",
            garden + "TulipYellow.prefab",
            garden + "Mint.prefab",
            garden + "GrassTall.prefab",
            garden + "GrassSmall.prefab"
        };

        int index = 0;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 10; col++)
            {
                float x = -45f + col * 9.5f;
                float z = -31f + row * 5.5f;
                string path = flowerPaths[(row + col) % flowerPaths.Length];
                InstantiateDetailPrefab(parent, path, $"HQ_Formal_Garden_Plant_{index:00}", new Vector3(x, 0.08f, z), Vector3.zero, Vector3.one * (0.9f + (col % 3) * 0.18f), false, "M_PlantLeaves", new Color(0.08f, 0.34f, 0.15f), false);
                index++;
            }
        }

        for (int i = 0; i < 16; i++)
        {
            float x = -82f + i * 10f;
            InstantiateDetailPrefab(parent, garden + "PotRectangle.prefab", $"HQ_North_Planter_{i:00}", new Vector3(x, 0.08f, 44f), Vector3.zero, Vector3.one * 1.5f, false, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        }

        for (int i = 0; i < 16; i++)
        {
            float z = -56f + i * 7f;
            InstantiateDetailPrefab(parent, garden + "Fence.prefab", $"HQ_West_Garden_Fence_{i:00}", new Vector3(-89f, 0.06f, z), new Vector3(0f, 90f, 0f), Vector3.one * 1.55f, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
            InstantiateDetailPrefab(parent, garden + "Fence.prefab", $"HQ_East_Garden_Fence_{i:00}", new Vector3(89f, 0.06f, z), new Vector3(0f, 90f, 0f), Vector3.one * 1.55f, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
        }

        InstantiateDetailPrefab(parent, garden + "WateringCup.prefab", "HQ_Garden_Watering_Cup", new Vector3(-25f, 0.1f, -28f), new Vector3(0f, 35f, 0f), Vector3.one * 1.4f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
        InstantiateDetailPrefab(parent, garden + "Rake.prefab", "HQ_Garden_Rake", new Vector3(-19f, 0.1f, -27f), new Vector3(0f, 115f, 0f), Vector3.one * 1.25f, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
        InstantiateDetailPrefab(parent, garden + "Shovel.prefab", "HQ_Garden_Shovel", new Vector3(-16f, 0.1f, -29f), new Vector3(0f, -25f, 0f), Vector3.one * 1.25f, false, "M_ImportedWood", new Color(0.22f, 0.16f, 0.1f), false);
    }

    private static void CreateAirfieldAssetLayer(Transform parent)
    {
        const string aircraft = "Assets/Generic Aircraft Models/Prefabs/";

        InstantiateDetailPrefab(parent, aircraft + "Structures/runway-a.prefab", "HQ_Airfield_Runway_Model", new Vector3(10f, 0.12f, -58f), new Vector3(0f, 90f, 0f), new Vector3(4.8f, 1f, 4.8f), false, "M_ImportedConcrete", new Color(0.42f, 0.42f, 0.4f), false);
        InstantiateDetailPrefab(parent, aircraft + "Aircrafts/aircraft-k.prefab", "HQ_Private_Jet_Model", new Vector3(-35f, 0.2f, -50f), new Vector3(0f, 82f, 0f), Vector3.one * 3.1f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
        InstantiateDetailPrefab(parent, aircraft + "Aircrafts/aircraft-i.prefab", "HQ_Helicopter_Model", new Vector3(73f, 0.2f, -46f), new Vector3(0f, -15f, 0f), Vector3.one * 2.2f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
        InstantiateDetailPrefab(parent, aircraft + "Structures/hangar-a.prefab", "HQ_Airfield_Hangar_Model", new Vector3(68f, 0.1f, -47f), new Vector3(0f, 90f, 0f), Vector3.one * 2.9f, true, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);

        for (int i = 0; i < 7; i++)
        {
            InstantiateDetailPrefab(parent, aircraft + "Parts/aircraft-wheel.prefab", $"HQ_Airfield_Wheel_Chock_{i:00}", new Vector3(-12f + i * 5f, 0.1f, -42f), new Vector3(0f, i * 23f, 0f), Vector3.one * 1.4f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
        }
    }

    private static void CreateSecurityAndRouteAssetLayer(Transform parent)
    {
        const string modular = "Assets/Barking_Dog/3D Free Modular Kit/Prefabs/";
        string arch = modular + "Door_Arch_01.prefab";
        string column = modular + "Column_01_Top.prefab";
        string wall = modular + "Wall_Simple_01.prefab";
        string fan = modular + "Fan_01.prefab";
        string light = modular + "Light_01.prefab";

        InstantiateDetailPrefab(parent, arch, "HQ_Security_Gate_Arch_Model", new Vector3(0f, 0.1f, 64.3f), Vector3.zero, Vector3.one * 3.1f, true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        InstantiateDetailPrefab(parent, column, "HQ_Security_Gate_Left_Cap_Model", new Vector3(-10f, 4.9f, 64.4f), Vector3.zero, Vector3.one * 2.3f, true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        InstantiateDetailPrefab(parent, column, "HQ_Security_Gate_Right_Cap_Model", new Vector3(10f, 4.9f, 64.4f), Vector3.zero, Vector3.one * 2.3f, true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);

        for (int i = 0; i < 12; i++)
        {
            InstantiateDetailPrefab(parent, wall, $"HQ_Service_Barrier_Wall_{i:00}", new Vector3(55f + i * 3f, 0.08f, -29f), new Vector3(0f, 90f, 0f), Vector3.one * 1.2f, true, "M_ImportedStone", new Color(0.47f, 0.44f, 0.38f), false);
        }

        for (int i = 0; i < 6; i++)
        {
            InstantiateDetailPrefab(parent, fan, $"HQ_Control_Room_Ceiling_Fan_{i:00}", new Vector3(69f + i * 3f, 2.9f, -8f + (i % 2) * 9f), Vector3.zero, Vector3.one * 1.45f, false, "M_ImportedMetal", new Color(0.33f, 0.35f, 0.36f), false);
            InstantiateDetailPrefab(parent, light, $"HQ_Control_Room_Modular_Light_{i:00}", new Vector3(69f + i * 3f, 2.75f, -13f + (i % 2) * 12f), Vector3.zero, Vector3.one * 1.3f, false, "M_Lamp", new Color(1f, 0.83f, 0.48f), false);
        }
    }

    private static void CreateEstateWallX(Transform parent, string name, float xMin, float xMax, float z)
    {
        float centerX = (xMin + xMax) * 0.5f;
        float length = Mathf.Abs(xMax - xMin) + 0.35f;
        GameObject wall = CreateCube(name, new Vector3(centerX, 2f, z), new Vector3(length, 4f, 1f), "M_StoneWall", new Color(0.58f, 0.56f, 0.5f));
        wall.transform.SetParent(parent);
        CreateDecorCube(parent, $"{name}_Top_Cap", new Vector3(centerX, 4.1f, z), new Vector3(length + 0.4f, 0.25f, 1.35f), "M_StoneWall", new Color(0.64f, 0.62f, 0.56f));
    }

    private static void CreateEstateWallZ(Transform parent, string name, float x, float zMin, float zMax)
    {
        float centerZ = (zMin + zMax) * 0.5f;
        float length = Mathf.Abs(zMax - zMin) + 0.35f;
        GameObject wall = CreateCube(name, new Vector3(x, 2f, centerZ), new Vector3(1f, 4f, length), "M_StoneWall", new Color(0.58f, 0.56f, 0.5f));
        wall.transform.SetParent(parent);
        CreateDecorCube(parent, $"{name}_Top_Cap", new Vector3(x, 4.1f, centerZ), new Vector3(1.35f, 0.25f, length + 0.4f), "M_StoneWall", new Color(0.64f, 0.62f, 0.56f));
    }

    private static void CreateEstateCornerPost(Transform parent, string name, Vector3 position)
    {
        CreateBlockingDecorCube(parent, name, position, new Vector3(1.45f, 4.2f, 1.45f), "M_StoneWall", new Color(0.64f, 0.62f, 0.56f));
        CreateDecorCube(parent, $"{name}_Cap", position + Vector3.up * 2.25f, new Vector3(1.75f, 0.28f, 1.75f), "M_StoneWall", new Color(0.7f, 0.68f, 0.62f));
    }

    private static void CreateFountain(Transform parent, Vector3 position, float radius)
    {
        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = $"Fountain_Base_{position.x}_{position.z}";
        baseObject.transform.SetParent(parent);
        baseObject.transform.position = position;
        baseObject.transform.localScale = new Vector3(radius, 0.2f, radius);
        baseObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Stone", new Color(0.5f, 0.48f, 0.42f));
        Object.DestroyImmediate(baseObject.GetComponent<Collider>());

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = $"Fountain_Water_{position.x}_{position.z}";
        water.transform.SetParent(parent);
        water.transform.position = position + Vector3.up * 0.22f;
        water.transform.localScale = new Vector3(radius * 0.72f, 0.06f, radius * 0.72f);
        water.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_PoolWater", new Color(0.08f, 0.38f, 0.62f));
        Object.DestroyImmediate(water.GetComponent<Collider>());

        CreateDecorCube(parent, $"Fountain_Jet_{position.x}_{position.z}", position + Vector3.up * 0.95f, new Vector3(0.25f, 1.3f, 0.25f), "M_WindowGlass", new Color(0.45f, 0.75f, 0.9f));
    }

    private static void CreateStairs(Transform parent, Vector3 basePosition, int steps, float width)
    {
        for (int i = 0; i < steps; i++)
        {
            Vector3 position = basePosition + new Vector3(0f, i * 0.08f, -i * 0.55f);
            Vector3 scale = new Vector3(width, 0.16f, 0.65f);
            GameObject step = CreateCube($"Facade_Stair_{i:00}", position, scale, "M_Stone", new Color(0.5f, 0.48f, 0.42f));
            step.transform.SetParent(parent);
        }
    }

    private static void CreatePool(Transform parent, Vector3 position, Vector3 scale)
    {
        CreateDecorCube(parent, $"Pool_Water_{position.x}_{position.z}", position + Vector3.up * 0.08f, scale, "M_PoolWater", new Color(0.08f, 0.38f, 0.62f));
        CreateDecorCube(parent, $"Pool_Edge_N_{position.x}_{position.z}", position + new Vector3(0f, 0.16f, scale.z * 0.5f + 0.55f), new Vector3(scale.x + 1.2f, 0.2f, 0.45f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, $"Pool_Edge_S_{position.x}_{position.z}", position + new Vector3(0f, 0.16f, -scale.z * 0.5f - 0.55f), new Vector3(scale.x + 1.2f, 0.2f, 0.45f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, $"Pool_Edge_W_{position.x}_{position.z}", position + new Vector3(-scale.x * 0.5f - 0.55f, 0.16f, 0f), new Vector3(0.45f, 0.2f, scale.z + 1.2f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        CreateDecorCube(parent, $"Pool_Edge_E_{position.x}_{position.z}", position + new Vector3(scale.x * 0.5f + 0.55f, 0.16f, 0f), new Vector3(0.45f, 0.2f, scale.z + 1.2f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
    }

    private static void CreateTennisCourt(Transform parent, Vector3 position)
    {
        CreateDecorCube(parent, "Tennis_Court_Surface", position, new Vector3(22f, 0.06f, 13f), "M_TennisCourt", new Color(0.13f, 0.32f, 0.32f));
        CreateDecorCube(parent, "Tennis_Court_Line_Center", position + Vector3.up * 0.05f, new Vector3(0.2f, 0.035f, 13.1f), "M_CourtLine", Color.white);
        CreateDecorCube(parent, "Tennis_Court_Line_N", position + new Vector3(0f, 0.05f, 6.2f), new Vector3(21f, 0.035f, 0.16f), "M_CourtLine", Color.white);
        CreateDecorCube(parent, "Tennis_Court_Line_S", position + new Vector3(0f, 0.05f, -6.2f), new Vector3(21f, 0.035f, 0.16f), "M_CourtLine", Color.white);
        CreateDecorCube(parent, "Tennis_Net", position + new Vector3(0f, 0.55f, 0f), new Vector3(21f, 0.8f, 0.08f), "M_DarkMetal", new Color(0.04f, 0.045f, 0.05f));
    }

    private static void CreateParkingLot(Transform parent, Vector3 position)
    {
        CreateDecorCube(parent, "Parking_Lot_Surface", position, new Vector3(24f, 0.06f, 19f), "M_Asphalt", new Color(0.17f, 0.17f, 0.16f));
        for (int i = 0; i < 6; i++)
        {
            float x = position.x - 8f + i * 3.2f;
            CreateDecorCube(parent, $"Parking_Line_{i}", new Vector3(x, 0.11f, position.z), new Vector3(0.12f, 0.035f, 17f), "M_CourtLine", Color.white);
            CreateCar(parent, new Vector3(x + 1.4f, 0.35f, position.z + (i % 2 == 0 ? 4f : -4f)), i);
        }
    }

    private static void CreateCar(Transform parent, Vector3 position, int index)
    {
        Color bodyColor = index % 3 == 0 ? new Color(0.1f, 0.12f, 0.14f) : index % 3 == 1 ? new Color(0.55f, 0.55f, 0.52f) : new Color(0.35f, 0.08f, 0.06f);
        CreateDecorCube(parent, $"Car_{index}_Body", position, new Vector3(2.4f, 0.55f, 4.2f), $"M_Car_{index}", bodyColor);
        CreateDecorCube(parent, $"Car_{index}_Cabin", position + Vector3.up * 0.48f, new Vector3(1.7f, 0.45f, 2f), "M_WindowGlass", new Color(0.12f, 0.25f, 0.32f));
    }

    private static void CreateAirplane(Transform parent, Vector3 position, float yaw)
    {
        GameObject plane = new GameObject("Estate_Private_Jet");
        plane.transform.SetParent(parent);
        plane.transform.position = position;
        plane.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateDecorCube(plane.transform, "Jet_Fuselage", Vector3.zero, new Vector3(3f, 1f, 15f), "M_Appliance", new Color(0.82f, 0.82f, 0.78f));
        CreateDecorCube(plane.transform, "Jet_Left_Wing", new Vector3(-4.2f, -0.05f, -0.4f), new Vector3(8f, 0.18f, 2.1f), "M_Appliance", new Color(0.78f, 0.78f, 0.74f));
        CreateDecorCube(plane.transform, "Jet_Right_Wing", new Vector3(4.2f, -0.05f, -0.4f), new Vector3(8f, 0.18f, 2.1f), "M_Appliance", new Color(0.78f, 0.78f, 0.74f));
        CreateDecorCube(plane.transform, "Jet_Tail", new Vector3(0f, 1f, -6.4f), new Vector3(0.35f, 2.2f, 1.8f), "M_Appliance", new Color(0.18f, 0.18f, 0.18f));
        CreateDecorCube(plane.transform, "Jet_Nose_Glass", new Vector3(0f, 0.25f, 7.8f), new Vector3(2.2f, 0.55f, 0.5f), "M_WindowGlass", new Color(0.12f, 0.25f, 0.32f));
    }

    private static void CreateHelicopter(Transform parent, Vector3 position, float yaw)
    {
        GameObject helicopter = new GameObject("Estate_Helicopter");
        helicopter.transform.SetParent(parent);
        helicopter.transform.position = position;
        helicopter.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateDecorCube(parent, "Helipad", new Vector3(position.x, 0.06f, position.z), new Vector3(17f, 0.08f, 17f), "M_Asphalt", new Color(0.16f, 0.16f, 0.15f));
        CreateDecorCube(helicopter.transform, "Heli_Body", Vector3.zero, new Vector3(3.2f, 1f, 6f), "M_DarkMetal", new Color(0.05f, 0.055f, 0.06f));
        CreateDecorCube(helicopter.transform, "Heli_Tail", new Vector3(0f, 0.15f, -5.2f), new Vector3(0.55f, 0.45f, 5.5f), "M_DarkMetal", new Color(0.05f, 0.055f, 0.06f));
        CreateDecorCube(helicopter.transform, "Heli_Main_Rotor_A", new Vector3(0f, 1.15f, 0f), new Vector3(10f, 0.08f, 0.25f), "M_DarkMetal", new Color(0.02f, 0.025f, 0.03f));
        CreateDecorCube(helicopter.transform, "Heli_Main_Rotor_B", new Vector3(0f, 1.17f, 0f), new Vector3(0.25f, 0.08f, 10f), "M_DarkMetal", new Color(0.02f, 0.025f, 0.03f));
    }

    private static void CreateGardenBeds(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float x = -18f + i * 12f;
            CreateDecorCube(parent, $"Garden_Bed_{i}", new Vector3(x, 0.08f, -36f), new Vector3(8f, 0.12f, 4f), "M_LawnDark", new Color(0.11f, 0.22f, 0.08f));
            CreateDecorCube(parent, $"Garden_Path_{i}", new Vector3(x, 0.11f, -31f), new Vector3(6f, 0.08f, 0.65f), "M_Stone", new Color(0.5f, 0.48f, 0.42f));
        }
    }

    private static void CreateTree(Transform parent, Vector3 position)
    {
        CreateDecorCube(parent, $"Tree_Trunk_{position.x}_{position.z}", position + Vector3.up * 0.55f, new Vector3(0.45f, 1.1f, 0.45f), "M_DarkWood", new Color(0.18f, 0.1f, 0.05f));
        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = $"Tree_Canopy_{position.x}_{position.z}";
        leaves.transform.SetParent(parent);
        leaves.transform.position = position + Vector3.up * 1.75f;
        leaves.transform.localScale = new Vector3(2.8f, 2.2f, 2.8f);
        leaves.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_PlantLeaves", new Color(0.08f, 0.28f, 0.1f));
        Object.DestroyImmediate(leaves.GetComponent<Collider>());
    }

    private static void CreateBlueprintFoundation(Transform parent)
    {
        CreateDecorCube(parent, "Start_Safe_Zone", new Vector3(-34f, 0.02f, 27f), new Vector3(13f, 0.04f, 8f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateDecorCube(parent, "Top_Main_Path", new Vector3(-10.5f, 0.016f, 25.25f), new Vector3(51f, 0.04f, 6.4f), "M_WoodFloor", new Color(0.38f, 0.31f, 0.23f));
        CreateDecorCube(parent, "Main_Room_Block_Floor", new Vector3(-3f, 0.018f, 7.25f), new Vector3(35.4f, 0.04f, 31.4f), "M_WoodFloor", new Color(0.42f, 0.33f, 0.23f));
        CreateDecorCube(parent, "Left_CCTV_Block_Floor", new Vector3(-32.5f, 0.018f, -4.5f), new Vector3(18.6f, 0.04f, 45.4f), "M_DarkFloor", new Color(0.12f, 0.13f, 0.14f));
        CreateDecorCube(parent, "Right_CCTV_Block_Floor", new Vector3(29f, 0.018f, 9f), new Vector3(23.4f, 0.04f, 30.4f), "M_DarkFloor", new Color(0.12f, 0.13f, 0.14f));
        CreateDecorCube(parent, "Fire_Door_Connector_Floor", new Vector3(14.5f, 0.02f, 3f), new Vector3(8.8f, 0.04f, 8f), "M_WoodFloor", new Color(0.4f, 0.32f, 0.24f));
        CreateDecorCube(parent, "Emergency_Hall_Floor", new Vector3(1f, 0.02f, -21.5f), new Vector3(17.4f, 0.04f, 11.4f), "M_WoodFloor", new Color(0.42f, 0.33f, 0.23f));
        CreateDecorCube(parent, "Lower_Run_Path", new Vector3(20f, 0.016f, -19.2f), new Vector3(44f, 0.04f, 6.2f), "M_WoodFloor", new Color(0.38f, 0.31f, 0.23f));
        CreateDecorCube(parent, "Lower_Return_Path", new Vector3(15f, 0.014f, -29f), new Vector3(49f, 0.04f, 5.8f), "M_WoodFloor", new Color(0.36f, 0.29f, 0.22f));
        CreateDecorCube(parent, "Airplane_Display_Pad", new Vector3(0f, 0.035f, -20.5f), new Vector3(13f, 0.08f, 5f), "M_Stone", new Color(0.28f, 0.28f, 0.26f));
    }

    private static void CreateBlueprintRoomShells(Transform parent)
    {
        CreateBlueprintOuterWalls(parent);

        CreateBlueprintWallZ(parent, "Left_CCTV_Wall_W", -41f, -27f, 18f);
        CreateBlueprintWallZ(parent, "Left_CCTV_Wall_E_Top", -24f, -2.8f, 18f);
        CreateBlueprintWallZ(parent, "Left_CCTV_Wall_E_Bottom", -24f, -27f, -11.2f);
        CreateBlueprintWallX(parent, "Left_CCTV_Wall_N", -41f, -24f, 18f);
        CreateBlueprintWallX(parent, "Left_CCTV_Wall_S", -41f, -24f, -27f);

        CreateBlueprintWallZ(parent, "Center_Block_West", -20f, -8.5f, 22.5f);
        CreateBlueprintWallZ(parent, "Center_Block_East_Upper", 14f, 6.2f, 22.5f);
        CreateBlueprintWallZ(parent, "Center_Block_East_Lower", 14f, -8.5f, -0.2f);
        CreateBlueprintWallX(parent, "Center_Block_North_Left", -20f, -5.4f, 22.5f);
        CreateBlueprintWallX(parent, "Center_Block_North_Right", 0.6f, 14f, 22.5f);
        CreateBlueprintWallX(parent, "Center_Block_South_Left", -20f, -4.2f, -8.5f);
        CreateBlueprintWallX(parent, "Center_Block_South_Right", 5.2f, 14f, -8.5f);
        CreateBlueprintWallZ(parent, "Room_One_Two_Divider_Upper", -10f, 4f, 22.5f);
        CreateBlueprintWallZ(parent, "Room_One_Two_Divider_Lower", -10f, -8.5f, -0.1f);
        CreateBlueprintWallX(parent, "Room_One_Lower_Divider", -20f, -10f, 6.2f);
        CreateBlueprintWallZ(parent, "Room_Two_Equipment_Divider_Top", 0.8f, 8.6f, 22.5f);
        CreateBlueprintWallZ(parent, "Room_Two_Equipment_Divider_Bottom", 0.8f, -8.5f, 3.6f);
        CreateBlueprintWallX(parent, "Stair_Room_South", 0.8f, 14f, 8.6f);
        CreateBlueprintWallZ(parent, "Stair_Room_West", 5.1f, 8.6f, 22.5f);

        CreateBlueprintWallZ(parent, "Right_CCTV_Wall_W", 18f, -6f, 24f);
        CreateBlueprintWallZ(parent, "Right_CCTV_Wall_E", 40f, -6f, 24f);
        CreateBlueprintWallX(parent, "Right_CCTV_Wall_N", 18f, 40f, 24f);
        CreateBlueprintWallX(parent, "Right_CCTV_Wall_S_Left", 18f, 25f, -6f);
        CreateBlueprintWallX(parent, "Right_CCTV_Wall_S_Right", 32f, 40f, -6f);

        CreateBlueprintWallZ(parent, "Emergency_Room_West", -8f, -27f, -16f);
        CreateBlueprintWallZ(parent, "Emergency_Room_East", 10f, -27f, -16f);
        CreateBlueprintWallX(parent, "Emergency_Room_North_Left", -8f, -2.5f, -16f);
        CreateBlueprintWallX(parent, "Emergency_Room_North_Right", 4.5f, 10f, -16f);
        CreateBlueprintWallX(parent, "Emergency_Room_South", -8f, 10f, -27f);
    }

    private static void CreateBlueprintOuterWalls(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;
        CreateBlueprintWallX(parent, "Boundary_North", -halfWidth, halfWidth, halfDepth);
        CreateBlueprintWallX(parent, "Boundary_South", -halfWidth, halfWidth, -halfDepth);
        CreateBlueprintWallZ(parent, "Boundary_West", -halfWidth, -halfDepth, halfDepth);
        CreateBlueprintWallZ(parent, "Boundary_East", halfWidth, -halfDepth, halfDepth);
    }

    private static void CreateBlueprintDoorAndSeamFillers(Transform parent)
    {
        CreateBlueprintDoorway(parent, "Entry_To_Center", new Vector3(-2.4f, 1.55f, 22.5f), true, 5.8f);
        CreateBlueprintDoorway(parent, "Room_One_To_Two", new Vector3(-10f, 1.55f, 1.95f), false, 4.4f);
        CreateBlueprintDoorway(parent, "Room_Two_To_Equipment", new Vector3(0.8f, 1.55f, 6.1f), false, 4.4f);
        CreateBlueprintDoorway(parent, "Main_To_Fire_Door", new Vector3(14f, 1.55f, 3f), false, 4.6f);
        CreateBlueprintDoorway(parent, "Left_CCTV_Service_Door", new Vector3(-24f, 1.55f, -7f), false, 8.4f);
        CreateBlueprintDoorway(parent, "Right_CCTV_South_Door", new Vector3(28.5f, 1.55f, -6f), true, 7.4f);
        CreateBlueprintDoorway(parent, "Emergency_North_Door", new Vector3(1f, 1.55f, -16f), true, 7.4f);
        CreateBlueprintDoorway(parent, "Center_To_Emergency", new Vector3(0.5f, 1.55f, -8.5f), true, 9.4f);

        Vector3[] capPositions =
        {
            new Vector3(-95f, 1.55f, 65f), new Vector3(95f, 1.55f, 65f), new Vector3(-95f, 1.55f, -65f), new Vector3(95f, 1.55f, -65f),
            new Vector3(-41f, 1.55f, 18f), new Vector3(-24f, 1.55f, 18f), new Vector3(-41f, 1.55f, -27f), new Vector3(-24f, 1.55f, -27f),
            new Vector3(-24f, 1.55f, -11.2f), new Vector3(-24f, 1.55f, -2.8f),
            new Vector3(-20f, 1.55f, 22.5f), new Vector3(14f, 1.55f, 22.5f), new Vector3(-20f, 1.55f, -8.5f), new Vector3(14f, 1.55f, -8.5f),
            new Vector3(-5.4f, 1.55f, 22.5f), new Vector3(0.6f, 1.55f, 22.5f), new Vector3(-4.2f, 1.55f, -8.5f), new Vector3(5.2f, 1.55f, -8.5f),
            new Vector3(-10f, 1.55f, 22.5f), new Vector3(-10f, 1.55f, 6.2f), new Vector3(-10f, 1.55f, 4f), new Vector3(-10f, 1.55f, -0.1f), new Vector3(-10f, 1.55f, -8.5f),
            new Vector3(0.8f, 1.55f, 22.5f), new Vector3(0.8f, 1.55f, 8.6f), new Vector3(0.8f, 1.55f, 3.6f), new Vector3(0.8f, 1.55f, -8.5f),
            new Vector3(5.1f, 1.55f, 22.5f), new Vector3(5.1f, 1.55f, 8.6f), new Vector3(14f, 1.55f, 6.2f), new Vector3(14f, 1.55f, -0.2f),
            new Vector3(18f, 1.55f, 24f), new Vector3(40f, 1.55f, 24f), new Vector3(18f, 1.55f, -6f), new Vector3(40f, 1.55f, -6f),
            new Vector3(25f, 1.55f, -6f), new Vector3(32f, 1.55f, -6f),
            new Vector3(-8f, 1.55f, -16f), new Vector3(10f, 1.55f, -16f), new Vector3(-8f, 1.55f, -27f), new Vector3(10f, 1.55f, -27f)
        };

        for (int i = 0; i < capPositions.Length; i++)
        {
            CreateBlueprintCap(parent, $"Blueprint_Sealed_Joint_{i:00}", capPositions[i]);
        }

        CreateDecorCube(parent, "Start_Label_Floor_Marker", new Vector3(-34f, 0.045f, 30.4f), new Vector3(5f, 0.05f, 0.45f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateDecorCube(parent, "Route_Arrow_Upper_Left", new Vector3(-29f, 0.045f, 25.25f), new Vector3(13f, 0.05f, 0.35f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateDecorCube(parent, "Route_Arrow_Upper_Right", new Vector3(9f, 0.045f, 25.25f), new Vector3(18f, 0.05f, 0.35f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateDecorCube(parent, "Route_Arrow_Lower_Right", new Vector3(24f, 0.045f, -18.8f), new Vector3(21f, 0.05f, 0.35f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateDecorCube(parent, "Route_Arrow_Lower_Return", new Vector3(14f, 0.045f, -29f), new Vector3(36f, 0.05f, 0.35f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
    }

    private static void CreateBlueprintEquipment(Transform parent)
    {
        CreateFurnitureCube(parent, "Equipment_Rack_A", new Vector3(4f, 0.75f, 8f), new Vector3(1.6f, 1.5f, 1.6f), "M_DarkMetal", new Color(0.08f, 0.09f, 0.1f));
        CreateFurnitureCube(parent, "Equipment_Rack_B", new Vector3(7f, 0.75f, 8f), new Vector3(1.6f, 1.5f, 1.6f), "M_DarkMetal", new Color(0.08f, 0.09f, 0.1f));
        CreateFurnitureCube(parent, "Bathroom_Block", new Vector3(1f, 0.45f, -21f), new Vector3(6f, 0.9f, 2.4f), "M_Ceramic", new Color(0.86f, 0.88f, 0.86f));
        CreateDecorCube(parent, "Emergency_Exit_Sign", new Vector3(1f, 2.35f, -25.68f), new Vector3(5.2f, 0.7f, 0.08f), "M_Goal", new Color(0.1f, 0.85f, 0.35f));
    }

    private static void CreateBlueprintLighting(Transform parent)
    {
        Vector3[] points =
        {
            new Vector3(-34f, 2.8f, 27f),
            new Vector3(-6f, 2.8f, 15f),
            new Vector3(-5f, 2.8f, 0f),
            new Vector3(-31f, 2.8f, -5f),
            new Vector3(28f, 2.8f, 9f),
            new Vector3(1f, 2.8f, -21f)
        };

        for (int i = 0; i < points.Length; i++)
        {
            CreateDecorCube(parent, $"Blueprint_Lamp_{i:00}", points[i] + Vector3.down * 0.12f, new Vector3(2.4f, 0.08f, 2.4f), "M_Lamp", new Color(0.96f, 0.9f, 0.66f));
            Light light = new GameObject($"Blueprint_Point_Light_{i:00}").AddComponent<Light>();
            light.transform.SetParent(parent);
            light.transform.position = points[i];
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.88f, 0.66f);
        }
    }

    private static void CreateBlueprintDefaultCctvs(Transform parent, CctvDetectionTarget target, StealthGameManager gameManager)
    {
        CreateCctv(parent, "Blueprint_CCTV_01_Entrance", new Vector3(-2.4f, 2.65f, 22.14f), new Vector3(-0.95f, 0f, -0.3f), 12f, 48f, 0.38f, 32f, 18f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_02_Internal_Hall", new Vector3(-9.6f, 2.65f, 4.8f), Vector3.right, 13f, 50f, 0.38f, 52f, 22f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_03_Fire_Door", new Vector3(14.36f, 2.65f, 3f), Vector3.right, 15f, 52f, 0.38f, 58f, 24f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_04_Right_Exterior", new Vector3(39.64f, 2.65f, 9f), Vector3.left, 17f, 55f, 0.38f, 68f, 24f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_05_Airplane_Right", new Vector3(10.36f, 2.65f, -19.2f), Vector3.right, 17f, 54f, 0.38f, 64f, 24f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_06_Airplane_Left", new Vector3(39.64f, 2.65f, -29f), Vector3.left, 18f, 54f, 0.38f, 72f, 24f, target, gameManager);
        CreateCctv(parent, "Blueprint_CCTV_07_Left_Exterior", new Vector3(-40.64f, 2.65f, -4f), Vector3.right, 19f, 56f, 0.38f, 78f, 26f, target, gameManager);
    }

    private static void CreateBlueprintWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = CreateCube(name, position, scale, "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
        wall.transform.SetParent(parent);
        float bottomY = position.y - scale.y * 0.5f;
        float topY = position.y + scale.y * 0.5f;
        CreateDecorCube(parent, $"{name}_Floor_Seal", new Vector3(position.x, bottomY + 0.08f, position.z), new Vector3(scale.x + 0.16f, 0.16f, scale.z + 0.16f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, $"{name}_Top_Seal", new Vector3(position.x, topY + 0.04f, position.z), new Vector3(scale.x + 0.12f, 0.08f, scale.z + 0.12f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
    }

    private static void CreateBlueprintWallX(Transform parent, string name, float xMin, float xMax, float z)
    {
        float centerX = (xMin + xMax) * 0.5f;
        float length = Mathf.Abs(xMax - xMin) + BlueprintWallOverlap;
        CreateBlueprintWall(parent, name, new Vector3(centerX, RoomWallHeight * 0.5f, z), new Vector3(length, RoomWallHeight, BlueprintWallThickness));
    }

    private static void CreateBlueprintWallZ(Transform parent, string name, float x, float zMin, float zMax)
    {
        float centerZ = (zMin + zMax) * 0.5f;
        float length = Mathf.Abs(zMax - zMin) + BlueprintWallOverlap;
        CreateBlueprintWall(parent, name, new Vector3(x, RoomWallHeight * 0.5f, centerZ), new Vector3(BlueprintWallThickness, RoomWallHeight, length));
    }

    private static void CreateBlueprintDoorway(Transform parent, string name, Vector3 position, bool horizontal, float width)
    {
        Vector3 thresholdScale = horizontal
            ? new Vector3(width + 0.4f, 0.12f, BlueprintWallThickness + 0.28f)
            : new Vector3(BlueprintWallThickness + 0.28f, 0.12f, width + 0.4f);
        CreateThreshold(parent, $"{name}_Threshold", new Vector3(position.x, 0.08f, position.z), thresholdScale);
        CreateDoorFrame(parent, $"{name}_Frame", position, horizontal, width);
        Vector3 doorScale = horizontal
            ? new Vector3(width * 0.78f, 2.25f, 0.08f)
            : new Vector3(0.08f, 2.25f, width * 0.78f);
        CreateDecorCube(parent, $"{name}_Door_Leaf", position + Vector3.up * 0.05f, doorScale, "M_Door", new Color(0.28f, 0.16f, 0.08f));
    }

    private static void CreateThreshold(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        CreateDecorCube(parent, name, position, scale, "M_Stone", new Color(0.28f, 0.28f, 0.26f));
    }

    private static void CreateDoorFrame(Transform parent, string name, Vector3 position, bool horizontal, float width)
    {
        if (horizontal)
        {
            CreateDecorCube(parent, $"{name}_Top", position + new Vector3(0f, 1.42f, 0f), new Vector3(width + 0.45f, 0.22f, 0.32f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
            CreateDecorCube(parent, $"{name}_Left", position + new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.22f, 2.85f, 0.32f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
            CreateDecorCube(parent, $"{name}_Right", position + new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.22f, 2.85f, 0.32f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
            return;
        }

        CreateDecorCube(parent, $"{name}_Top", position + new Vector3(0f, 1.42f, 0f), new Vector3(0.32f, 0.22f, width + 0.45f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, $"{name}_Left", position + new Vector3(0f, 0f, -width * 0.5f), new Vector3(0.32f, 2.85f, 0.22f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, $"{name}_Right", position + new Vector3(0f, 0f, width * 0.5f), new Vector3(0.32f, 2.85f, 0.22f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
    }

    private static void CreateBlueprintCap(Transform parent, string name, Vector3 position)
    {
        CreateBlockingDecorCube(parent, name, position, new Vector3(0.92f, RoomWallHeight + 0.08f, 0.92f), "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
    }

    private static void CreateTwoStoryHouseShell(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;
        float exteriorCenterY = ExteriorWallHeight * 0.5f;

        CreateDecorCube(parent, "Front_Porch_Stone", new Vector3(0f, 0.02f, -44.2f), new Vector3(13f, 0.08f, 6f), "M_Stone", new Color(0.33f, 0.34f, 0.33f));
        CreateDecorCube(parent, "Back_Patio_Stone", new Vector3(0f, 0.02f, 44.2f), new Vector3(20f, 0.08f, 6f), "M_Stone", new Color(0.32f, 0.32f, 0.31f));

        CreateHouseWall(parent, "Exterior_Wall_Front_Left", new Vector3(-18.3f, exteriorCenterY, -halfDepth), new Vector3(21.4f, ExteriorWallHeight, 0.55f));
        CreateHouseWall(parent, "Exterior_Wall_Front_Right", new Vector3(18.3f, exteriorCenterY, -halfDepth), new Vector3(21.4f, ExteriorWallHeight, 0.55f));
        CreateHouseWall(parent, "Exterior_Wall_Back", new Vector3(0f, exteriorCenterY, halfDepth), new Vector3(HouseWidth, ExteriorWallHeight, 0.55f));
        CreateHouseWall(parent, "Exterior_Wall_West", new Vector3(-halfWidth, exteriorCenterY, 0f), new Vector3(0.55f, ExteriorWallHeight, HouseDepth));
        CreateHouseWall(parent, "Exterior_Wall_East", new Vector3(halfWidth, exteriorCenterY, 0f), new Vector3(0.55f, ExteriorWallHeight, HouseDepth));

        CreateDecorCube(parent, "Front_Door_Frame_Top", new Vector3(0f, 3.1f, -halfDepth - 0.03f), new Vector3(8.2f, 0.32f, 0.72f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, "Front_Door_Left_Frame", new Vector3(-4.25f, 1.55f, -halfDepth - 0.03f), new Vector3(0.32f, 3.1f, 0.72f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, "Front_Door_Right_Frame", new Vector3(4.25f, 1.55f, -halfDepth - 0.03f), new Vector3(0.32f, 3.1f, 0.72f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));

        CreateSecondFloorSlabs(parent);
        CreateHouseTrim(parent);
    }

    private static void CreateSecondFloorSlabs(Transform parent)
    {
        float y = SecondFloorY - 0.08f;
        CreateHouseFloor(parent, "Second_Floor_West_Wing", new Vector3(-19f, y, 0f), new Vector3(20f, 0.16f, HouseDepth - 1f));
        CreateHouseFloor(parent, "Second_Floor_East_Wing", new Vector3(19f, y, 0f), new Vector3(20f, 0.16f, HouseDepth - 1f));
        CreateHouseFloor(parent, "Second_Floor_Front_Landing", new Vector3(0f, y, -26f), new Vector3(18f, 0.16f, 29f));
        CreateHouseFloor(parent, "Second_Floor_Back_Landing", new Vector3(0f, y, 27f), new Vector3(18f, 0.16f, 28f));

        CreateDecorCube(parent, "Stairwell_Railing_North", new Vector3(0f, SecondFloorY + 0.65f, 8f), new Vector3(15f, 1.3f, 0.18f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Stairwell_Railing_West", new Vector3(-7.5f, SecondFloorY + 0.65f, -2.5f), new Vector3(0.18f, 1.3f, 21f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Stairwell_Railing_East", new Vector3(7.5f, SecondFloorY + 0.65f, -2.5f), new Vector3(0.18f, 1.3f, 21f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
    }

    private static void CreateHouseInteriorWalls(Transform parent)
    {
        CreateHouseWall(parent, "Ground_Hall_Left_Wall_A", new Vector3(-9f, RoomWallHeight * 0.5f, -25f), new Vector3(0.42f, RoomWallHeight, 24f));
        CreateHouseWall(parent, "Ground_Hall_Right_Wall_A", new Vector3(9f, RoomWallHeight * 0.5f, -25f), new Vector3(0.42f, RoomWallHeight, 24f));
        CreateHouseWall(parent, "Ground_Hall_Left_Wall_B", new Vector3(-9f, RoomWallHeight * 0.5f, 17f), new Vector3(0.42f, RoomWallHeight, 26f));
        CreateHouseWall(parent, "Ground_Hall_Right_Wall_B", new Vector3(9f, RoomWallHeight * 0.5f, 17f), new Vector3(0.42f, RoomWallHeight, 26f));
        CreateHouseWall(parent, "Ground_Living_Divider", new Vector3(-19f, RoomWallHeight * 0.5f, -7f), new Vector3(19f, RoomWallHeight, 0.42f));
        CreateHouseWall(parent, "Ground_Kitchen_Divider", new Vector3(19f, RoomWallHeight * 0.5f, -7f), new Vector3(19f, RoomWallHeight, 0.42f));
        CreateHouseWall(parent, "Ground_Back_Room_Divider", new Vector3(0f, RoomWallHeight * 0.5f, 18f), new Vector3(18f, RoomWallHeight, 0.42f));

        float upperCenterY = SecondFloorY + RoomWallHeight * 0.5f;
        CreateHouseWall(parent, "Upper_Hall_Left_Wall_A", new Vector3(-9f, upperCenterY, -26f), new Vector3(0.42f, RoomWallHeight, 22f));
        CreateHouseWall(parent, "Upper_Hall_Right_Wall_A", new Vector3(9f, upperCenterY, -26f), new Vector3(0.42f, RoomWallHeight, 22f));
        CreateHouseWall(parent, "Upper_Hall_Left_Wall_B", new Vector3(-9f, upperCenterY, 22f), new Vector3(0.42f, RoomWallHeight, 24f));
        CreateHouseWall(parent, "Upper_Hall_Right_Wall_B", new Vector3(9f, upperCenterY, 22f), new Vector3(0.42f, RoomWallHeight, 24f));
        CreateHouseWall(parent, "Upper_Master_Divider", new Vector3(19f, upperCenterY, 18f), new Vector3(19f, RoomWallHeight, 0.42f));
        CreateHouseWall(parent, "Upper_Bedroom_Divider", new Vector3(-19f, upperCenterY, 4f), new Vector3(19f, RoomWallHeight, 0.42f));
    }

    private static void CreateHouseStaircase(Transform parent)
    {
        int steps = 22;
        float startZ = -15.5f;
        float depth = 0.78f;
        float width = 7.2f;
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float y = t * SecondFloorY;
            float z = startZ + i * depth;
            GameObject step = CreateCube($"Main_Stair_Step_{i:00}", new Vector3(0f, y, z), new Vector3(width, 0.16f, depth + 0.05f), "M_StairWood", new Color(0.31f, 0.21f, 0.13f));
            step.transform.SetParent(parent);
        }

        CreateDecorCube(parent, "Stair_Left_Rail", new Vector3(-4.05f, 1.7f, -7f), new Vector3(0.18f, 2.9f, 18f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Stair_Right_Rail", new Vector3(4.05f, 1.7f, -7f), new Vector3(0.18f, 2.9f, 18f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
    }

    private static void CreateHouseFurnishings(Transform parent)
    {
        CreateSofa(parent, new Vector3(-20f, 0.55f, -24f), 0f);
        CreateCoffeeTable(parent, new Vector3(-20f, 0.32f, -18f));
        CreateRug(parent, "Living_Room_Rug", new Vector3(-20f, 0.015f, -18f), new Vector3(10f, 0.03f, 7f), new Color(0.45f, 0.08f, 0.07f));
        CreateBookshelf(parent, new Vector3(-27.5f, 1.45f, -1f), 90f);
        CreateBookshelf(parent, new Vector3(-27.5f, 1.45f, 28f), 90f);

        CreateKitchen(parent);
        CreateDiningSet(parent, new Vector3(18f, 0.45f, 8f));
        CreateBed(parent, "Upper_Master_Bed", new Vector3(19f, SecondFloorY + 0.45f, 29f), 180f);
        CreateBed(parent, "Upper_Guest_Bed", new Vector3(-19f, SecondFloorY + 0.45f, 26f), 180f);
        CreateDesk(parent, new Vector3(-20f, SecondFloorY + 0.42f, -23f), 0f);
        CreateWardrobe(parent, new Vector3(27.4f, SecondFloorY + 1.1f, 26f), 90f);
        CreateBathroom(parent, new Vector3(20f, SecondFloorY, -25f));

        CreatePlant(parent, new Vector3(-25f, 0.65f, -34f));
        CreatePlant(parent, new Vector3(25f, 0.65f, -34f));
        CreatePlant(parent, new Vector3(-25f, SecondFloorY + 0.65f, 36f));
        CreatePictureFrames(parent);
    }

    private static void CreateHouseLighting(Transform parent)
    {
        Vector3[] points =
        {
            new Vector3(0f, 2.75f, -28f),
            new Vector3(-20f, 2.75f, -18f),
            new Vector3(20f, 2.75f, -18f),
            new Vector3(-20f, 2.75f, 18f),
            new Vector3(20f, 2.75f, 18f),
            new Vector3(0f, SecondFloorY + 2.75f, -25f),
            new Vector3(-20f, SecondFloorY + 2.75f, 23f),
            new Vector3(20f, SecondFloorY + 2.75f, 23f)
        };

        for (int i = 0; i < points.Length; i++)
        {
            CreateDecorCube(parent, $"House_Ceiling_Lamp_{i:00}", points[i] + Vector3.down * 0.12f, new Vector3(2.3f, 0.08f, 2.3f), "M_Lamp", new Color(0.96f, 0.9f, 0.66f));
            Light light = new GameObject($"House_Point_Light_{i:00}").AddComponent<Light>();
            light.transform.SetParent(parent);
            light.transform.position = points[i];
            light.type = LightType.Point;
            light.range = 13f;
            light.intensity = 1.55f;
            light.color = new Color(1f, 0.88f, 0.66f);
        }
    }

    private static void CreateHouseExteriorDetails(Transform parent)
    {
        for (float x = -20f; x <= 20f; x += 10f)
        {
            CreateDecorCube(parent, $"Front_Window_{x}", new Vector3(x, 2.15f, -41.32f), new Vector3(4.2f, 1.35f, 0.08f), "M_WindowGlass", new Color(0.26f, 0.55f, 0.72f));
            CreateDecorCube(parent, $"Upper_Front_Window_{x}", new Vector3(x, SecondFloorY + 2.05f, -41.32f), new Vector3(4.2f, 1.35f, 0.08f), "M_WindowGlass", new Color(0.26f, 0.55f, 0.72f));
        }

        CreateDecorCube(parent, "Goal_Room_Door", new Vector3(18f, SecondFloorY + 0.12f, 32.4f), new Vector3(5.8f, 0.08f, 0.18f), "M_Goal", new Color(0.1f, 0.85f, 0.35f));
        CreateDecorCube(parent, "Roof_Visual_Plate", new Vector3(0f, ExteriorWallHeight + 0.18f, 0f), new Vector3(HouseWidth + 2f, 0.24f, HouseDepth + 2f), "M_RoofDark", new Color(0.12f, 0.09f, 0.075f));
    }

    private static void CreateHouseGapFillers(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;

        CreateDecorCube(parent, "Ground_Floor_Front_Edge_Cover", new Vector3(0f, 0.065f, -halfDepth + 0.08f), new Vector3(HouseWidth + 0.35f, 0.08f, 0.32f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Ground_Floor_Back_Edge_Cover", new Vector3(0f, 0.065f, halfDepth - 0.08f), new Vector3(HouseWidth + 0.35f, 0.08f, 0.32f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Ground_Floor_West_Edge_Cover", new Vector3(-halfWidth + 0.08f, 0.065f, 0f), new Vector3(0.32f, 0.08f, HouseDepth + 0.35f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Ground_Floor_East_Edge_Cover", new Vector3(halfWidth - 0.08f, 0.065f, 0f), new Vector3(0.32f, 0.08f, HouseDepth + 0.35f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));

        CreateDecorCube(parent, "Front_Door_Threshold_Outer", new Vector3(0f, 0.12f, -halfDepth - 0.34f), new Vector3(8.8f, 0.16f, 0.62f), "M_Stone", new Color(0.28f, 0.28f, 0.26f));
        CreateDecorCube(parent, "Front_Door_Threshold_Inner", new Vector3(0f, 0.12f, -halfDepth + 0.34f), new Vector3(8.8f, 0.16f, 0.62f), "M_Stone", new Color(0.28f, 0.28f, 0.26f));
        CreateDecorCube(parent, "Front_Door_Left_Return", new Vector3(-4.55f, 1.62f, -halfDepth + 0.34f), new Vector3(0.5f, 3.05f, 0.55f), "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
        CreateDecorCube(parent, "Front_Door_Right_Return", new Vector3(4.55f, 1.62f, -halfDepth + 0.34f), new Vector3(0.5f, 3.05f, 0.55f), "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
        CreateDecorCube(parent, "Front_Door_Open_Panel_Left", new Vector3(-2.95f, 1.45f, -halfDepth + 0.18f), new Vector3(1.8f, 2.65f, 0.14f), "M_Door", new Color(0.28f, 0.16f, 0.08f));
        CreateDecorCube(parent, "Front_Door_Open_Panel_Right", new Vector3(2.95f, 1.45f, -halfDepth + 0.18f), new Vector3(1.8f, 2.65f, 0.14f), "M_Door", new Color(0.28f, 0.16f, 0.08f));

        CreateDecorCube(parent, "Second_Floor_Seam_West_Front", new Vector3(-9f, SecondFloorY + 0.025f, -26f), new Vector3(0.34f, 0.08f, 29.5f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Seam_East_Front", new Vector3(9f, SecondFloorY + 0.025f, -26f), new Vector3(0.34f, 0.08f, 29.5f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Seam_West_Back", new Vector3(-9f, SecondFloorY + 0.025f, 27f), new Vector3(0.34f, 0.08f, 28.5f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Seam_East_Back", new Vector3(9f, SecondFloorY + 0.025f, 27f), new Vector3(0.34f, 0.08f, 28.5f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Stairwell_Lip_Front", new Vector3(0f, SecondFloorY + 0.035f, -11.5f), new Vector3(18.5f, 0.08f, 0.34f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Stairwell_Lip_Back", new Vector3(0f, SecondFloorY + 0.035f, 13f), new Vector3(18.5f, 0.08f, 0.34f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Stairwell_Lip_Left", new Vector3(-9f, SecondFloorY + 0.035f, 0.75f), new Vector3(0.34f, 0.08f, 24.8f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));
        CreateDecorCube(parent, "Second_Floor_Stairwell_Lip_Right", new Vector3(9f, SecondFloorY + 0.035f, 0.75f), new Vector3(0.34f, 0.08f, 24.8f), "M_WoodTrim", new Color(0.19f, 0.13f, 0.075f));

        CreateCornerFiller(parent, "Exterior_Corner_NorthWest", new Vector3(-halfWidth, ExteriorWallHeight * 0.5f, halfDepth));
        CreateCornerFiller(parent, "Exterior_Corner_NorthEast", new Vector3(halfWidth, ExteriorWallHeight * 0.5f, halfDepth));
        CreateCornerFiller(parent, "Exterior_Corner_SouthWest", new Vector3(-halfWidth, ExteriorWallHeight * 0.5f, -halfDepth));
        CreateCornerFiller(parent, "Exterior_Corner_SouthEast", new Vector3(halfWidth, ExteriorWallHeight * 0.5f, -halfDepth));

        CreateDecorCube(parent, "Goal_Door_Frame_Left", new Vector3(14f, SecondFloorY + 1.5f, 32.35f), new Vector3(0.24f, 2.85f, 0.36f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, "Goal_Door_Frame_Right", new Vector3(22f, SecondFloorY + 1.5f, 32.35f), new Vector3(0.24f, 2.85f, 0.36f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
        CreateDecorCube(parent, "Goal_Door_Frame_Top", new Vector3(18f, SecondFloorY + 2.9f, 32.35f), new Vector3(8.2f, 0.24f, 0.36f), "M_DoorFrame", new Color(0.16f, 0.11f, 0.07f));
    }

    private static void CreateCornerFiller(Transform parent, string name, Vector3 position)
    {
        CreateDecorCube(parent, name, position, new Vector3(0.72f, ExteriorWallHeight + 0.04f, 0.72f), "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
    }

    private static void CreateHouseDefaultCctvs(Transform parent, CctvDetectionTarget target, StealthGameManager gameManager)
    {
        CreateCctv(parent, "House_CCTV_Foyer_Cross", new Vector3(-8.64f, 2.75f, -18f), Vector3.right, 15f, 52f, 0.38f, 36f, 26f, target, gameManager);
        CreateCctv(parent, "House_CCTV_Living_West", new Vector3(-28.64f, 2.7f, -11f), Vector3.right, 16f, 54f, 0.42f, 82f, 30f, target, gameManager);
        CreateCctv(parent, "House_CCTV_Kitchen_East", new Vector3(28.64f, 2.7f, -3f), Vector3.left, 16f, 54f, 0.42f, 82f, 30f, target, gameManager);
        CreateCctv(parent, "House_CCTV_Upper_Hall", new Vector3(0f, SecondFloorY + 2.65f, 40.64f), Vector3.back, 18f, 58f, 0.35f, 92f, 34f, target, gameManager);
        CreateCctv(parent, "House_CCTV_Master_Room", new Vector3(28.64f, SecondFloorY + 2.55f, 24f), Vector3.left, 15f, 52f, 0.35f, 70f, 30f, target, gameManager);
    }

    private static void CreateHouseWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = CreateCube(name, position, scale, "M_HouseWall", new Color(0.72f, 0.69f, 0.62f));
        wall.transform.SetParent(parent);
        CreateDecorCube(parent, $"{name}_BaseTrim", position + new Vector3(0f, -scale.y * 0.5f + 0.13f, 0f), new Vector3(scale.x + 0.06f, 0.18f, scale.z + 0.06f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
    }

    private static void CreateHouseFloor(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject floor = CreateCube(name, position, scale, "M_WoodFloor", new Color(0.43f, 0.33f, 0.24f));
        floor.transform.SetParent(parent);
    }

    private static void CreateHouseTrim(Transform parent)
    {
        float halfWidth = HouseWidth * 0.5f;
        float halfDepth = HouseDepth * 0.5f;
        CreateDecorCube(parent, "Exterior_Top_Trim_Front", new Vector3(0f, ExteriorWallHeight + 0.08f, -halfDepth), new Vector3(HouseWidth, 0.22f, 0.74f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Exterior_Top_Trim_Back", new Vector3(0f, ExteriorWallHeight + 0.08f, halfDepth), new Vector3(HouseWidth, 0.22f, 0.74f), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Exterior_Top_Trim_West", new Vector3(-halfWidth, ExteriorWallHeight + 0.08f, 0f), new Vector3(0.74f, 0.22f, HouseDepth), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
        CreateDecorCube(parent, "Exterior_Top_Trim_East", new Vector3(halfWidth, ExteriorWallHeight + 0.08f, 0f), new Vector3(0.74f, 0.22f, HouseDepth), "M_WoodTrim", new Color(0.18f, 0.12f, 0.07f));
    }

    private static GameObject CreateFurnitureCube(Transform parent, string name, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject cube = CreateCube(name, position, scale, materialName, color);
        cube.transform.SetParent(parent, false);
        return cube;
    }

    private static void CreateSofa(Transform parent, Vector3 position, float yaw)
    {
        GameObject root = new GameObject("Living_Room_Sofa");
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateFurnitureCube(root.transform, "Sofa_Seat", Vector3.zero, new Vector3(7f, 0.55f, 2.2f), "M_SofaFabric", new Color(0.18f, 0.28f, 0.36f));
        CreateFurnitureCube(root.transform, "Sofa_Back", new Vector3(0f, 0.75f, -1.05f), new Vector3(7.2f, 1.1f, 0.35f), "M_SofaFabric", new Color(0.14f, 0.22f, 0.3f));
        CreateFurnitureCube(root.transform, "Sofa_Left_Arm", new Vector3(-3.75f, 0.55f, 0f), new Vector3(0.35f, 0.9f, 2.4f), "M_SofaFabric", new Color(0.14f, 0.22f, 0.3f));
        CreateFurnitureCube(root.transform, "Sofa_Right_Arm", new Vector3(3.75f, 0.55f, 0f), new Vector3(0.35f, 0.9f, 2.4f), "M_SofaFabric", new Color(0.14f, 0.22f, 0.3f));
    }

    private static void CreateCoffeeTable(Transform parent, Vector3 position)
    {
        CreateFurnitureCube(parent, "Coffee_Table_Top", position + new Vector3(0f, 0.22f, 0f), new Vector3(4.4f, 0.18f, 2.4f), "M_DarkWood", new Color(0.22f, 0.13f, 0.07f));
        CreateFurnitureCube(parent, "Coffee_Table_Base", position + new Vector3(0f, -0.02f, 0f), new Vector3(3.3f, 0.24f, 1.4f), "M_DarkWood", new Color(0.16f, 0.09f, 0.05f));
    }

    private static void CreateKitchen(Transform parent)
    {
        for (int i = 0; i < 5; i++)
        {
            CreateFurnitureCube(parent, $"Kitchen_Counter_{i}", new Vector3(16f + i * 2.4f, 0.55f, -28f), new Vector3(2.25f, 1.1f, 1.25f), "M_Cabinet", new Color(0.36f, 0.32f, 0.27f));
        }

        CreateFurnitureCube(parent, "Kitchen_Island", new Vector3(20.5f, 0.55f, -19f), new Vector3(7.8f, 1.1f, 2.4f), "M_Cabinet", new Color(0.32f, 0.29f, 0.25f));
        CreateFurnitureCube(parent, "Fridge", new Vector3(27.2f, 1.35f, -33.5f), new Vector3(2.2f, 2.7f, 1.8f), "M_Appliance", new Color(0.66f, 0.68f, 0.67f));
        CreateFurnitureCube(parent, "Stove", new Vector3(14.8f, 0.68f, -33.5f), new Vector3(2.6f, 1.35f, 1.8f), "M_Appliance", new Color(0.09f, 0.1f, 0.1f));
    }

    private static void CreateDiningSet(Transform parent, Vector3 position)
    {
        CreateFurnitureCube(parent, "Dining_Table", position + new Vector3(0f, 0.35f, 0f), new Vector3(6.4f, 0.22f, 3.2f), "M_DarkWood", new Color(0.21f, 0.12f, 0.06f));
        for (int i = 0; i < 6; i++)
        {
            float x = -2.5f + (i % 3) * 2.5f;
            float z = i < 3 ? -2.35f : 2.35f;
            CreateFurnitureCube(parent, $"Dining_Chair_{i}", position + new Vector3(x, 0.35f, z), new Vector3(0.9f, 0.7f, 0.9f), "M_Chair", new Color(0.17f, 0.1f, 0.06f));
        }
    }

    private static void CreateBed(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateFurnitureCube(root.transform, "Bed_Frame", Vector3.zero, new Vector3(5.6f, 0.55f, 8f), "M_DarkWood", new Color(0.19f, 0.11f, 0.06f));
        CreateFurnitureCube(root.transform, "Mattress", new Vector3(0f, 0.42f, 0.2f), new Vector3(5.1f, 0.38f, 7.2f), "M_Bedding", new Color(0.86f, 0.82f, 0.73f));
        CreateFurnitureCube(root.transform, "Pillow_Left", new Vector3(-1.3f, 0.75f, 2.55f), new Vector3(1.7f, 0.25f, 1f), "M_Pillow", new Color(0.96f, 0.94f, 0.88f));
        CreateFurnitureCube(root.transform, "Pillow_Right", new Vector3(1.3f, 0.75f, 2.55f), new Vector3(1.7f, 0.25f, 1f), "M_Pillow", new Color(0.96f, 0.94f, 0.88f));
    }

    private static void CreateBookshelf(Transform parent, Vector3 position, float yaw)
    {
        GameObject shelf = new GameObject($"Bookshelf_{position.x}_{position.z}");
        shelf.transform.SetParent(parent);
        shelf.transform.position = position;
        shelf.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateFurnitureCube(shelf.transform, "Bookshelf_Frame", Vector3.zero, new Vector3(0.8f, 2.9f, 6.5f), "M_DarkWood", new Color(0.2f, 0.12f, 0.06f));
        for (int i = 0; i < 4; i++)
        {
            CreateDecorCube(shelf.transform, $"Book_Row_{i}", new Vector3(0.43f, -0.95f + i * 0.62f, 0f), new Vector3(0.12f, 0.24f, 5.6f), "M_Books", new Color(0.58f - i * 0.08f, 0.2f + i * 0.1f, 0.16f + i * 0.07f));
        }
    }

    private static void CreateDesk(Transform parent, Vector3 position, float yaw)
    {
        GameObject desk = new GameObject("Upper_Study_Desk");
        desk.transform.SetParent(parent);
        desk.transform.position = position;
        desk.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateFurnitureCube(desk.transform, "Desk_Top", Vector3.zero, new Vector3(5.2f, 0.22f, 2.1f), "M_DarkWood", new Color(0.2f, 0.11f, 0.06f));
        CreateDecorCube(desk.transform, "Monitor", new Vector3(0f, 0.62f, -0.45f), new Vector3(1.7f, 1f, 0.08f), "M_Screen", new Color(0.03f, 0.3f, 0.34f));
    }

    private static void CreateWardrobe(Transform parent, Vector3 position, float yaw)
    {
        GameObject wardrobe = CreateFurnitureCube(parent, "Wardrobe", position, new Vector3(1.6f, 2.2f, 6.5f), "M_DarkWood", new Color(0.18f, 0.1f, 0.055f));
        wardrobe.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void CreateBathroom(Transform parent, Vector3 origin)
    {
        CreateFurnitureCube(parent, "Bathroom_Tub", origin + new Vector3(4.8f, 0.35f, -7.5f), new Vector3(4.8f, 0.7f, 2.2f), "M_Ceramic", new Color(0.86f, 0.88f, 0.86f));
        CreateFurnitureCube(parent, "Bathroom_Sink", origin + new Vector3(-3.2f, 0.55f, -7.5f), new Vector3(2f, 1.1f, 1.2f), "M_Ceramic", new Color(0.86f, 0.88f, 0.86f));
        CreateFurnitureCube(parent, "Bathroom_Toilet", origin + new Vector3(-5.5f, 0.45f, -2.2f), new Vector3(1.25f, 0.9f, 1.4f), "M_Ceramic", new Color(0.88f, 0.9f, 0.88f));
    }

    private static void CreateRug(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        CreateDecorCube(parent, name, position, scale, "M_Rug", color);
    }

    private static void CreatePlant(Transform parent, Vector3 position)
    {
        CreateFurnitureCube(parent, $"Plant_Pot_{position.x}_{position.z}", position, new Vector3(1f, 0.65f, 1f), "M_PlantPot", new Color(0.33f, 0.18f, 0.08f));
        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = $"Plant_Leaves_{position.x}_{position.z}";
        leaves.transform.SetParent(parent);
        leaves.transform.position = position + new Vector3(0f, 0.85f, 0f);
        leaves.transform.localScale = new Vector3(1.55f, 1.25f, 1.55f);
        leaves.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_PlantLeaves", new Color(0.08f, 0.34f, 0.15f));
        Object.DestroyImmediate(leaves.GetComponent<Collider>());
    }

    private static void CreatePictureFrames(Transform parent)
    {
        CreateDecorCube(parent, "Living_Wall_Painting", new Vector3(-9.25f, 1.95f, -20f), new Vector3(0.08f, 1.4f, 3.4f), "M_Painting", new Color(0.68f, 0.26f, 0.18f));
        CreateDecorCube(parent, "Dining_Wall_Painting", new Vector3(9.25f, 1.95f, 8f), new Vector3(0.08f, 1.4f, 3.4f), "M_Painting", new Color(0.18f, 0.38f, 0.58f));
        CreateDecorCube(parent, "Upper_Hall_Painting", new Vector3(-9.25f, SecondFloorY + 1.95f, 14f), new Vector3(0.08f, 1.4f, 3.4f), "M_Painting", new Color(0.68f, 0.52f, 0.18f));
    }

    private static void CreatePerimeterWalls(Transform parent)
    {
        CreateWall(parent, "Outer_Wall_North", new Vector3(0f, 2.25f, 78f), new Vector3(84f, 4.5f, 0.55f));
        CreateWall(parent, "Outer_Wall_South", new Vector3(0f, 2.25f, -78f), new Vector3(84f, 4.5f, 0.55f));
        CreateWall(parent, "Outer_Wall_West", new Vector3(-42f, 2.25f, 0f), new Vector3(0.55f, 4.5f, 156f));
        CreateWall(parent, "Outer_Wall_East", new Vector3(42f, 2.25f, 0f), new Vector3(0.55f, 4.5f, 156f));

        CreateDecorCube(parent, "North_Top_Service_Rail", new Vector3(0f, WallTopTrimY, 77.55f), new Vector3(82f, 0.18f, 0.28f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "South_Top_Service_Rail", new Vector3(0f, WallTopTrimY, -77.55f), new Vector3(82f, 0.18f, 0.28f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "West_Top_Service_Rail", new Vector3(-41.55f, WallTopTrimY, 0f), new Vector3(0.28f, 0.18f, 154f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "East_Top_Service_Rail", new Vector3(41.55f, WallTopTrimY, 0f), new Vector3(0.28f, 0.18f, 154f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
    }

    private static void CreateFloorPanelGrid(Transform parent)
    {
        for (float x = -36f; x <= 36f; x += 6f)
        {
            CreateDecorCube(parent, $"Floor_Seam_X_{x}", new Vector3(x, 0.012f, 0f), new Vector3(0.035f, 0.025f, 154f), "M_FloorSeam", new Color(0.07f, 0.08f, 0.085f));
        }

        for (float z = -72f; z <= 72f; z += 6f)
        {
            CreateDecorCube(parent, $"Floor_Seam_Z_{z}", new Vector3(0f, 0.014f, z), new Vector3(82f, 0.025f, 0.035f), "M_FloorSeam", new Color(0.07f, 0.08f, 0.085f));
        }

        CreateFloorStripe(parent, "Start_Bay_Blue_Line", new Vector3(0f, 0.035f, -72f), new Vector3(18f, 0.03f, 0.22f), "M_StartStripe", new Color(0.05f, 0.45f, 0.95f));
        CreateFloorStripe(parent, "Goal_Bay_Green_Line", new Vector3(0f, 0.035f, 71f), new Vector3(18f, 0.03f, 0.22f), "M_Goal", new Color(0.1f, 0.85f, 0.35f));

        float[] checkpointZ = { -54f, -30f, -6f, 18f, 42f, 62f };
        foreach (float z in checkpointZ)
        {
            CreateFloorStripe(parent, $"Hazard_Crossing_{z}", new Vector3(0f, 0.038f, z), new Vector3(10f, 0.03f, 0.18f), "M_DangerStripe", new Color(1f, 0.68f, 0.08f));
        }
    }

    private static void CreateInteriorSecurityLayout(Transform parent)
    {
        CreateGateWall(parent, "Security_Gate_A", -54f, -10f, 8.5f);
        CreateGateWall(parent, "Security_Gate_B", -30f, 13f, 9f);
        CreateGateWall(parent, "Security_Gate_C", -6f, -14f, 8f);
        CreateGateWall(parent, "Security_Gate_D", 18f, 8f, 10f);
        CreateGateWall(parent, "Security_Gate_E", 42f, -8f, 8.5f);
        CreateGateWall(parent, "Final_Security_Gate", 62f, 0f, 11f);

        CreateVerticalPartition(parent, "West_Lab_Block_A", -24f, -64f, -38f, -4f, 7f);
        CreateVerticalPartition(parent, "East_Storage_Block_A", 24f, -48f, -18f, 5f, 7f);
        CreateVerticalPartition(parent, "West_Server_Block", -24f, -22f, 12f, -7f, 7f);
        CreateVerticalPartition(parent, "East_Control_Block", 24f, 8f, 38f, 6f, 7f);
        CreateVerticalPartition(parent, "West_Archive_Block", -24f, 34f, 66f, -5f, 8f);

        for (float z = -66f; z <= 66f; z += 22f)
        {
            CreatePillar(parent, $"Heavy_Pillar_W_{z}", new Vector3(-35f, 1.65f, z));
            CreatePillar(parent, $"Heavy_Pillar_E_{z}", new Vector3(35f, 1.65f, z));
            CreatePillar(parent, $"Center_Pillar_L_{z}", new Vector3(-9f, 1.65f, z + 7f));
            CreatePillar(parent, $"Center_Pillar_R_{z}", new Vector3(9f, 1.65f, z - 7f));
        }

        CreateCoverRun(parent, -16f, -67f, 4, 1);
        CreateCoverRun(parent, 17f, -59f, 5, -1);
        CreateCoverRun(parent, -30f, -36f, 4, 1);
        CreateCoverRun(parent, 31f, -24f, 4, -1);
        CreateCoverRun(parent, -16f, -4f, 5, 1);
        CreateCoverRun(parent, 18f, 9f, 5, -1);
        CreateCoverRun(parent, -31f, 33f, 4, 1);
        CreateCoverRun(parent, 29f, 48f, 5, -1);
    }

    private static void CreateIndustrialProps(Transform parent)
    {
        Vector3[] crateBases =
        {
            new Vector3(-34f, 0.55f, -70f),
            new Vector3(31f, 0.55f, -64f),
            new Vector3(-33f, 0.55f, -44f),
            new Vector3(30f, 0.55f, -10f),
            new Vector3(-32f, 0.55f, 16f),
            new Vector3(31f, 0.55f, 30f),
            new Vector3(-31f, 0.55f, 55f),
            new Vector3(26f, 0.55f, 66f)
        };

        for (int i = 0; i < crateBases.Length; i++)
        {
            CreateCrateStack(parent, crateBases[i], 3 + i % 3);
        }

        CreateControlConsole(parent, "Security_Console_A", new Vector3(-31f, 0.65f, -51f), 90f);
        CreateControlConsole(parent, "Security_Console_B", new Vector3(31f, 0.65f, -28f), -90f);
        CreateControlConsole(parent, "Server_Terminal_A", new Vector3(-31f, 0.65f, 5f), 90f);
        CreateControlConsole(parent, "Control_Room_Terminal", new Vector3(31f, 0.65f, 28f), -90f);

        for (float z = -60f; z <= 58f; z += 18f)
        {
            CreateDecorCube(parent, $"Cable_Tray_W_{z}", new Vector3(-39.5f, 3.65f, z), new Vector3(0.18f, 0.14f, 12f), "M_DarkMetal", new Color(0.06f, 0.065f, 0.07f));
            CreateDecorCube(parent, $"Cable_Tray_E_{z}", new Vector3(39.5f, 3.65f, z), new Vector3(0.18f, 0.14f, 12f), "M_DarkMetal", new Color(0.06f, 0.065f, 0.07f));
        }
    }

    private static void CreateCeilingAndLighting(Transform parent)
    {
        for (float z = -72f; z <= 72f; z += 12f)
        {
            CreateDecorCube(parent, $"Ceiling_Beam_{z}", new Vector3(0f, CeilingBeamY, z), new Vector3(82f, 0.18f, 0.32f), "M_CeilingBeam", new Color(0.09f, 0.1f, 0.105f));
        }

        for (float z = -66f; z <= 66f; z += 18f)
        {
            CreateOverheadLight(parent, new Vector3(-18f, CeilingLightY, z));
            CreateOverheadLight(parent, new Vector3(18f, CeilingLightY, z + 9f));
        }

        for (float z = -65f; z <= 65f; z += 26f)
        {
            CreatePipe(parent, $"Red_Service_Pipe_{z}", new Vector3(-40.8f, ServicePipeY, z), new Vector3(-40.8f, ServicePipeY, z + 18f), 0.08f, "M_RedPipe", new Color(0.65f, 0.08f, 0.06f));
            CreatePipe(parent, $"Blue_Service_Pipe_{z}", new Vector3(40.8f, ServicePipeY - 0.2f, z), new Vector3(40.8f, ServicePipeY - 0.2f, z + 18f), 0.07f, "M_BluePipe", new Color(0.05f, 0.24f, 0.72f));
        }
    }

    private static void CreateEnvironmentalStoryDetails(Transform parent)
    {
        CreateDecorCube(parent, "Loading_Dock_Door", new Vector3(0f, 2.65f, -77.35f), new Vector3(16f, 4.3f, 0.18f), "M_Door", new Color(0.18f, 0.2f, 0.21f));
        CreateDecorCube(parent, "Final_Vault_Door", new Vector3(0f, 2.65f, 77.35f), new Vector3(14f, 4.3f, 0.18f), "M_GoalDoor", new Color(0.11f, 0.24f, 0.18f));

        for (float z = -61f; z <= 61f; z += 24f)
        {
            CreateDecorCube(parent, $"Warning_Sign_W_{z}", new Vector3(-41.68f, WallSignY, z), new Vector3(0.04f, 0.8f, 2.2f), "M_SignYellow", new Color(1f, 0.75f, 0.08f));
            CreateDecorCube(parent, $"Warning_Sign_E_{z}", new Vector3(41.68f, WallSignY, z + 10f), new Vector3(0.04f, 0.8f, 2.2f), "M_SignRed", new Color(0.85f, 0.1f, 0.08f));
        }
    }

    private static void CreateGoal(Transform parent, StealthGameManager gameManager)
    {
        CreateGoal(parent, gameManager, new Vector3(0f, 0.08f, 27f), new Vector3(7f, 0.16f, 2.8f));
    }

    private static void CreateGoal(Transform parent, StealthGameManager gameManager, Vector3 position, Vector3 scale)
    {
        GameObject goal = CreateCube("Goal_Zone", position, scale, "M_Goal", new Color(0.1f, 0.85f, 0.35f));
        goal.transform.SetParent(parent);

        Collider goalCollider = goal.GetComponent<Collider>();
        goalCollider.isTrigger = true;

        GoalZone goalZone = goal.AddComponent<GoalZone>();
        goalZone.Configure(gameManager);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Goal_Marker";
        marker.transform.SetParent(goal.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        marker.transform.localScale = new Vector3(1.8f, 0.08f, 1.8f);
        marker.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Goal", new Color(0.1f, 0.85f, 0.35f));
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }

    private static void CreateGateWall(Transform parent, string name, float z, float gapCenterX, float gapWidth)
    {
        float leftWidth = gapCenterX + 42f - gapWidth * 0.5f;
        float rightWidth = 42f - gapCenterX - gapWidth * 0.5f;

        if (leftWidth > 0.5f)
        {
            CreateWall(parent, $"{name}_Left", new Vector3(-42f + leftWidth * 0.5f, 1.9f, z), new Vector3(leftWidth, 3.8f, 0.45f));
        }

        if (rightWidth > 0.5f)
        {
            CreateWall(parent, $"{name}_Right", new Vector3(gapCenterX + gapWidth * 0.5f + rightWidth * 0.5f, 1.9f, z), new Vector3(rightWidth, 3.8f, 0.45f));
        }

        CreateDecorCube(parent, $"{name}_Door_Frame", new Vector3(gapCenterX, DoorHeaderY, z), new Vector3(gapWidth + 0.8f, 0.28f, 0.62f), "M_DoorFrame", new Color(0.08f, 0.09f, 0.1f));
        CreateDecorCube(parent, $"{name}_Warning_Bar", new Vector3(gapCenterX, 0.09f, z), new Vector3(gapWidth, 0.04f, 0.22f), "M_DangerStripe", new Color(1f, 0.68f, 0.08f));
    }

    private static void CreateVerticalPartition(Transform parent, string name, float x, float zMin, float zMax, float gapCenterZ, float gapDepth)
    {
        float lowerDepth = gapCenterZ - zMin - gapDepth * 0.5f;
        float upperDepth = zMax - gapCenterZ - gapDepth * 0.5f;

        if (lowerDepth > 0.5f)
        {
            CreateWall(parent, $"{name}_Lower", new Vector3(x, 1.9f, zMin + lowerDepth * 0.5f), new Vector3(0.45f, 3.8f, lowerDepth));
        }

        if (upperDepth > 0.5f)
        {
            CreateWall(parent, $"{name}_Upper", new Vector3(x, 1.9f, gapCenterZ + gapDepth * 0.5f + upperDepth * 0.5f), new Vector3(0.45f, 3.8f, upperDepth));
        }

        CreateDecorCube(parent, $"{name}_Door_Header", new Vector3(x, DoorHeaderY, gapCenterZ), new Vector3(0.65f, 0.3f, gapDepth + 0.8f), "M_DoorFrame", new Color(0.08f, 0.09f, 0.1f));
    }

    private static void CreateCoverRun(Transform parent, float x, float z, int count, int direction)
    {
        for (int i = 0; i < count; i++)
        {
            float offset = i * 2.2f * direction;
            GameObject cover = CreateCube($"Modular_Cover_{x}_{z}_{i}", new Vector3(x + offset, 0.85f, z + i % 2 * 1.4f), new Vector3(1.8f, 1.7f, 0.55f), "M_CoverPanel", new Color(0.24f, 0.28f, 0.3f));
            cover.transform.SetParent(parent);
            CreateDecorCube(parent, $"Cover_Rim_{x}_{z}_{i}", cover.transform.position + new Vector3(0f, 0.88f, 0f), new Vector3(1.95f, 0.12f, 0.64f), "M_WallTrim", new Color(0.07f, 0.08f, 0.085f));
        }
    }

    private static void CreateControlConsole(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject consoleRoot = new GameObject(name);
        consoleRoot.transform.SetParent(parent);
        consoleRoot.transform.position = position;
        consoleRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject baseBlock = CreateCube($"{name}_Base", Vector3.zero, new Vector3(1.8f, 0.8f, 0.8f), "M_DarkMetal", new Color(0.08f, 0.09f, 0.1f));
        baseBlock.transform.SetParent(consoleRoot.transform, false);

        GameObject screen = CreateCube($"{name}_Screen", new Vector3(0f, 0.62f, 0.42f), new Vector3(1.35f, 0.48f, 0.06f), "M_Screen", new Color(0.03f, 0.42f, 0.5f));
        screen.transform.SetParent(consoleRoot.transform, false);
        Object.DestroyImmediate(screen.GetComponent<Collider>());

        CreateSmallIndicator(consoleRoot.transform, $"{name}_Indicator_Green", new Vector3(-0.55f, 0.18f, 0.43f), new Color(0.05f, 0.9f, 0.25f));
        CreateSmallIndicator(consoleRoot.transform, $"{name}_Indicator_Red", new Vector3(0.55f, 0.18f, 0.43f), new Color(0.9f, 0.08f, 0.05f));
    }

    private static void CreateSmallIndicator(Transform parent, string name, Vector3 localPosition, Color color)
    {
        GameObject lightObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightObject.name = name;
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localScale = Vector3.one * 0.12f;
        lightObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + "_Mat", color);
        Object.DestroyImmediate(lightObject.GetComponent<Collider>());
    }

    private static void CreatePipe(Transform parent, string name, Vector3 start, Vector3 end, float radius, string materialName, Color color)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length <= 0.01f)
        {
            return;
        }

        GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe.name = name;
        pipe.transform.SetParent(parent);
        pipe.transform.position = (start + end) * 0.5f;
        pipe.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        pipe.transform.localScale = new Vector3(radius, length * 0.5f, radius);
        pipe.GetComponent<Renderer>().sharedMaterial = CreateMaterial(materialName, color);
        Object.DestroyImmediate(pipe.GetComponent<Collider>());
    }

    private static bool TryGetSelectedWallMountPose(out Vector3 position, out Vector3 forward, out Transform parent)
    {
        position = Vector3.zero;
        forward = Vector3.forward;
        parent = null;

        Transform selected = Selection.activeTransform;
        if (selected == null)
        {
            return false;
        }

        Renderer selectedRenderer = selected.GetComponent<Renderer>();
        if (selectedRenderer == null)
        {
            selectedRenderer = selected.GetComponentInChildren<Renderer>();
        }

        if (selectedRenderer == null)
        {
            return false;
        }

        Bounds bounds = selectedRenderer.bounds;
        if (bounds.size.y < 1.2f)
        {
            return false;
        }

        Vector3 cameraPosition = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera.transform.position
            : bounds.center + Vector3.back * 10f;

        bool useXFace = bounds.size.x <= bounds.size.z;
        float sign = useXFace
            ? Mathf.Sign(cameraPosition.x - bounds.center.x)
            : Mathf.Sign(cameraPosition.z - bounds.center.z);

        if (Mathf.Approximately(sign, 0f))
        {
            sign = 1f;
        }

        Vector3 normal = useXFace ? new Vector3(sign, 0f, 0f) : new Vector3(0f, 0f, sign);
        float surfaceOffset = useXFace ? bounds.extents.x : bounds.extents.z;
        float mountHeight = Mathf.Clamp(bounds.min.y + 2.85f, bounds.min.y + 1.4f, bounds.max.y - 0.45f);

        position = new Vector3(bounds.center.x, mountHeight, bounds.center.z) + normal * (surfaceOffset + 0.08f);
        forward = normal;
        parent = selected;
        return true;
    }

    private static GameObject CreateCctv(Transform parent, string name, Vector3 position, Vector3 lookDirection, float distance, float angle, float detectTime, float patrolRange, float patrolSpeed, CctvDetectionTarget target, StealthGameManager gameManager)
    {
        GameObject cctvRoot = new GameObject(name);
        if (parent != null)
        {
            cctvRoot.transform.SetParent(parent);
        }

        cctvRoot.transform.position = position;
        cctvRoot.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        Transform headPivot = CreateHeadPivot(cctvRoot.transform);

        CreateCctvLocalPart(cctvRoot.transform, "Wall_Mount_Plate", new Vector3(0f, 0f, -0.08f), new Vector3(0.95f, 0.78f, 0.08f), "M_DarkMetal", new Color(0.045f, 0.05f, 0.055f));
        CreateCctvLocalPart(cctvRoot.transform, "Wall_Mount_Bevel_Top", new Vector3(0f, 0.43f, -0.03f), new Vector3(1.05f, 0.08f, 0.12f), "M_CCTV", new Color(0.08f, 0.085f, 0.09f));
        CreateCctvLocalPart(cctvRoot.transform, "Wall_Mount_Bevel_Bottom", new Vector3(0f, -0.43f, -0.03f), new Vector3(1.05f, 0.08f, 0.12f), "M_CCTV", new Color(0.08f, 0.085f, 0.09f));
        CreateCctvLocalSphere(cctvRoot.transform, "Plate_Screw_Top_Left", new Vector3(-0.33f, 0.25f, -0.015f), Vector3.one * 0.075f, "M_Screw", new Color(0.62f, 0.64f, 0.62f));
        CreateCctvLocalSphere(cctvRoot.transform, "Plate_Screw_Top_Right", new Vector3(0.33f, 0.25f, -0.015f), Vector3.one * 0.075f, "M_Screw", new Color(0.62f, 0.64f, 0.62f));
        CreateCctvLocalSphere(cctvRoot.transform, "Plate_Screw_Bottom_Left", new Vector3(-0.33f, -0.25f, -0.015f), Vector3.one * 0.075f, "M_Screw", new Color(0.62f, 0.64f, 0.62f));
        CreateCctvLocalSphere(cctvRoot.transform, "Plate_Screw_Bottom_Right", new Vector3(0.33f, -0.25f, -0.015f), Vector3.one * 0.075f, "M_Screw", new Color(0.62f, 0.64f, 0.62f));
        CreateCctvLocalPart(cctvRoot.transform, "Mount_Arm", new Vector3(0f, 0f, 0.22f), new Vector3(0.16f, 0.16f, 0.58f), "M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        CreateCctvLocalSphere(cctvRoot.transform, "Rear_Ball_Joint", new Vector3(0f, 0f, 0.52f), Vector3.one * 0.26f, "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));
        CreateCctvLocalPart(cctvRoot.transform, "Cable_Conduit", new Vector3(-0.52f, 0.18f, -0.02f), new Vector3(0.08f, 0.12f, 0.1f), "M_DarkMetal", new Color(0.025f, 0.028f, 0.03f));
        CreateCctvLocalPart(cctvRoot.transform, "Cable_Run_Up", new Vector3(-0.52f, 0.49f, -0.02f), new Vector3(0.08f, 0.52f, 0.08f), "M_DarkMetal", new Color(0.025f, 0.028f, 0.03f));

        CreateCctvLocalPart(headPivot, "Camera_Body", new Vector3(0f, 0f, 0.46f), new Vector3(0.72f, 0.38f, 0.75f), "M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        CreateCctvLocalPart(headPivot, "Camera_Hood", new Vector3(0f, 0.24f, 0.5f), new Vector3(0.88f, 0.08f, 0.86f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));
        CreateCctvLocalPart(headPivot, "Camera_Hood_Left_Lip", new Vector3(-0.48f, 0.08f, 0.52f), new Vector3(0.08f, 0.32f, 0.82f), "M_DarkMetal", new Color(0.028f, 0.032f, 0.036f));
        CreateCctvLocalPart(headPivot, "Camera_Hood_Right_Lip", new Vector3(0.48f, 0.08f, 0.52f), new Vector3(0.08f, 0.32f, 0.82f), "M_DarkMetal", new Color(0.028f, 0.032f, 0.036f));
        CreateCctvLocalPart(headPivot, "Camera_Bottom_Rail", new Vector3(0f, -0.24f, 0.46f), new Vector3(0.78f, 0.07f, 0.62f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.SetParent(headPivot, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.88f);
        lens.transform.localScale = new Vector3(0.28f, 0.28f, 0.13f);
        lens.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Lens", new Color(0.03f, 0.45f, 0.6f));
        Object.DestroyImmediate(lens.GetComponent<Collider>());

        CreateCctvLocalPart(headPivot, "Lens_Ring", new Vector3(0f, 0f, 0.82f), new Vector3(0.42f, 0.42f, 0.08f), "M_DarkMetal", new Color(0.025f, 0.028f, 0.03f));
        CreateCctvLocalSphere(headPivot, "Status_LED", new Vector3(0.29f, -0.11f, 0.88f), Vector3.one * 0.07f, "M_StatusLed", new Color(0.08f, 1f, 0.25f));
        CreateCctvLocalPart(headPivot, "Left_Hinge", new Vector3(-0.46f, 0f, 0.22f), new Vector3(0.1f, 0.28f, 0.12f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));
        CreateCctvLocalPart(headPivot, "Right_Hinge", new Vector3(0.46f, 0f, 0.22f), new Vector3(0.1f, 0.28f, 0.12f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));

        GameObject origin = new GameObject("Detection_Origin");
        origin.transform.SetParent(headPivot, false);
        origin.transform.localPosition = new Vector3(0f, 0f, 1.02f);
        origin.transform.localRotation = Quaternion.identity;

        Light spot = new GameObject("View_Light").AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = distance;
        spot.spotAngle = angle;
        spot.intensity = 2.4f;
        spot.color = new Color(1f, 0.9f, 0.55f);
        spot.transform.SetParent(origin.transform, false);

        CctvDetector detector = cctvRoot.AddComponent<CctvDetector>();
        detector.Configure(target, LayerMask.GetMask("Default"), origin.transform);
        detector.ConfigureView(distance, angle, detectTime);
        cctvRoot.AddComponent<CctvViewVisualizer>();

        if (detector.onDetected != null && gameManager != null)
        {
            UnityEventTools.AddPersistentListener(detector.onDetected, gameManager.OnPlayerDetected);
        }

        CctvPatrol patrol = cctvRoot.AddComponent<CctvPatrol>();
        patrol.Configure(patrolRange, patrolSpeed, headPivot);

        return cctvRoot;
    }

    private static GameObject CreateCctvLocalPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, string materialName, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = CreateMaterial(materialName, color);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static GameObject CreateCctvLocalSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, string materialName, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = CreateMaterial(materialName, color);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static Transform CreateHeadPivot(Transform cctvRoot)
    {
        GameObject pivotObject = new GameObject(CctvHeadPivotName);
        Transform pivot = pivotObject.transform;
        pivot.SetParent(cctvRoot, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        return pivot;
    }

    private static Transform EnsureCctvHeadPivot(CctvDetector detector)
    {
        Transform cctvRoot = detector.transform;
        Transform pivot = cctvRoot.Find(CctvHeadPivotName);
        if (pivot == null)
        {
            pivot = cctvRoot.Find(LegacyCctvViewPivotName);
        }

        if (pivot == null)
        {
            pivot = cctvRoot.Find(LegacyCctvYawPivotName);
        }

        if (pivot == null)
        {
            pivot = CreateHeadPivot(cctvRoot);
        }
        else
        {
            pivot.name = CctvHeadPivotName;
        }

        MoveChildToHead(cctvRoot, pivot, "Camera_Body");
        MoveChildToHead(cctvRoot, pivot, "Lens");
        MoveDetectionOriginToHead(detector, pivot);
        RemoveEmptyLegacyPivots(cctvRoot, pivot);
        return pivot;
    }

    private static Transform FindCctvChild(Transform cctvRoot, string childName)
    {
        Transform direct = cctvRoot.Find(childName);
        if (direct != null)
        {
            return direct;
        }

        Transform headPivot = cctvRoot.Find(CctvHeadPivotName);
        Transform headChild = headPivot != null ? headPivot.Find(childName) : null;
        if (headChild != null)
        {
            return headChild;
        }

        Transform viewPivot = cctvRoot.Find(LegacyCctvViewPivotName);
        Transform viewChild = viewPivot != null ? viewPivot.Find(childName) : null;
        if (viewChild != null)
        {
            return viewChild;
        }

        Transform legacyPivot = cctvRoot.Find(LegacyCctvYawPivotName);
        return legacyPivot != null ? legacyPivot.Find(childName) : null;
    }

    private static void MoveChildToHead(Transform cctvRoot, Transform headPivot, string childName)
    {
        Transform child = FindCctvChild(cctvRoot, childName);
        if (child == null || child.parent == headPivot)
        {
            return;
        }

        child.SetParent(headPivot, true);
        EditorUtility.SetDirty(child);
    }

    private static void MoveDetectionOriginToHead(CctvDetector detector, Transform headPivot)
    {
        Transform origin = FindCctvChild(detector.transform, "Detection_Origin");
        if (origin == null)
        {
            origin = new GameObject("Detection_Origin").transform;
        }

        origin.SetParent(headPivot, true);
        detector.SetDetectionOrigin(origin);
        EditorUtility.SetDirty(origin);
    }

    private static void RemoveEmptyLegacyPivots(Transform cctvRoot, Transform headPivot)
    {
        Transform viewPivot = cctvRoot.Find(LegacyCctvViewPivotName);
        if (viewPivot != null && viewPivot != headPivot && viewPivot.childCount == 0)
        {
            Object.DestroyImmediate(viewPivot.gameObject);
        }

        Transform legacyPivot = cctvRoot.Find(LegacyCctvYawPivotName);
        if (legacyPivot != null && legacyPivot != headPivot && legacyPivot.childCount == 0)
        {
            Object.DestroyImmediate(legacyPivot.gameObject);
        }
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        float wallHeight = Mathf.Max(scale.y, SecurityWallHeight);
        position.y = wallHeight * 0.5f;
        scale.y = wallHeight;

        GameObject wall = CreateCube(name, position, scale, "M_Wall", new Color(0.55f, 0.55f, 0.55f));
        wall.transform.SetParent(parent);
    }

    private static void CreatePillar(Transform parent, string name, Vector3 position)
    {
        GameObject pillar = CreateCube(name, position, new Vector3(1.5f, 2.5f, 1.5f), "M_Pillar", new Color(0.38f, 0.41f, 0.43f));
        pillar.transform.SetParent(parent);
    }

    private static void CreateCrateStack(Transform parent, Vector3 basePosition, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float xOffset = (i % 2) * 1.05f;
            float zOffset = (i / 2) * 1.05f;
            Vector3 position = basePosition + new Vector3(xOffset, i >= 2 ? 1f : 0f, zOffset);
            GameObject crate = CreateCube($"Crate_{basePosition.x}_{basePosition.z}_{i}", position, Vector3.one, "M_Crate", new Color(0.42f, 0.32f, 0.2f));
            crate.transform.SetParent(parent);
        }
    }

    private static void CreateFloorStripe(Transform parent, string name, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject stripe = CreateCube(name, position, scale, materialName, color);
        stripe.transform.SetParent(parent);
        Object.DestroyImmediate(stripe.GetComponent<Collider>());
    }

    private static GameObject CreateDecorCube(Transform parent, string name, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject cube = CreateCube(name, position, scale, materialName, color);
        cube.transform.SetParent(parent, false);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject CreateBlockingDecorCube(Transform parent, string name, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject cube = CreateCube(name, position, scale, materialName, color);
        cube.transform.SetParent(parent, false);
        EnsureBoxCollider(cube);
        return cube;
    }

    private static GameObject InstantiateDetailPrefab(Transform parent, string assetPath, string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool blocksCctv, string fallbackMaterialName = null, Color fallbackColor = default, bool replaceAllMaterials = true)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
        }

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(eulerAngles);
        instance.transform.localScale = scale;

        if (!string.IsNullOrEmpty(fallbackMaterialName) && (replaceAllMaterials || ForceSafeImportedMaterials))
        {
            ReplacePrefabMaterials(instance.transform, fallbackMaterialName, fallbackColor);
        }
        else if (!string.IsNullOrEmpty(fallbackMaterialName))
        {
            RepairBrokenPrefabMaterials(instance.transform, fallbackMaterialName, fallbackColor);
        }

        if (blocksCctv)
        {
            EnsureBoxCollidersForRenderers(instance.transform);
        }

        return instance;
    }

    private static void RepairBrokenPrefabMaterials(Transform root, string materialName, Color color)
    {
        Material fallback = null;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterials.Length == 0)
            {
                continue;
            }

            Material[] repaired = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < repaired.Length; i++)
            {
                if (!IsBrokenMaterial(repaired[i]))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = CreateMaterial(materialName, color);
                }
                repaired[i] = fallback;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = repaired;
            }
        }
    }

    private static bool IsBrokenMaterial(Material material)
    {
        if (material == null || material.shader == null)
        {
            return true;
        }

        if (material.shader.name == "Hidden/InternalErrorShader")
        {
            return true;
        }

        return ShouldReplaceMaterialShader(material.shader);
    }

    private static void ReplacePrefabMaterials(Transform root, string materialName, Color color)
    {
        Material material = CreateMaterial(materialName, color);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] replacement = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < replacement.Length; i++)
            {
                replacement[i] = material;
            }

            renderer.sharedMaterials = replacement;
        }
    }

    private static int ReplaceImportedSceneMaterials()
    {
#if UNITY_2023_1_OR_NEWER
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);
#endif
        int rendererCount = 0;
        foreach (Renderer renderer in renderers)
        {
            Transform importedRoot = FindImportedAssetRoot(renderer.transform);
            if (importedRoot == null)
            {
                continue;
            }

            if (!TryGetImportedFallbackMaterial(importedRoot.name, out string materialName, out Color color))
            {
                continue;
            }

            ApplyMaterialToRenderer(renderer, CreateMaterial(materialName, color));
            EditorUtility.SetDirty(renderer);
            rendererCount++;
        }

        return rendererCount;
    }

    private static Transform FindImportedAssetRoot(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name.StartsWith("Asset_") || current.name.StartsWith("HQ_"))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static bool TryGetImportedFallbackMaterial(string objectName, out string materialName, out Color color)
    {
        if (objectName.Contains("Plant") || objectName.Contains("Garden") || objectName.Contains("Hydrangea") || objectName.Contains("Tulip") || objectName.Contains("Grass") || objectName.Contains("Mint"))
        {
            materialName = "M_PlantLeaves";
            color = new Color(0.08f, 0.34f, 0.15f);
            return true;
        }

        if (objectName.Contains("Road") || objectName.Contains("Runway") || objectName.Contains("Pavement") || objectName.Contains("Concrete"))
        {
            materialName = "M_ImportedConcrete";
            color = new Color(0.42f, 0.42f, 0.4f);
            return true;
        }

        if (objectName.Contains("Jet") || objectName.Contains("Helicopter") || objectName.Contains("Aircraft") || objectName.Contains("Airfield") || objectName.Contains("Metal"))
        {
            materialName = "M_ImportedMetal";
            color = new Color(0.33f, 0.35f, 0.36f);
            return true;
        }

        if (objectName.Contains("Light") || objectName.Contains("Lamp"))
        {
            materialName = "M_Lamp";
            color = new Color(1f, 0.83f, 0.48f);
            return true;
        }

        if (objectName.Contains("Sofa") || objectName.Contains("Desk") || objectName.Contains("Kitchen") || objectName.Contains("Bed") || objectName.Contains("Bathroom") || objectName.Contains("Wardrobe") || objectName.Contains("Storage") || objectName.Contains("Shelf") || objectName.Contains("Table"))
        {
            materialName = "M_ImportedWood";
            color = new Color(0.22f, 0.16f, 0.1f);
            return true;
        }

        if (objectName.Contains("Fountain") || objectName.Contains("Gate") || objectName.Contains("Barrier"))
        {
            materialName = "M_ImportedStone";
            color = new Color(0.47f, 0.44f, 0.38f);
            return true;
        }

        if (objectName.Contains("Roof"))
        {
            materialName = "M_ImportedRoof";
            color = new Color(0.13f, 0.13f, 0.14f);
            return true;
        }

        if (objectName.Contains("Hangar") && !objectName.Contains("Floor"))
        {
            materialName = "M_ImportedMetal";
            color = new Color(0.33f, 0.35f, 0.36f);
            return true;
        }

        if (objectName.Contains("Floor"))
        {
            materialName = "M_ImportedConcrete";
            color = new Color(0.42f, 0.42f, 0.4f);
            return true;
        }

        if (objectName.Contains("Door"))
        {
            materialName = "M_ImportedDoor";
            color = new Color(0.18f, 0.11f, 0.06f);
            return true;
        }

        if (objectName.Contains("Shutter"))
        {
            materialName = "M_ImportedWood";
            color = new Color(0.22f, 0.16f, 0.1f);
            return true;
        }

        if (objectName.Contains("Stair") || objectName.Contains("Pillar") || objectName.Contains("Chimney"))
        {
            materialName = "M_ImportedStone";
            color = new Color(0.47f, 0.44f, 0.38f);
            return true;
        }

        materialName = "M_ImportedTrim";
        color = new Color(0.55f, 0.48f, 0.38f);
        return true;
    }

    private static void ApplyMaterialToRenderer(Renderer renderer, Material material)
    {
        Material[] replacement = new Material[renderer.sharedMaterials.Length];
        for (int i = 0; i < replacement.Length; i++)
        {
            replacement[i] = material;
        }

        renderer.sharedMaterials = replacement;
    }

    private static int EnsureBoxCollidersForRenderers(Transform root)
    {
        int colliderCount = 0;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponent<Collider>() != null)
            {
                continue;
            }

            EnsureBoxCollider(renderer.gameObject);
            colliderCount++;
        }

        return colliderCount;
    }

    private static int EnsureBlockingCollidersForArchitecture(Transform scope)
    {
        Renderer[] renderers;
        if (scope != null)
        {
            renderers = scope.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            renderers = Object.FindObjectsOfType<Renderer>(true);
#endif
        }

        int colliderCount = 0;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !ShouldHaveCctvBlockingCollider(renderer.gameObject.name))
            {
                continue;
            }

            EnsureBoxCollider(renderer.gameObject);
            colliderCount++;
        }

        return colliderCount;
    }

    private static BoxCollider EnsureBoxCollider(GameObject gameObject)
    {
        BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        return boxCollider;
    }

    private static bool ShouldHaveCctvBlockingCollider(string objectName)
    {
        if (ContainsToken(objectName, "Door_Leaf") ||
            ContainsToken(objectName, "Threshold") ||
            ContainsToken(objectName, "Window") ||
            ContainsToken(objectName, "Water") ||
            ContainsToken(objectName, "Route_Line") ||
            ContainsToken(objectName, "Stripe") ||
            ContainsToken(objectName, "Floor_Seam") ||
            ContainsToken(objectName, "Start_Bay") ||
            ContainsToken(objectName, "Goal_Bay"))
        {
            return false;
        }

        string[] blockingTokens =
        {
            "Wall",
            "Facade",
            "Roof",
            "Pillar",
            "Arch",
            "Gate",
            "Corner",
            "_Cap",
            "_Block",
            "_Return",
            "Tower",
            "Sealed_Joint",
            "Door_Frame",
            "Door_Header",
            "Building_North",
            "Building_South",
            "Building_East",
            "Building_West"
        };

        foreach (string token in blockingTokens)
        {
            if (ContainsToken(objectName, token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsToken(string value, string token)
    {
        return value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void CreateOverheadLight(Transform parent, Vector3 position)
    {
        GameObject lamp = CreateCube($"Lamp_{position.z}", position + Vector3.down * 0.15f, new Vector3(3.5f, 0.12f, 0.45f), "M_Lamp", new Color(0.9f, 0.88f, 0.72f));
        lamp.transform.SetParent(parent);
        Object.DestroyImmediate(lamp.GetComponent<Collider>());

        Light light = new GameObject($"Area_Light_{position.z}").AddComponent<Light>();
        light.transform.SetParent(parent);
        light.transform.position = position;
        light.type = LightType.Point;
        light.range = 11f;
        light.intensity = 2.1f;
        light.color = new Color(1f, 0.92f, 0.72f);
    }

    private static void AttachMainCameraToPlayer(Transform player)
    {
        Camera mainCamera = GetOrCreateMainCamera();
        mainCamera.transform.SetParent(player, false);
        mainCamera.transform.localPosition = new Vector3(0f, 0.75f, 0.08f);
        mainCamera.transform.localRotation = Quaternion.identity;
        mainCamera.fieldOfView = 70f;
        mainCamera.nearClipPlane = 0.05f;
        mainCamera.targetDisplay = 0;
        mainCamera.cullingMask = ~0;
        mainCamera.enabled = true;
        mainCamera.gameObject.SetActive(true);
    }

    private static Camera GetOrCreateMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
            }

            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = cameraObject.AddComponent<Camera>();
            }
        }

        if (mainCamera.GetComponent<AudioListener>() == null && FindExistingAudioListener() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        return mainCamera;
    }

    private static AudioListener FindExistingAudioListener()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<AudioListener>();
#else
        return Object.FindObjectOfType<AudioListener>();
#endif
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = CreateMaterial(materialName, color);
        return cube;
    }

    private static void CreateGameUi(out Text statusText, out Text hintText)
    {
        GameObject canvasObject = new GameObject("Stealth_Game_UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        statusText = CreateUiText(canvasObject.transform, "Status_Text", "목적지까지 들키지 말고 이동", 34, new Vector2(0f, -34f), new Color(1f, 0.95f, 0.72f));
        hintText = CreateUiText(canvasObject.transform, "Hint_Text", "WASD 이동 | 마우스 시점 | 초록 구역에 도착", 21, new Vector2(0f, -78f), Color.white);
    }

    private static Text CreateUiText(Transform parent, string name, string textValue, int size, Vector2 anchoredPosition, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = textValue;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = CreateUiFont();
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(900f, 44f);

        return text;
    }

    private static Text CreateDetectionUi()
    {
        GameObject canvasObject = new GameObject("Detection_UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("Detection_Text");
        textObject.transform.SetParent(canvasObject.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.text = "감지됨";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = CreateUiFont();
        text.fontSize = 54;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.12f, 0.08f);
        text.gameObject.SetActive(false);

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -48f);
        rect.sizeDelta = new Vector2(420f, 90f);

        Selection.activeGameObject = textObject;
        return text;
    }

    private static Font CreateUiFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 54);
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        const string folderPath = "Assets/CCTVStarter";
        string assetPath = $"{folderPath}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = GetSafeSurfaceShader();

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader" || ShouldReplaceMaterialShader(material.shader))
        {
            material.shader = shader;
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader GetSafeSurfaceShader()
    {
        Shader shader = null;
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
        }

        return shader;
    }

    private static bool ShouldReplaceMaterialShader(Shader shader)
    {
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            return false;
        }

        return shader.name.Contains("Universal Render Pipeline") || shader.name.Contains("HDRenderPipeline");
    }
}
