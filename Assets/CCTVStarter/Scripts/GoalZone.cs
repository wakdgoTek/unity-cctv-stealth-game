using UnityEngine;

public class GoalZone : MonoBehaviour
{
    [SerializeField] private StealthGameManager gameManager;

    public void Configure(StealthGameManager newGameManager)
    {
        gameManager = newGameManager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameManager == null)
        {
            return;
        }

        if (other.GetComponentInParent<CctvDetectionTarget>() != null)
        {
            gameManager.CompleteGoal();
        }
    }
}
