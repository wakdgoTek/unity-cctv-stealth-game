using UnityEngine;

public class CctvPatrol : MonoBehaviour
{
    [SerializeField] private float yawRange = 80f;
    [SerializeField] private float yawSpeed = 35f;

    private Quaternion startRotation;

    private void Awake()
    {
        startRotation = transform.localRotation;
    }

    public void Configure(float newYawRange, float newYawSpeed)
    {
        yawRange = newYawRange;
        yawSpeed = newYawSpeed;
    }

    private void Update()
    {
        float yaw = Mathf.PingPong(Time.time * yawSpeed, yawRange) - yawRange * 0.5f;
        transform.localRotation = startRotation * Quaternion.Euler(0f, yaw, 0f);
    }
}
