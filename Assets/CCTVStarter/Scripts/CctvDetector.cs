using UnityEngine;
using UnityEngine.Events;

public class CctvDetector : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private CctvDetectionTarget target;
    [SerializeField] private string autoFindTargetTag = "Player";

    [Header("View")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField] private float viewDistance = 12f;
    [SerializeField, Range(1f, 180f)] private float viewAngle = 65f;
    [SerializeField] private float requiredVisibleTime = 1.2f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("State")]
    [SerializeField] private bool drawDebugRay = true;

    [Header("Events")]
    public UnityEvent onDetected = new UnityEvent();
    public UnityEvent onLost = new UnityEvent();

    private float visibleTimer;
    private bool detected;

    public bool CanSeeTargetNow { get; private set; }
    public bool IsDetected => detected;
    public float DetectionProgress => requiredVisibleTime <= 0f ? 1f : Mathf.Clamp01(visibleTimer / requiredVisibleTime);
    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;
    public LayerMask ObstacleMask => obstacleMask;
    public Transform DetectionOrigin => Origin;

    private Transform Origin => detectionOrigin != null ? detectionOrigin : transform;

    private void Awake()
    {
        TryAutoFindTarget();
    }

    private void Update()
    {
        if (target == null)
        {
            TryAutoFindTarget();
        }

        CanSeeTargetNow = target != null && CanSeeTarget(target);
        UpdateDetectionState(CanSeeTargetNow);
    }

    public void Configure(CctvDetectionTarget newTarget, LayerMask newObstacleMask)
    {
        target = newTarget;
        obstacleMask = newObstacleMask;
    }

    public void Configure(CctvDetectionTarget newTarget, LayerMask newObstacleMask, Transform newDetectionOrigin)
    {
        target = newTarget;
        obstacleMask = newObstacleMask;
        detectionOrigin = newDetectionOrigin;
    }

    public void ConfigureView(float newViewDistance, float newViewAngle, float newRequiredVisibleTime)
    {
        viewDistance = newViewDistance;
        viewAngle = newViewAngle;
        requiredVisibleTime = newRequiredVisibleTime;
    }

    public void SetDetectionOrigin(Transform newDetectionOrigin)
    {
        detectionOrigin = newDetectionOrigin;
    }

    public void ForceResetDetection()
    {
        visibleTimer = 0f;
        detected = false;
        CanSeeTargetNow = false;
    }

    private void TryAutoFindTarget()
    {
        if (target != null || string.IsNullOrWhiteSpace(autoFindTargetTag))
        {
            return;
        }

        GameObject found = GameObject.FindGameObjectWithTag(autoFindTargetTag);
        if (found != null)
        {
            target = found.GetComponent<CctvDetectionTarget>();
        }
    }

    private bool CanSeeTarget(CctvDetectionTarget candidate)
    {
        Vector3 origin = Origin.position;
        Vector3 targetPosition = candidate.AimPosition;
        Vector3 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance > viewDistance)
        {
            return false;
        }

        Vector3 direction = toTarget.normalized;
        float angle = Vector3.Angle(Origin.forward, direction);
        if (angle > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == candidate.transform || hit.transform.IsChildOf(candidate.transform))
            {
                if (drawDebugRay)
                {
                    Debug.DrawLine(origin, targetPosition, Color.green);
                }

                return true;
            }

            if (drawDebugRay)
            {
                Debug.DrawLine(origin, hit.point, Color.red);
            }

            return false;
        }

        if (drawDebugRay)
        {
            Debug.DrawLine(origin, targetPosition, Color.green);
        }

        return true;
    }

    private void UpdateDetectionState(bool canSeeTarget)
    {
        if (canSeeTarget)
        {
            visibleTimer += Time.deltaTime;

            if (!detected && visibleTimer >= requiredVisibleTime)
            {
                detected = true;
                onDetected?.Invoke();
            }

            return;
        }

        visibleTimer = 0f;

        if (detected)
        {
            detected = false;
            onLost?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTransform = Origin;
        Vector3 origin = originTransform.position;

        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(origin, viewDistance);

        Vector3 left = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up) * originTransform.forward;
        Vector3 right = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up) * originTransform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, left * viewDistance);
        Gizmos.DrawRay(origin, right * viewDistance);
        Gizmos.DrawRay(origin, originTransform.forward * viewDistance);
    }
}
