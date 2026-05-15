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

    [MenuItem("Tools/CCTV Starter/Create Stealth Mini Game")]
    public static void CreateStealthMiniGame()
    {
        CleanupGeneratedObjects();

        GameObject root = new GameObject("StealthMiniGame");

        GameObject floor = CreateCube("Security_Complex_Floor", new Vector3(0f, -0.05f, 0f), new Vector3(84f, 0.1f, 156f), "M_Floor", new Color(0.13f, 0.15f, 0.16f));
        floor.transform.SetParent(root.transform);

        BuildSecurityComplex(root.transform);

        Vector3 startPosition = new Vector3(0f, 1f, -72f);
        GameObject player = CreatePlayer(root.transform, startPosition);

        Text statusText;
        Text hintText;
        CreateGameUi(out statusText, out hintText);

        GameObject gameManagerObject = new GameObject("Stealth_Game_Manager");
        gameManagerObject.transform.SetParent(root.transform);
        StealthGameManager gameManager = gameManagerObject.AddComponent<StealthGameManager>();
        gameManager.Configure(player.transform, player.GetComponent<SimplePlayerController>(), statusText, hintText, startPosition);

        CreateGoal(root.transform, gameManager, new Vector3(0f, 0.08f, 73.5f), new Vector3(14f, 0.16f, 5f));

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
        CreatePerimeterWalls(parent);
        CreateFloorPanelGrid(parent);
        CreateInteriorSecurityLayout(parent);
        CreateIndustrialProps(parent);
        CreateCeilingAndLighting(parent);
        CreateEnvironmentalStoryDetails(parent);
    }

    private static void CreatePerimeterWalls(Transform parent)
    {
        CreateWall(parent, "Outer_Wall_North", new Vector3(0f, 2.25f, 78f), new Vector3(84f, 4.5f, 0.55f));
        CreateWall(parent, "Outer_Wall_South", new Vector3(0f, 2.25f, -78f), new Vector3(84f, 4.5f, 0.55f));
        CreateWall(parent, "Outer_Wall_West", new Vector3(-42f, 2.25f, 0f), new Vector3(0.55f, 4.5f, 156f));
        CreateWall(parent, "Outer_Wall_East", new Vector3(42f, 2.25f, 0f), new Vector3(0.55f, 4.5f, 156f));

        CreateDecorCube(parent, "North_Top_Service_Rail", new Vector3(0f, 4.72f, 77.55f), new Vector3(82f, 0.18f, 0.28f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "South_Top_Service_Rail", new Vector3(0f, 4.72f, -77.55f), new Vector3(82f, 0.18f, 0.28f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "West_Top_Service_Rail", new Vector3(-41.55f, 4.72f, 0f), new Vector3(0.28f, 0.18f, 154f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
        CreateDecorCube(parent, "East_Top_Service_Rail", new Vector3(41.55f, 4.72f, 0f), new Vector3(0.28f, 0.18f, 154f), "M_WallTrim", new Color(0.11f, 0.12f, 0.13f));
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
            CreateDecorCube(parent, $"Ceiling_Beam_{z}", new Vector3(0f, 4.9f, z), new Vector3(82f, 0.18f, 0.32f), "M_CeilingBeam", new Color(0.09f, 0.1f, 0.105f));
        }

        for (float z = -66f; z <= 66f; z += 18f)
        {
            CreateOverheadLight(parent, new Vector3(-18f, 5.15f, z));
            CreateOverheadLight(parent, new Vector3(18f, 5.15f, z + 9f));
        }

        for (float z = -65f; z <= 65f; z += 26f)
        {
            CreatePipe(parent, $"Red_Service_Pipe_{z}", new Vector3(-40.8f, 3.25f, z), new Vector3(-40.8f, 3.25f, z + 18f), 0.08f, "M_RedPipe", new Color(0.65f, 0.08f, 0.06f));
            CreatePipe(parent, $"Blue_Service_Pipe_{z}", new Vector3(40.8f, 3.05f, z), new Vector3(40.8f, 3.05f, z + 18f), 0.07f, "M_BluePipe", new Color(0.05f, 0.24f, 0.72f));
        }
    }

    private static void CreateEnvironmentalStoryDetails(Transform parent)
    {
        CreateDecorCube(parent, "Loading_Dock_Door", new Vector3(0f, 2.1f, -77.35f), new Vector3(16f, 3.2f, 0.18f), "M_Door", new Color(0.18f, 0.2f, 0.21f));
        CreateDecorCube(parent, "Final_Vault_Door", new Vector3(0f, 2.1f, 77.35f), new Vector3(14f, 3.2f, 0.18f), "M_GoalDoor", new Color(0.11f, 0.24f, 0.18f));

        for (float z = -61f; z <= 61f; z += 24f)
        {
            CreateDecorCube(parent, $"Warning_Sign_W_{z}", new Vector3(-41.68f, 2.6f, z), new Vector3(0.04f, 0.7f, 2.2f), "M_SignYellow", new Color(1f, 0.75f, 0.08f));
            CreateDecorCube(parent, $"Warning_Sign_E_{z}", new Vector3(41.68f, 2.6f, z + 10f), new Vector3(0.04f, 0.7f, 2.2f), "M_SignRed", new Color(0.85f, 0.1f, 0.08f));
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

        CreateDecorCube(parent, $"{name}_Door_Frame", new Vector3(gapCenterX, 2.55f, z), new Vector3(gapWidth + 0.8f, 0.28f, 0.62f), "M_DoorFrame", new Color(0.08f, 0.09f, 0.1f));
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

        CreateDecorCube(parent, $"{name}_Door_Header", new Vector3(x, 2.65f, gapCenterZ), new Vector3(0.65f, 0.3f, gapDepth + 0.8f), "M_DoorFrame", new Color(0.08f, 0.09f, 0.1f));
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
        CreateCctvLocalPart(cctvRoot.transform, "Mount_Arm", new Vector3(0f, 0f, 0.22f), new Vector3(0.16f, 0.16f, 0.58f), "M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        CreateCctvLocalPart(cctvRoot.transform, "Cable_Conduit", new Vector3(-0.52f, 0.18f, -0.02f), new Vector3(0.08f, 0.12f, 0.1f), "M_DarkMetal", new Color(0.025f, 0.028f, 0.03f));

        CreateCctvLocalPart(headPivot, "Camera_Body", new Vector3(0f, 0f, 0.46f), new Vector3(0.72f, 0.38f, 0.75f), "M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        CreateCctvLocalPart(headPivot, "Camera_Hood", new Vector3(0f, 0.24f, 0.5f), new Vector3(0.88f, 0.08f, 0.86f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));
        CreateCctvLocalPart(headPivot, "Camera_Bottom_Rail", new Vector3(0f, -0.24f, 0.46f), new Vector3(0.78f, 0.07f, 0.62f), "M_DarkMetal", new Color(0.035f, 0.04f, 0.045f));

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.SetParent(headPivot, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.88f);
        lens.transform.localScale = new Vector3(0.28f, 0.28f, 0.13f);
        lens.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Lens", new Color(0.03f, 0.45f, 0.6f));
        Object.DestroyImmediate(lens.GetComponent<Collider>());

        CreateCctvLocalPart(headPivot, "Lens_Ring", new Vector3(0f, 0f, 0.82f), new Vector3(0.42f, 0.42f, 0.08f), "M_DarkMetal", new Color(0.025f, 0.028f, 0.03f));
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
        cube.transform.SetParent(parent);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
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

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}
