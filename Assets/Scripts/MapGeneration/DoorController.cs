using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Header("Pivot / Geometry")]
    [Tooltip("Empty placed at the hinge edge (world pivot for rotation).")]
    public Transform hinge;                     // required when rotating root
    [Tooltip("Optional mesh transform; not required when rotating root.")]
    public Transform leaf;                      // kept for convenience; unused for motion

    [Header("Motion")]
    public float openAngle = 90f;               // degrees from closed to open
    public bool invertSwing = false;            // flip sign
    public float swingSpeed = 6f;               // higher = snappier
    public bool startOpen = false;

    [Header("Interaction")]
    public string playerTag = "Player";
    public float toggleCooldown = 0.25f;
    public bool requireLooking = false;
    [Range(0.4f, 0.95f)] public float lookDot = 0.6f;

    [Header("Optional")]
    public bool allowProximity = false;
    public float proximityRadius = 2.0f;
    public bool autoCloseWhenPlayerLeaves = false;
    public float autoCloseDelay = 2.0f;
    public bool debugLogs = false;
    public bool drawGizmos = true;

    // --- internal state ---
    Transform _playerInside;
    Transform _player;
    bool _isOpen;
    float _targetAngle;         // desired absolute angle (deg) from closed
    float _currentAngle;        // current absolute angle (deg) from closed
    float _angleVel;            // for SmoothDampAngle
    float _lastToggle;
    float _leaveTime = -1f;

    // cached base pose, pivot & axis (all in WORLD space)
    Vector3 _basePosW;
    Quaternion _baseRotW;
    Vector3 _pivotW;
    Vector3 _axisW;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // if you're using a separate blocking collider, add it as a 2nd collider (non-trigger)
    }

    void Awake()
    {
        if (!hinge)
        {
            Debug.LogError($"{name}: DoorController needs a Hinge Transform (world pivot).", this);
            enabled = false;
            return;
        }

        // cache base pose and pivot/axis (world space)
        _basePosW = transform.position;
        _baseRotW = transform.rotation;
        _pivotW   = hinge.position;           // world pivot stays fixed
        _axisW    = hinge.up.normalized;      // swing around world up of hinge

        _isOpen = startOpen;
        _targetAngle  = DesiredOpenAngle(_isOpen);
        _currentAngle = _targetAngle;
        ApplyAngle(_currentAngle);

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) _player = p.transform;
    }

    void Update()
    {
        // animate toward target angle
        float smoothTime = 1f / Mathf.Max(0.0001f, swingSpeed);
        _currentAngle = Mathf.SmoothDampAngle(_currentAngle, _targetAngle, ref _angleVel, smoothTime);
        ApplyAngle(_currentAngle);

        // can we interact?
        bool inside = _playerInside != null;
        if (!inside && allowProximity && _player)
            inside = Vector3.Distance(_player.position, transform.position) <= proximityRadius;

        if (inside && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (Time.time - _lastToggle >= toggleCooldown)
            {
                if (!requireLooking || PlayerIsLookingAtDoor())
                    Toggle();
            }
        }

        if (autoCloseWhenPlayerLeaves && !_playerInside && _isOpen && _leaveTime > 0f)
        {
            if (Time.time - _leaveTime >= autoCloseDelay)
                SetOpen(false);
        }
    }

    void ApplyAngle(float angleDeg)
    {
        // absolute rotation about a fixed world pivot/axis from the closed pose
        Quaternion R = Quaternion.AngleAxis(angleDeg, _axisW);
        transform.position = R * (_basePosW - _pivotW) + _pivotW;
        transform.rotation = R * _baseRotW;
    }

    float DesiredOpenAngle(bool open) =>
        open ? (invertSwing ? -Mathf.Abs(openAngle) : Mathf.Abs(openAngle)) : 0f;

    public void Toggle() => SetOpen(!_isOpen);

    public void SetOpen(bool open)
    {
        _isOpen = open;
        _targetAngle = DesiredOpenAngle(open);
        _lastToggle = Time.time;
        if (debugLogs) Debug.Log($"{name}: SetOpen({open})", this);
    }

    bool PlayerIsLookingAtDoor()
    {
        var cam = Camera.main;
        if (!cam) return true;
        Vector3 toDoor = (transform.position - cam.transform.position).normalized;
        return Vector3.Dot(cam.transform.forward, toDoor) > lookDot;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerInside = other.transform;
            _leaveTime = -1f;
            if (debugLogs) Debug.Log($"{name}: Player ENTER trigger", this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_playerInside && other.transform == _playerInside)
        {
            _playerInside = null;
            _leaveTime = Time.time;
            if (debugLogs) Debug.Log($"{name}: Player EXIT trigger", this);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        if (hinge)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hinge.position, 0.08f);
            Gizmos.DrawLine(hinge.position, hinge.position + hinge.up * 0.5f);
        }
        if (allowProximity)
        {
            Gizmos.color = new Color(0f,1f,0f,0.25f);
            Gizmos.DrawWireSphere(transform.position, proximityRadius);
        }
    }
#endif
}
