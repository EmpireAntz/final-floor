using UnityEngine;

public class FootstepAudioBlendTree : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    public string locomotionLayerName = "Base Layer";
    [Tooltip("Name of the WALK blend-tree state in your Animator")]
    public string walkTreeState = "Walk";     // <-- set to your walk state name
    [Tooltip("Name of the RUN blend-tree state in your Animator")]
    public string runTreeState  = "Run";      // <-- set to your run state name

    [Header("Step times (normalized 0..1)")]
    [Range(0,1f)] public float walkLeft  = 0.15f;
    [Range(0,1f)] public float walkRight = 0.65f;
    [Range(0,1f)] public float runLeft   = 0.10f;
    [Range(0,1f)] public float runRight  = 0.60f;

    [Tooltip("Minimum seconds between two steps to prevent double fires")]
    public float retriggerPadding = 0.08f;

    [Header("Audio")]
    public AudioSource source;        // 3D AudioSource on player
    public AudioClip[] defaultClips;  // random pick each step
    [Range(0,1f)] public float volume = 0.8f;

    [Header("Ground check (optional)")]
    public Transform groundProbe;     // usually hips or root
    public float probeDistance = 0.6f;
    public LayerMask groundMask = ~0; // everything by default

    int _layer;
    float _lastLeft, _lastRight;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _layer = animator ? animator.GetLayerIndex(locomotionLayerName) : 0;
        _lastLeft = _lastRight = -999f;
    }

    void Update()
    {
        if (!animator || animator.layerCount <= _layer) return;

        var st = animator.GetCurrentAnimatorStateInfo(_layer);
        if (animator.IsInTransition(_layer)) return; // avoid jitter while blending

        bool inWalk = st.IsName(walkTreeState);
        bool inRun  = st.IsName(runTreeState);
        if (!inWalk && !inRun) return;

        // 0..1 phase within current cycle (works for looped blend trees)
        float t = st.normalizedTime % 1f;

        // Choose times based on which tree we’re in
        float left = inWalk ? walkLeft  : runLeft;
        float right= inWalk ? walkRight : runRight;

        // Fire near the target times (frame-agnostic)
        float tol = Time.deltaTime * 1.5f;

        if (Time.time - _lastLeft > retriggerPadding && Mathf.Abs(t - left) < tol)
        {
            PlayFootstep();
            _lastLeft = Time.time;
        }
        if (Time.time - _lastRight > retriggerPadding && Mathf.Abs(t - right) < tol)
        {
            PlayFootstep();
            _lastRight = Time.time;
        }
    }

    void PlayFootstep()
    {
        if (!source) return;

        // Optional: simple surface probe (hook up tags/materials later if you want)
        if (Physics.Raycast((groundProbe ? groundProbe.position : transform.position) + Vector3.up * 0.1f,
                            Vector3.down, out var hit, probeDistance + 0.2f, groundMask))
        {
            // you could branch on hit.collider.tag here for different sets
        }

        var clip = Pick(defaultClips);
        if (clip) source.PlayOneShot(clip, volume);
    }

    AudioClip Pick(AudioClip[] set)
    {
        if (set == null || set.Length == 0) return null;
        return set[Random.Range(0, set.Length)];
    }
}
