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
        enabled = false;
    }

    private void Update()
    {
    }

    public void Configure(float newMoveDistance, float newMoveSpeed, Vector3 newLocalMoveAxis)
    {
        moveDistance = newMoveDistance;
        moveSpeed = newMoveSpeed;
        localMoveAxis = newLocalMoveAxis;
    }
}
