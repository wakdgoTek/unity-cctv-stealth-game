using UnityEngine;

public class CctvPatrol : MonoBehaviour
{
    [SerializeField] private float yawRange = 80f;
    [SerializeField] private float yawSpeed = 35f;

    private Quaternion startRotation;
    private Vector3 startPosition;

    private void Awake()
    {
        CaptureHomeTransform();
    }

    public void Configure(float newYawRange, float newYawSpeed)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
    }

    public void CaptureHomeTransform()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        float yaw = Mathf.PingPong(Time.time * yawSpeed, yawRange) - yawRange * 0.5f;
        transform.position = startPosition;
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * startRotation;
    }
}
