using UnityEngine;

public class CctvDetectionTarget : MonoBehaviour
{
    [SerializeField] private Transform aimPoint;

    public Vector3 AimPosition
    {
        get
        {
            if (aimPoint != null)
            {
                return aimPoint.position;
            }

            return transform.position + Vector3.up;
        }
    }
}
