using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UpperBodyComboController : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;             // auto-found if left empty
    public Animator animator;               // auto-found if left empty

    [Header("Animator Setup")]
    public string upperBodyLayerName = "UpperBody";
    public string[] attackStates = { "AttackUpper1", "AttackUpper2", "AttackUpper3" };
    public string attackStateTag = "Attack";    // optional tag on those states

    [Header("Requirements")]
    public EquipSlot weaponSlot = EquipSlot.HandRight;

    [Header("Layer Weight")]
    public float fadeInSpeed = 10f;
    public float fadeOutSpeed = 8f;
    public float maxWeight = 1f;
    public float crossfade = 0.05f;

    [Header("Combo / Input")]
    public float comboResetTime = 0.6f;
    public float clickCooldown = 0.05f;

    // ---------- Slash (no Animation Events; mounted under Player) ----------
    [Header("Slash (no Animation Events)")]
    [Tooltip("Same prefab for all attacks; toggled from code.")]
    public GameObject slashPrefab;          // pooled instance under player
    public Transform slashParent;           // leave null => this.transform

    // Per-attack timings in normalized [0..1] (must match attackStates order)
    [Range(0,1f)] public float[] slashOnTimes  = new float[] { 0.25f, 0.30f, 0.35f };
    [Range(0,1f)] public float[] slashOffTimes = new float[] { 0.45f, 0.50f, 0.55f };

    [Tooltip("If your prefab's ParticleSystem should rely on Play On Awake.")]
    public bool slashPlayOnEnable = true;

    [Tooltip("Optional per-attack local offsets/rotations under slashParent/player.")]
    public Vector3[] slashLocalPositions;   // size = 3 if you want per-attack placement
    public Vector3[] slashLocalEulers;      // size = 3 if you want per-attack rotation

    GameObject _slashInstance;              // pooled VFX
    bool _firedOn, _firedOff;               // guards for on/off per attack
    // ----------------------------------------------------------------------

    int   _upperLayer = -1;
    int   _attackTagHash;
    int   _step = -1;
    float _nextClick = 0f;

    bool  _queuedNext = false;
    float _lastAttackEndTime = -999f;
    bool  _wasAttacking = false;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)  animator  = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _upperLayer    = animator ? animator.GetLayerIndex(upperBodyLayerName) : -1;
        _attackTagHash = Animator.StringToHash(attackStateTag);

        if (_upperLayer >= 0) animator.SetLayerWeight(_upperLayer, 0f);
    }

    void Update()
    {
        if (!animator || _upperLayer < 0) return;
        if (Time.timeScale == 0f) return;

        // Input
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
                TryAttack();
        }

        // State info
        var st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
        bool attacking = st.tagHash == _attackTagHash || IsAnyAttackState(st);

        // Fire slash by normalized time (no animation events)
        if (attacking && !animator.IsInTransition(_upperLayer))
        {
            int idx = Mathf.Clamp(_step, 0, attackStates.Length - 1);
            float t = st.normalizedTime;                   // first pass only
            if (t >= 0f && t < 1.1f)
            {
                if (!_firedOn  && idx < slashOnTimes.Length  && t >= slashOnTimes[idx])  { _firedOn  = true; SlashOnForStep(idx); }
                if (!_firedOff && idx < slashOffTimes.Length && t >= slashOffTimes[idx]) { _firedOff = true; SlashOff(); }
            }
        }
        else if (_firedOn || _firedOff)
        {
            // left attack state; reset guards
            _firedOn = _firedOff = false;
        }

        // Drive layer weight
        float target  = attacking ? maxWeight : 0f;
        float current = animator.GetLayerWeight(_upperLayer);
        float speed   = (target > current) ? fadeInSpeed : fadeOutSpeed;
        float next    = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        if (!Mathf.Approximately(current, next)) animator.SetLayerWeight(_upperLayer, next);

        // Detect leaving attack to start cooldown window (and ensure VFX off)
        if (_wasAttacking && !attacking && !animator.IsInTransition(_upperLayer))
        {
            _lastAttackEndTime = Time.time;
            SlashOff();                       // << safety-off when leaving state
        }

        // Release queued attack once current finished/left attack
        if (_queuedNext && !animator.IsInTransition(_upperLayer))
        {
            st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
            attacking = st.tagHash == _attackTagHash || IsAnyAttackState(st);
            if (!attacking || st.normalizedTime >= 1f)
            {
                _queuedNext = false;
                PlayNextAttack();
            }
        }

        _wasAttacking = attacking;
    }

    void TryAttack()
    {
        if (Time.time < _nextClick) return;
        if (!HasItemInSlot(weaponSlot)) return;

        var st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
        bool inAttack = st.tagHash == _attackTagHash || IsAnyAttackState(st);

        // If we're in the middle of an attack, just queue the next
        if (inAttack && st.normalizedTime < 1f)
        {
            _queuedNext = true;
            _nextClick = Time.time + clickCooldown;
            return;
        }

        PlayNextAttack();
    }

    void PlayNextAttack()
    {
        // Reset combo if cooldown since last finished attack expired
        if (Time.time - _lastAttackEndTime > comboResetTime)
            _step = -1;

        // make sure previous slash is not lingering when chaining
        SlashOff();                 // << force-off before starting next attack

        _step = (_step + 1) % attackStates.Length;

        // new attack: reset slash guards
        _firedOn = _firedOff = false;

        animator.CrossFadeInFixedTime(attackStates[_step], crossfade, _upperLayer, 0f);
        _nextClick = Time.time + clickCooldown;
    }

    bool HasItemInSlot(EquipSlot slot)
    {
        if (!inventory || inventory.slotOrder == null) return false;
        int ix = System.Array.IndexOf(inventory.slotOrder, slot);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it);
    }

    bool IsAnyAttackState(AnimatorStateInfo st)
    {
        for (int i = 0; i < attackStates.Length; i++)
            if (st.IsName(attackStates[i])) return true;
        return false;
    }

    // ==================== Slash helpers (pooled under Player) ====================
    void EnsureSlashInstance()
    {
        if (_slashInstance || !slashPrefab) return;
        var parent = slashParent ? slashParent : transform;
        _slashInstance = Instantiate(slashPrefab, parent);
        _slashInstance.name = "Slash_VFX (pooled)";
        _slashInstance.SetActive(false);
    }

    void SlashOnForStep(int idx)
    {
        EnsureSlashInstance();
        if (!_slashInstance) return;

        // Optional per-attack placement
        if (slashLocalPositions != null && idx < slashLocalPositions.Length)
            _slashInstance.transform.localPosition = slashLocalPositions[idx];
        if (slashLocalEulers != null && idx < slashLocalEulers.Length)
            _slashInstance.transform.localRotation = Quaternion.Euler(slashLocalEulers[idx]);

        var ps = _slashInstance.GetComponent<ParticleSystem>();
        if (ps) { ps.Clear(true); if (!slashPlayOnEnable) ps.Play(); }

        _slashInstance.SetActive(true);
    }

    void SlashOff()
    {
        if (!_slashInstance) return;
        var ps = _slashInstance.GetComponent<ParticleSystem>();
        if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _slashInstance.SetActive(false);
    }

    // Keep these public if you ever switch back to Animation Events:
    public void Anim_SlashOn()  => SlashOnForStep(Mathf.Clamp(_step, 0, attackStates.Length - 1));
    public void Anim_SlashOff() => SlashOff();
    // ============================================================================
}
