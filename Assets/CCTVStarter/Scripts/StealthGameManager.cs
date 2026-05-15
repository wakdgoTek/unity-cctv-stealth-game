using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StealthGameManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private Text statusText;
    [SerializeField] private Text hintText;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float caughtResetDelay = 1.2f;

    private CctvDetector[] detectors;
    private bool gameEnded;
    private bool resetting;
    private bool started;

    private void OnEnable()
    {
        RefreshDetectors();

        if (started)
        {
            SubscribeToDetectors();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromDetectors();
    }

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<SimplePlayerController>();
        }

        if (startPosition == Vector3.zero && player != null)
        {
            startPosition = player.position;
        }

        RefreshDetectors();
    }

    private void Start()
    {
        started = true;
        RefreshDetectors();
        SubscribeToDetectors();
        ShowPlayingText();
    }

    private void Update()
    {
        if (WasResetPressed())
        {
            ResetGame();
        }
    }

    public void Configure(Transform newPlayer, SimplePlayerController newPlayerController, Text newStatusText, Text newHintText, Vector3 newStartPosition)
    {
        player = newPlayer;
        playerController = newPlayerController;
        statusText = newStatusText;
        hintText = newHintText;
        startPosition = newStartPosition;
    }

    public void OnPlayerDetected()
    {
        if (gameEnded || resetting)
        {
            return;
        }

        resetting = true;
        SetText(statusText, "발각됨!");
        SetText(hintText, "잠시 후 시작 위치로 돌아갑니다.");
        StartCoroutine(ResetAfterCaught());
    }

    public void CompleteGoal()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        resetting = false;
        SetText(statusText, "탈출 성공!");
        SetText(hintText, "R 키를 누르면 다시 시작합니다.");

        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
        }
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        gameEnded = false;
        resetting = false;
        MovePlayerToStart();
        ResetDetectors();
        ShowPlayingText();

        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
        }
    }

    private IEnumerator ResetAfterCaught()
    {
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
        }

        yield return new WaitForSeconds(caughtResetDelay);

        MovePlayerToStart();
        ResetDetectors();
        ShowPlayingText();
        resetting = false;

        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
        }
    }

    private void MovePlayerToStart()
    {
        if (player == null)
        {
            return;
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = startPosition;
            body.rotation = Quaternion.identity;
        }
        else
        {
            player.position = startPosition;
            player.rotation = Quaternion.identity;
        }

        if (playerController != null)
        {
            playerController.ResetView();
        }
    }

    private void ResetDetectors()
    {
        RefreshDetectors();

        if (detectors == null)
        {
            return;
        }

        foreach (CctvDetector detector in detectors)
        {
            if (detector != null)
            {
                detector.ForceResetDetection();
            }
        }
    }

    private void ShowPlayingText()
    {
        SetText(statusText, "목적지까지 들키지 말고 이동");
        SetText(hintText, "WASD 이동 | 마우스 시점 | 초록 구역에 도착");
    }

    private void RefreshDetectors()
    {
#if UNITY_2023_1_OR_NEWER
        detectors = FindObjectsByType<CctvDetector>(FindObjectsSortMode.None);
#else
        detectors = FindObjectsOfType<CctvDetector>();
#endif
    }

    private void SubscribeToDetectors()
    {
        if (detectors == null)
        {
            return;
        }

        foreach (CctvDetector detector in detectors)
        {
            if (detector == null)
            {
                continue;
            }

            if (detector.GetComponent<CctvViewVisualizer>() == null)
            {
                detector.gameObject.AddComponent<CctvViewVisualizer>();
            }

            if (detector.GetComponent<CctvPatrol>() == null)
            {
                detector.gameObject.AddComponent<CctvPatrol>();
            }

            CctvSideMover sideMover = detector.GetComponent<CctvSideMover>();
            if (sideMover != null)
            {
                sideMover.enabled = false;
            }

            detector.onDetected.RemoveListener(OnPlayerDetected);
            detector.onDetected.AddListener(OnPlayerDetected);
        }
    }

    private void UnsubscribeFromDetectors()
    {
        if (detectors == null)
        {
            return;
        }

        foreach (CctvDetector detector in detectors)
        {
            if (detector != null)
            {
                detector.onDetected.RemoveListener(OnPlayerDetected);
            }
        }
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static bool WasResetPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }
}
