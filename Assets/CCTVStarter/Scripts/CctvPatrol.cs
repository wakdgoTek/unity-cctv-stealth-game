using UnityEngine;

public class CctvPatrol : MonoBehaviour
{
    private const string YawPivotName = "Yaw_Pivot";

    [SerializeField] private Transform yawPivot;
    [SerializeField] private float yawRange = 80f;
    [SerializeField] private float yawSpeed = 35f;

    private Quaternion homeLocalRotation;

    private void Awake()
    {
        if (yawPivot == null)
        {
            yawPivot = transform.Find(YawPivotName);
        }
    }

    private void Start()
    {
        EnsureYawPivot();
        CaptureHomeTransform();
    }

    public void Configure(float newYawRange, float newYawSpeed)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
    }

    public void Configure(float newYawRange, float newYawSpeed, Transform newYawPivot)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
        SetYawPivot(newYawPivot);
    }

    public void SetYawPivot(Transform newYawPivot)
    {
        yawPivot = newYawPivot;
        CaptureHomeTransform();
    }

    public void CaptureHomeTransform()
    {
        Transform pivot = GetPivotOrTransform();
        homeLocalRotation = pivot.localRotation;
    }

    private void Update()
    {
        EnsureYawPivot();

        float yaw = Mathf.PingPong(Time.time * yawSpeed, yawRange) - yawRange * 0.5f;
        yawPivot.localRotation = homeLocalRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    private Transform GetPivotOrTransform()
    {
        return yawPivot != null ? yawPivot : transform;
    }

    private void EnsureYawPivot()
    {
        if (yawPivot != null)
        {
            return;
        }

        Transform existing = transform.Find(YawPivotName);
        if (existing != null)
        {
            yawPivot = existing;
            return;
        }

        GameObject pivotObject = new GameObject(YawPivotName);
        yawPivot = pivotObject.transform;
        yawPivot.SetParent(transform, false);
        yawPivot.localPosition = Vector3.zero;
        yawPivot.localRotation = Quaternion.identity;
        yawPivot.localScale = Vector3.one;

        MoveChildUnderPivot("Camera_Body");
        MoveChildUnderPivot("Lens");
        MoveChildUnderPivot("Detection_Origin");
    }

    private void MoveChildUnderPivot(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null || child == yawPivot || child.IsChildOf(yawPivot))
        {
            return;
        }

        child.SetParent(yawPivot, true);
    }
}
