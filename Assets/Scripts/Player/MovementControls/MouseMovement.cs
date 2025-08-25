using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;     // yaw on this (body)
    public Transform cameraPivot;    // pitch on this (parent of Camera)

    [Header("Look Settings")]
    public float mouseSensitivity = 0.12f;
    public float maxPitchUp = 70f;
    public float maxPitchDown = 80f;

    [Header("Guards (no setup needed)")]
    public bool stopWhenTimeScaleZero   = true;  // pauses when you set Time.timeScale = 0
    public bool stopWhenCursorUnlocked  = true;  // stops when UI unlocks cursor
    public bool stopWhenAppUnfocused    = true;  // stops if app/window loses focus

    float yaw;    // degrees around Y
    float pitch;  // degrees around X
    bool _syncedOnce;
    bool _cursorWasLocked;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cursorWasLocked = (Cursor.lockState == CursorLockMode.Locked);
    }

    void OnEnable()  { SyncToTransforms(); }   // in case this enables after spawn
    void Start()     { SyncToTransforms(); }   // double safety

    // Call this if you ever programmatically rotate the player/camera
    public void SyncToTransforms()
    {
        if (playerRoot)  yaw   = playerRoot.eulerAngles.y;
        if (cameraPivot) pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
        _syncedOnce = true;
    }

    void Update()
    {
        // Global guards (no InventoryUI assignment needed)
        if (stopWhenAppUnfocused && !Application.isFocused) return;
        if (stopWhenTimeScaleZero && Time.timeScale == 0f) { _syncedOnce = false; return; }

        bool locked = (Cursor.lockState == CursorLockMode.Locked);

        // If cursor is unlocked (e.g., UI open), stop and prep to resync when it relocks
        if (stopWhenCursorUnlocked && !locked)
        {
            _cursorWasLocked = false;
            _syncedOnce = false;
            return;
        }

        // Just re-locked this frame? Resync and eat the first delta to avoid a jump
        if (stopWhenCursorUnlocked && locked && !_cursorWasLocked)
        {
            SyncToTransforms();
            _cursorWasLocked = true;
            // consume this frame's delta so we don't add it after relock
            return;
        }

        _cursorWasLocked = locked;

        if (Mouse.current == null) return;

        Vector2 md = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // If some other script rotated us and the mouse hasn't moved yet, resync once
        if (!_syncedOnce && Mathf.Approximately(md.x, 0f) && playerRoot)
            yaw = playerRoot.eulerAngles.y;
        if (!_syncedOnce && Mathf.Approximately(md.y, 0f) && cameraPivot)
            pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
        if (!Mathf.Approximately(md.x, 0f) || !Mathf.Approximately(md.y, 0f))
            _syncedOnce = true;

        // Accumulate look
        yaw   += md.x;
        pitch -= md.y;
        pitch  = Mathf.Clamp(pitch, -maxPitchDown, maxPitchUp);

        // Apply
        if (playerRoot)
            playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }
}
