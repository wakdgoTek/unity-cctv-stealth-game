using UnityEngine;

[RequireComponent(typeof(CctvDetector))]
public class CctvViewVisualizer : MonoBehaviour
{
    [SerializeField] private Color viewColor = new Color(1f, 0f, 0f, 0.28f);
    [SerializeField] private float groundOffset = 0.03f;
    [SerializeField] private float wallCheckHeight = 1.1f;
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
        EnsureVisualObjects();
        RebuildMesh();
    }

    private void LateUpdate()
    {
        if (detector == null)
        {
            return;
        }

        if (visualTransform == null)
        {
            EnsureVisualObjects();
        }

        Transform origin = detector.DetectionOrigin;
        visualTransform.position = new Vector3(origin.position.x, groundOffset, origin.position.z);
        visualTransform.rotation = Quaternion.Euler(0f, origin.eulerAngles.y, 0f);
        RebuildMesh();
    }

    private void OnValidate()
    {
        segments = Mathf.Clamp(segments, 8, 96);
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

        material.color = viewColor;
        SetMaterialFloatIfAvailable("_Surface", 1f);
        SetMaterialFloatIfAvailable("_Blend", 0f);
        SetMaterialFloatIfAvailable("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetMaterialFloatIfAvailable("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetMaterialFloatIfAvailable("_ZWrite", 0f);
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
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            float radians = angle * Mathf.Deg2Rad;
            float visibleDistance = GetVisibleDistance(angle, distance);
            vertices[i + 1] = new Vector3(Mathf.Sin(radians) * visibleDistance, 0f, Mathf.Cos(radians) * visibleDistance);
        }

        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
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

    private float GetVisibleDistance(float localYawAngle, float maxDistance)
    {
        Transform origin = detector.DetectionOrigin;
        Vector3 flatForward = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = transform.forward;
        }

        Vector3 direction = Quaternion.AngleAxis(localYawAngle, Vector3.up) * flatForward;
        Vector3 rayOrigin = new Vector3(origin.position.x, wallCheckHeight, origin.position.z);
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
