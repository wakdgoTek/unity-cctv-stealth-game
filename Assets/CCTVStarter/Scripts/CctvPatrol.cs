using UnityEngine;
using UnityEngine.Serialization;

public class CctvPatrol : MonoBehaviour
{
    private const string HeadPivotName = "Head_Pivot";
    private const string LegacyViewPivotName = "View_Pivot";
    private const string LegacyYawPivotName = "Yaw_Pivot";

    [FormerlySerializedAs("viewPivot")]
    [FormerlySerializedAs("yawPivot")]
    [SerializeField] private Transform headPivot;
    [SerializeField] private float yawRange = 80f;
    [SerializeField] private float yawSpeed = 35f;

    private Quaternion homeLocalRotation;

    private void Awake()
    {
        if (headPivot == null)
        {
            headPivot = FindHeadPivot();
        }
    }

    private void Start()
    {
        EnsureHeadPivot();
        CaptureHomeTransform();
    }

    public void Configure(float newYawRange, float newYawSpeed)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
    }

    public void Configure(float newYawRange, float newYawSpeed, Transform newHeadPivot)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
        SetHeadPivot(newHeadPivot);
    }

    public void SetHeadPivot(Transform newHeadPivot)
    {
        headPivot = newHeadPivot;
        CaptureHomeTransform();
    }

    public void SetViewPivot(Transform newViewPivot)
    {
        SetHeadPivot(newViewPivot);
    }

    public void CaptureHomeTransform()
    {
        Transform pivot = headPivot != null ? headPivot : transform;
        homeLocalRotation = pivot.localRotation;
    }

    private void Update()
    {
        EnsureHeadPivot();

        float yaw = Mathf.PingPong(Time.time * yawSpeed, yawRange) - yawRange * 0.5f;
        headPivot.localRotation = homeLocalRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    private Transform FindHeadPivot()
    {
        Transform pivot = transform.Find(HeadPivotName);
        if (pivot != null)
        {
            return pivot;
        }

        pivot = transform.Find(LegacyViewPivotName);
        if (pivot != null)
        {
            return pivot;
        }

        return transform.Find(LegacyYawPivotName);
    }

    private void EnsureHeadPivot()
    {
        if (headPivot == null)
        {
            Transform existing = FindHeadPivot();
            if (existing != null)
            {
                headPivot = existing;
                headPivot.name = HeadPivotName;
            }
            else
            {
                GameObject pivotObject = new GameObject(HeadPivotName);
                headPivot = pivotObject.transform;
                headPivot.SetParent(transform, false);
                headPivot.localPosition = Vector3.zero;
                headPivot.localRotation = Quaternion.identity;
                headPivot.localScale = Vector3.one;
            }
        }

        if (headPivot.name != HeadPivotName)
        {
            headPivot.name = HeadPivotName;
        }

        MoveChildUnderHead("Camera_Body");
        MoveChildUnderHead("Lens");
        MoveChildUnderHead("Detection_Origin");
        UpdateDetectorOrigin();
    }

    private Transform FindChildInRig(string childName)
    {
        Transform direct = transform.Find(childName);
        if (direct != null)
        {
            return direct;
        }

        if (headPivot != null)
        {
            Transform underHead = headPivot.Find(childName);
            if (underHead != null)
            {
                return underHead;
            }
        }

        Transform legacyViewPivot = transform.Find(LegacyViewPivotName);
        Transform underView = legacyViewPivot != null ? legacyViewPivot.Find(childName) : null;
        if (underView != null)
        {
            return underView;
        }

        Transform legacyYawPivot = transform.Find(LegacyYawPivotName);
        return legacyYawPivot != null ? legacyYawPivot.Find(childName) : null;
    }

    private void MoveChildUnderHead(string childName)
    {
        Transform child = FindChildInRig(childName);
        if (child == null || child.parent == headPivot)
        {
            return;
        }

        child.SetParent(headPivot, true);
    }

    private void UpdateDetectorOrigin()
    {
        Transform detectionOrigin = FindChildInRig("Detection_Origin");
        if (detectionOrigin == null)
        {
            GameObject originObject = new GameObject("Detection_Origin");
            detectionOrigin = originObject.transform;
            detectionOrigin.SetParent(headPivot, false);
            detectionOrigin.localPosition = new Vector3(0f, 0f, 0.5f);
            detectionOrigin.localRotation = Quaternion.identity;
        }

        CctvDetector detector = GetComponent<CctvDetector>();
        if (detector != null)
        {
            detector.SetDetectionOrigin(detectionOrigin);
        }
    }
}
