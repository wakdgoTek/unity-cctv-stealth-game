using UnityEngine;

public class CctvSideMover : MonoBehaviour
{
    [SerializeField] private float moveDistance = 2.5f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private Vector3 localMoveAxis = Vector3.right;

    private Vector3 startPosition;
    private Vector3 moveAxis;

    private void Awake()
    {
        startPosition = transform.position;
        moveAxis = transform.TransformDirection(localMoveAxis.normalized);
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance * 0.5f;
        transform.position = startPosition + moveAxis * offset;
    }

    public void Configure(float newMoveDistance, float newMoveSpeed, Vector3 newLocalMoveAxis)
    {
        moveDistance = newMoveDistance;
        moveSpeed = newMoveSpeed;
        localMoveAxis = newLocalMoveAxis;
    }
}
