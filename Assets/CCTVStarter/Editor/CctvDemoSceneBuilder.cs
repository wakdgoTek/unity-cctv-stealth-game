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

        GameObject floor = CreateCube("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(30f, 0.1f, 58f), "M_Floor", new Color(0.18f, 0.21f, 0.23f));
        floor.transform.SetParent(root.transform);

        CreateWall(root.transform, "Back_Wall", new Vector3(0f, 1.5f, -29f), new Vector3(30f, 3f, 0.45f));
        CreateWall(root.transform, "Front_Wall", new Vector3(0f, 1.5f, 29f), new Vector3(30f, 3f, 0.45f));
        CreateWall(root.transform, "Left_Wall", new Vector3(-15f, 1.5f, 0f), new Vector3(0.45f, 3f, 58f));
        CreateWall(root.transform, "Right_Wall", new Vector3(15f, 1.5f, 0f), new Vector3(0.45f, 3f, 58f));

        CreateWall(root.transform, "Gate_Wall_A_Left", new Vector3(-8.2f, 1.5f, -16f), new Vector3(13.6f, 3f, 0.45f));
        CreateWall(root.transform, "Gate_Wall_A_Right", new Vector3(8.2f, 1.5f, -16f), new Vector3(7.6f, 3f, 0.45f));
        CreateWall(root.transform, "Gate_Wall_B_Left", new Vector3(-8.2f, 1.5f, 0f), new Vector3(7.6f, 3f, 0.45f));
        CreateWall(root.transform, "Gate_Wall_B_Right", new Vector3(8.2f, 1.5f, 0f), new Vector3(13.6f, 3f, 0.45f));
        CreateWall(root.transform, "Gate_Wall_C_Left", new Vector3(-8.2f, 1.5f, 15.5f), new Vector3(13.6f, 3f, 0.45f));
        CreateWall(root.transform, "Gate_Wall_C_Right", new Vector3(8.2f, 1.5f, 15.5f), new Vector3(7.6f, 3f, 0.45f));

        CreateWall(root.transform, "Cover_A", new Vector3(-5.4f, 1.25f, -23f), new Vector3(7.5f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_B", new Vector3(6.2f, 1.25f, -20.3f), new Vector3(5.8f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_C", new Vector3(3.5f, 1.25f, -10.4f), new Vector3(8.4f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_D", new Vector3(-6.5f, 1.25f, -6.7f), new Vector3(5.2f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_E", new Vector3(-3.5f, 1.25f, 5.2f), new Vector3(8f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_F", new Vector3(6.7f, 1.25f, 8.6f), new Vector3(5.5f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_G", new Vector3(2.5f, 1.25f, 21f), new Vector3(9f, 2.5f, 0.45f));
        CreateWall(root.transform, "Cover_H", new Vector3(-7.8f, 1.25f, 23.5f), new Vector3(4.8f, 2.5f, 0.45f));

        CreatePillar(root.transform, "Pillar_A", new Vector3(-11f, 1.25f, -19f));
        CreatePillar(root.transform, "Pillar_B", new Vector3(11f, 1.25f, -11f));
        CreatePillar(root.transform, "Pillar_C", new Vector3(-11f, 1.25f, 4f));
        CreatePillar(root.transform, "Pillar_D", new Vector3(11f, 1.25f, 18f));

        CreateCrateStack(root.transform, new Vector3(-10.6f, 0.55f, -25f), 3);
        CreateCrateStack(root.transform, new Vector3(10.2f, 0.55f, -14f), 2);
        CreateCrateStack(root.transform, new Vector3(-10.5f, 0.55f, -2.5f), 4);
        CreateCrateStack(root.transform, new Vector3(10.5f, 0.55f, 3.4f), 3);
        CreateCrateStack(root.transform, new Vector3(-10.8f, 0.55f, 13f), 2);
        CreateCrateStack(root.transform, new Vector3(10.8f, 0.55f, 24f), 4);

        CreateFloorStripe(root.transform, "Start_Stripe", new Vector3(0f, 0.01f, -26.2f), new Vector3(8f, 0.03f, 0.18f), "M_StartStripe", new Color(0.1f, 0.55f, 0.9f));
        CreateFloorStripe(root.transform, "Danger_Stripe_A", new Vector3(0f, 0.012f, -16f), new Vector3(4.2f, 0.03f, 0.16f), "M_DangerStripe", new Color(1f, 0.72f, 0.1f));
        CreateFloorStripe(root.transform, "Danger_Stripe_B", new Vector3(0f, 0.012f, 0f), new Vector3(4.2f, 0.03f, 0.16f), "M_DangerStripe", new Color(1f, 0.72f, 0.1f));
        CreateFloorStripe(root.transform, "Danger_Stripe_C", new Vector3(0f, 0.012f, 15.5f), new Vector3(4.2f, 0.03f, 0.16f), "M_DangerStripe", new Color(1f, 0.72f, 0.1f));

        CreateOverheadLight(root.transform, new Vector3(0f, 5f, -22f));
        CreateOverheadLight(root.transform, new Vector3(0f, 5f, -7f));
        CreateOverheadLight(root.transform, new Vector3(0f, 5f, 8f));
        CreateOverheadLight(root.transform, new Vector3(0f, 5f, 23f));

        Vector3 startPosition = new Vector3(0f, 1f, -26.2f);
        GameObject player = CreatePlayer(root.transform, startPosition);

        Text statusText;
        Text hintText;
        CreateGameUi(out statusText, out hintText);

        GameObject gameManagerObject = new GameObject("Stealth_Game_Manager");
        gameManagerObject.transform.SetParent(root.transform);
        StealthGameManager gameManager = gameManagerObject.AddComponent<StealthGameManager>();
        gameManager.Configure(player.transform, player.GetComponent<SimplePlayerController>(), statusText, hintText, startPosition);

        CreateGoal(root.transform, gameManager);

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

        if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.pivot + Vector3.up * 2f;
            lookDirection = Vector3.ProjectOnPlane(SceneView.lastActiveSceneView.camera.transform.forward, Vector3.up);
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                lookDirection = Vector3.forward;
            }
        }

        GameObject cctv = CreateCctv(null, "Placeable_CCTV", position, lookDirection, 12f, 65f, 0.5f, 80f, 35f, target, gameManager);
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

    private static void CreateGoal(Transform parent, StealthGameManager gameManager)
    {
        GameObject goal = CreateCube("Goal_Zone", new Vector3(0f, 0.08f, 27f), new Vector3(7f, 0.16f, 2.8f), "M_Goal", new Color(0.1f, 0.85f, 0.35f));
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

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Camera_Body";
        body.transform.SetParent(headPivot, false);
        body.transform.localScale = new Vector3(0.75f, 0.34f, 0.55f);
        body.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_CCTV", new Color(0.08f, 0.09f, 0.1f));
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.SetParent(headPivot, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        lens.transform.localScale = Vector3.one * 0.24f;
        lens.GetComponent<Renderer>().sharedMaterial = CreateMaterial("M_Lens", new Color(0.05f, 0.45f, 0.65f));
        Object.DestroyImmediate(lens.GetComponent<Collider>());

        GameObject origin = new GameObject("Detection_Origin");
        origin.transform.SetParent(headPivot, false);
        origin.transform.localPosition = new Vector3(0f, 0f, 0.5f);
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
