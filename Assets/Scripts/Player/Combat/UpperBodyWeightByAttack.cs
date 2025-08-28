using UnityEngine;
using UnityEngine.InputSystem;

public class UpperBodyCombatController : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;               // drag your Inventory
    public Animator animator;                 // drag your Animator (or auto-find)

    [Header("Animator Setup")]
    public string upperBodyLayerName = "UpperBody";
    public string attackTrigger      = "Attack";   // transition condition on UpperBody layer
    public string attackStateTag     = "Attack";   // tag set on AttackUpper state

    [Header("Equipment")]
    public EquipSlot weaponSlot = EquipSlot.HandRight;  // must match your sword slot

    [Header("Layer Weight")]
    public float fadeInSpeed  = 10f;  // weight per second
    public float fadeOutSpeed = 8f;
    public float maxWeight    = 1f;

    [Header("Cooldown (optional)")]
    public float attackCooldown = 0.05f; // tiny spam guard
    float _nextAttackTime;

    int _upperLayerIndex = -1;
    int _attackTagHash;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)  animator  = GetComponentInChildren<Animator>();
        _upperLayerIndex = animator ? animator.GetLayerIndex(upperBodyLayerName) : -1;
        _attackTagHash   = Animator.StringToHash(attackStateTag);

        if (_upperLayerIndex >= 0 && animator != null)
            animator.SetLayerWeight(_upperLayerIndex, 0f);
    }

    void Update()
    {
        // 1) Input: Left mouse to attack (new Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryAttack();

        // 2) Weight: fade layer on while upper body is attacking, off otherwise
        if (_upperLayerIndex >= 0 && animator != null)
        {
            var st   = animator.GetCurrentAnimatorStateInfo(_upperLayerIndex);
            bool attackingUpper = st.tagHash == _attackTagHash || st.IsName("AttackUpper");

            float target = attackingUpper ? maxWeight : 0f;
            float current = animator.GetLayerWeight(_upperLayerIndex);
            float speed = (target > current) ? fadeInSpeed : fadeOutSpeed;
            float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);

            if (!Mathf.Approximately(current, next))
                animator.SetLayerWeight(_upperLayerIndex, next);
        }
    }

    void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;
        if (!HasItemInSlot(weaponSlot)) return; // require sword equipped

        // Fire the upper-body attack
        animator.SetTrigger(attackTrigger);

        // tiny cooldown to prevent over-trigger
        _nextAttackTime = Time.time + attackCooldown;
    }

    bool HasItemInSlot(EquipSlot slot)
    {
        if (!inventory || inventory.slotOrder == null) return false;
        int ix = System.Array.IndexOf(inventory.slotOrder, slot);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it);
    }
}
