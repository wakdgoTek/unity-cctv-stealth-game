using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(CctvDetector))]
public class CctvViewVisualizer : MonoBehaviour
{
    [SerializeField] private Color viewColor = new Color(1f, 0.08f, 0.02f, 0.13f);
    [SerializeField] private float coneVerticalAngle = 24f;
    [SerializeField] private int segments = 32;

    private CctvDetector detector;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Transform visualTransform;
    private Material material;
    private bool visualReady;

    private void Awake()
    {
        detector = GetComponent<CctvDetector>();
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RefreshVisual();
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            RefreshVisual();
        }
    }

    private void OnValidate()
    {
        segments = Mathf.Clamp(segments, 8, 96);
    }

    private void RefreshVisual()
    {
        if (detector == null)
        {
            detector = GetComponent<CctvDetector>();
        }

        if (detector == null)
        {
            return;
        }

        EnsureVisualObjects();
        ApplyVisualTransform();
        RebuildMesh();
    }

    private void ApplyVisualTransform()
    {
        Transform origin = detector.DetectionOrigin;
        visualTransform.position = origin.position;
        visualTransform.rotation = origin.rotation;
    }

    private void EnsureVisualObjects()
    {
        Transform existing = transform.Find("Visible_Red_Detection_Range");
        GameObject visualObject;
        if (existing != null)
        {
            visualObject = existing.gameObject;
        }
        else
        {
            visualObject = new GameObject("Visible_Red_Detection_Range");
            visualObject.transform.SetParent(transform, false);
        }

        visualObject.transform.localScale = Vector3.one;
        visualTransform = visualObject.transform;

        meshFilter = visualObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = visualObject.AddComponent<MeshFilter>();
        }

        meshRenderer = visualObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = visualObject.AddComponent<MeshRenderer>();
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "CCTV Visible Detection Range";
            meshFilter.sharedMesh = mesh;
        }

        material = meshRenderer.sharedMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            meshRenderer.sharedMaterial = material;
        }

        Color effectiveViewColor = viewColor;
        effectiveViewColor.a = Mathf.Min(effectiveViewColor.a, 0.13f);
        material.color = effectiveViewColor;
        SetMaterialColorIfAvailable("_BaseColor", effectiveViewColor);
        SetMaterialColorIfAvailable("_Color", effectiveViewColor);
        SetMaterialFloatIfAvailable("_Surface", 1f);
        SetMaterialFloatIfAvailable("_Blend", 0f);
        SetMaterialFloatIfAvailable("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetMaterialFloatIfAvailable("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetMaterialFloatIfAvailable("_ZWrite", 0f);
        SetMaterialFloatIfAvailable("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        SetMaterialFloatIfAvailable("_AlphaClip", 0f);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        visualReady = true;
    }

    private void RebuildMesh()
    {
        if (detector == null)
        {
            detector = GetComponent<CctvDetector>();
        }

        if (!visualReady || mesh == null)
        {
            EnsureVisualObjects();
        }

        float distance = detector.ViewDistance;
        float halfAngle = detector.ViewAngle * 0.5f;
        float halfVerticalAngle = coneVerticalAngle * 0.5f;
        int arcVertexCount = segments + 1;
        int vertexCount = 1 + arcVertexCount * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 12];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            float radians = angle * Mathf.Deg2Rad;
            float visibleDistance = GetVisibleDistance(angle, distance);
            float coneHalfHeight = Mathf.Tan(halfVerticalAngle * Mathf.Deg2Rad) * visibleDistance;
            Vector3 flatPoint = new Vector3(Mathf.Sin(radians) * visibleDistance, 0f, Mathf.Cos(radians) * visibleDistance);
            vertices[1 + i] = flatPoint + Vector3.up * coneHalfHeight;
            vertices[1 + arcVertexCount + i] = flatPoint + Vector3.down * coneHalfHeight;
        }

        for (int i = 0; i < segments; i++)
        {
            int topA = 1 + i;
            int topB = 1 + i + 1;
            int bottomA = 1 + arcVertexCount + i;
            int bottomB = 1 + arcVertexCount + i + 1;
            int triangleIndex = i * 12;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = topA;
            triangles[triangleIndex + 2] = topB;

            triangles[triangleIndex + 3] = 0;
            triangles[triangleIndex + 4] = bottomB;
            triangles[triangleIndex + 5] = bottomA;

            triangles[triangleIndex + 6] = topA;
            triangles[triangleIndex + 7] = bottomA;
            triangles[triangleIndex + 8] = topB;

            triangles[triangleIndex + 9] = topB;
            triangles[triangleIndex + 10] = bottomA;
            triangles[triangleIndex + 11] = bottomB;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private void SetMaterialFloatIfAvailable(string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private void SetMaterialColorIfAvailable(string propertyName, Color value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private float GetVisibleDistance(float localYawAngle, float maxDistance)
    {
        Transform origin = detector.DetectionOrigin;
        Vector3 direction = (origin.rotation * Quaternion.AngleAxis(localYawAngle, Vector3.up)) * Vector3.forward;
        Vector3 rayOrigin = origin.position;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, maxDistance, detector.ObstacleMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = maxDistance;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.transform.GetComponentInParent<CctvDetectionTarget>() != null)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, Vector3.Distance(rayOrigin, hit.point));
        }

        return nearestDistance;
    }
}
