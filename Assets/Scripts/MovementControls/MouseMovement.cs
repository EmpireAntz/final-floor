using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;     // The body that should yaw (usually the Player object)
    public Transform cameraPivot;    // Empty parent of the Camera that should pitch

    [Header("Look Settings")]
    public float mouseSensitivity = 0.12f; // tune to taste; no Time.deltaTime for mouse delta
    public float maxPitchUp = 70f;
    public float maxPitchDown = 80f;

    float yaw;    // around Y (body)
    float pitch;  // around X (camera only)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Mouse delta is pixels THIS FRAME; no Time.deltaTime needed
        Vector2 md = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // Yaw the player root (keeps feet planted)
        yaw += md.x;
        if (playerRoot != null)
            playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Pitch the camera pivot only
        pitch -= md.y;
        pitch = Mathf.Clamp(pitch, -maxPitchDown, maxPitchUp);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
