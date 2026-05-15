using UnityEngine;

public class CctvPatrol : MonoBehaviour
{
    private const string ViewPivotName = "View_Pivot";
    private const string LegacyYawPivotName = "Yaw_Pivot";

    [SerializeField] private Transform viewPivot;
    [SerializeField] private float yawRange = 80f;
    [SerializeField] private float yawSpeed = 35f;

    private Quaternion homeLocalRotation;

    private void Awake()
    {
        if (viewPivot == null)
        {
            viewPivot = transform.Find(ViewPivotName);
        }
    }

    private void Start()
    {
        EnsureViewPivot();
        CaptureHomeTransform();
    }

    public void Configure(float newYawRange, float newYawSpeed)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
    }

    public void Configure(float newYawRange, float newYawSpeed, Transform newViewPivot)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
        SetViewPivot(newViewPivot);
    }

    public void SetViewPivot(Transform newViewPivot)
    {
        viewPivot = newViewPivot;
        CaptureHomeTransform();
    }

    public void CaptureHomeTransform()
    {
        Transform pivot = viewPivot != null ? viewPivot : transform;
        homeLocalRotation = pivot.localRotation;
    }

    private void Update()
    {
        EnsureViewPivot();

        float yaw = Mathf.PingPong(Time.time * yawSpeed, yawRange) - yawRange * 0.5f;
        viewPivot.localRotation = homeLocalRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    private void EnsureViewPivot()
    {
        if (viewPivot == null)
        {
            viewPivot = transform.Find(ViewPivotName);
            if (viewPivot == null)
            {
                Transform detectionOrigin = FindDetectionOrigin();
                GameObject pivotObject = new GameObject(ViewPivotName);
                viewPivot = pivotObject.transform;
                viewPivot.SetParent(transform, false);

                if (detectionOrigin != null)
                {
                    viewPivot.position = detectionOrigin.position;
                    viewPivot.rotation = detectionOrigin.rotation;
                }
                else
                {
                    viewPivot.localPosition = Vector3.zero;
                    viewPivot.localRotation = Quaternion.identity;
                }

                viewPivot.localScale = Vector3.one;
            }
        }

        MoveChildBackToRoot("Camera_Body");
        MoveChildBackToRoot("Lens");
        MoveDetectionOriginUnderViewPivot();
    }

    private Transform FindDetectionOrigin()
    {
        Transform direct = transform.Find("Detection_Origin");
        if (direct != null)
        {
            return direct;
        }

        if (viewPivot != null)
        {
            Transform viewOrigin = viewPivot.Find("Detection_Origin");
            if (viewOrigin != null)
            {
                return viewOrigin;
            }
        }

        Transform legacyPivot = transform.Find(LegacyYawPivotName);
        return legacyPivot != null ? legacyPivot.Find("Detection_Origin") : null;
    }

    private void MoveChildBackToRoot(string childName)
    {
        Transform direct = transform.Find(childName);
        if (direct != null)
        {
            return;
        }

        Transform child = viewPivot != null ? viewPivot.Find(childName) : null;
        if (child == null)
        {
            Transform legacyPivot = transform.Find(LegacyYawPivotName);
            child = legacyPivot != null ? legacyPivot.Find(childName) : null;
        }

        if (child != null)
        {
            child.SetParent(transform, true);
        }
    }

    private void MoveDetectionOriginUnderViewPivot()
    {
        Transform detectionOrigin = FindDetectionOrigin();
        if (detectionOrigin == null)
        {
            GameObject originObject = new GameObject("Detection_Origin");
            detectionOrigin = originObject.transform;
        }

        detectionOrigin.SetParent(viewPivot, true);
        detectionOrigin.localPosition = Vector3.zero;
        detectionOrigin.localRotation = Quaternion.identity;

        CctvDetector detector = GetComponent<CctvDetector>();
        if (detector != null)
        {
            detector.SetDetectionOrigin(detectionOrigin);
        }
    }
}
