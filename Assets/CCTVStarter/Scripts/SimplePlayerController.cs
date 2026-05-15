using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float lookDeadZone = 0.35f;
    [SerializeField] private float maxLookDeltaPerFrame = 18f;
    [SerializeField] private float lookInputLockDuration = 0.35f;
    [SerializeField] private float eyeHeight = 0.75f;
    [SerializeField] private Camera playerCamera;

    private Rigidbody body;
    private float yaw;
    private float pitch;
    private bool controlsEnabled = true;
    private int ignoreLookFrames;
    private float lookInputLockedUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        yaw = transform.eulerAngles.y;
        SetupFirstPersonCamera();
    }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!controlsEnabled || !Application.isFocused || Time.unscaledTime < lookInputLockedUntil)
        {
            FlushLookInput();
            return;
        }

        if (ignoreLookFrames > 0)
        {
            ignoreLookFrames--;
            FlushLookInput();
            return;
        }

        Vector2 look = Vector2.ClampMagnitude(ReadLookInput(), maxLookDeltaPerFrame);
        if (look.sqrMagnitude < lookDeadZone * lookDeadZone)
        {
            return;
        }

        yaw += look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -80f, 80f);
        ApplyBodyRotation();

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            return;
        }

        Vector2 input = ReadMoveInput();

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move.y = 0f;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        Vector3 nextPosition = body.position + move * moveSpeed * Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (enabled)
        {
            SuppressLookInput(0.25f, 8);
        }

        ApplyCursorState();
    }

    public void ResetView()
    {
        pitch = 0f;
        yaw = 0f;
        ApplyBodyRotation();
        SuppressLookInput(lookInputLockDuration, 12);

        if (body != null)
        {
            body.rotation = Quaternion.identity;
            body.angularVelocity = Vector3.zero;
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0.08f);
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }

    private void ApplyBodyRotation()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

        if (body != null)
        {
            body.rotation = rotation;
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = controlsEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !controlsEnabled;
    }

    private void SetupFirstPersonCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            playerCamera = cameraObject.AddComponent<Camera>();
        }

        playerCamera.gameObject.SetActive(true);
        playerCamera.enabled = true;
        playerCamera.tag = "MainCamera";
        playerCamera.targetDisplay = 0;
        playerCamera.cullingMask = ~0;
        playerCamera.transform.SetParent(transform, false);
        playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0.08f);
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.fieldOfView = 70f;
        playerCamera.nearClipPlane = 0.05f;

        if (playerCamera.GetComponent<AudioListener>() == null && FindExistingAudioListener() == null)
        {
            playerCamera.gameObject.AddComponent<AudioListener>();
        }
    }

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            return input;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    private static Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    private void SuppressLookInput(float duration, int frames)
    {
        ignoreLookFrames = Mathf.Max(ignoreLookFrames, frames);
        lookInputLockedUntil = Mathf.Max(lookInputLockedUntil, Time.unscaledTime + duration);
        FlushLookInput();
    }

    private static void FlushLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Mouse.current.delta.ReadValue();
        }
#endif
    }

    private static AudioListener FindExistingAudioListener()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<AudioListener>();
#else
        return FindObjectOfType<AudioListener>();
#endif
    }
}
