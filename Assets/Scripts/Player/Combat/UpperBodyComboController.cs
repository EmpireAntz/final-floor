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
    public string attackStateTag = "Attack";  

    [Header("Requirements")]
    public EquipSlot weaponSlot = EquipSlot.HandRight;

    [Header("Layer Weight")]
    public float fadeInSpeed  = 10f;
    public float fadeOutSpeed = 8f;
    public float maxWeight    = 1f;
    public float crossfade    = 0.05f;

    [Header("Combo / Input")]
    public float comboResetTime = 0.6f;   // extend this window on every click
    public float clickCooldown  = 0.05f;

    int   _upperLayer = -1;
    int   _attackTagHash;
    int   _step = -1;
    float _nextClick = 0f;

    // track combo window and queued click
    float _comboUntil = 0f;
    bool  _queuedNext = false;
    

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

        // Drive layer weight
        var st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
        bool attacking = st.tagHash == _attackTagHash || IsAnyAttackState(st);
        float target  = attacking ? maxWeight : 0f;
        float current = animator.GetLayerWeight(_upperLayer);
        float speed   = (target > current) ? fadeInSpeed : fadeOutSpeed;
        float next    = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        if (!Mathf.Approximately(current, next))
            animator.SetLayerWeight(_upperLayer, next);

        // Fire queued attack right after current completes
        if (_queuedNext && !animator.IsInTransition(_upperLayer))
        {
            st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
            if (st.normalizedTime >= 1f || !IsAnyAttackState(st))
            {
                _queuedNext = false;
                PlayNextAttack(); // uses the preserved combo window
            }
        }
    }

    void TryAttack()
    {
        if (Time.time < _nextClick) return;
        if (!HasItemInSlot(weaponSlot)) return;

        // Extend/refresh combo window on every click
        _comboUntil = Time.time + comboResetTime;

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
        // Only reset to the first attack if the combo window has actually expired
        if (Time.time > _comboUntil) _step = -1;

        _step = (_step + 1) % attackStates.Length;
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
}
