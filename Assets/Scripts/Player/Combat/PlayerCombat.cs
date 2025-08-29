using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;                 // drag your Inventory (auto-finds if left empty)
    public Animator animator;                   // drag your Animator (auto-finds if left empty)

    [Header("Equipment")]
    public EquipSlot weaponSlot = EquipSlot.HandRight; // slot that must have the sword equipped

    [Header("Animator Setup")]
    public string upperBodyLayerName  = "UpperBody";   // layer that holds AttackUpper
    public string swordAttackTrigger  = "Attack";      // Any State -> AttackUpper trigger on that layer
    public string upperAttackStateTag = "Attack";      // tag set on AttackUpper state

    [Header("Layer Weight (UpperBody)")]
    public float fadeInSpeed  = 10f;   // weight per second toward 1
    public float fadeOutSpeed = 8f;    // weight per second toward 0
    public float maxWeight    = 1f;

    [Header("Cooldown")]
    public float swordCooldown = 0.05f;

    // ---- internals ---- 
    int   _upperLayerIndex = -1;
    int   _hashAttack;
    int   _attackTagHash;
    float _nextSwordTime;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)  animator  = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _upperLayerIndex = animator ? animator.GetLayerIndex(upperBodyLayerName) : -1;
        _hashAttack      = Animator.StringToHash(swordAttackTrigger);
        _attackTagHash   = Animator.StringToHash(upperAttackStateTag);

        if (_upperLayerIndex >= 0)
            animator.SetLayerWeight(_upperLayerIndex, 0f);
    }

    void Update()
    {
        if (!animator) return;
        if (Time.timeScale == 0f) return; // paused (e.g., inventory open)

        // ignore clicks over UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        // Left mouse -> sword swing (only if sword equipped)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TrySwordAttack();

        // Smoothly drive the UpperBody layer weight while the swing plays
        if (_upperLayerIndex >= 0)
        {
            var st = animator.GetCurrentAnimatorStateInfo(_upperLayerIndex);
            bool upperAttacking = st.tagHash == _attackTagHash || st.IsName("AttackUpper");

            float target  = upperAttacking ? maxWeight : 0f;
            float current = animator.GetLayerWeight(_upperLayerIndex);
            float speed   = (target > current) ? fadeInSpeed : fadeOutSpeed;
            float next    = Mathf.MoveTowards(current, target, speed * Time.deltaTime);

            if (!Mathf.Approximately(current, next))
                animator.SetLayerWeight(_upperLayerIndex, next);
        }
    }

    void TrySwordAttack()
    {
        if (_upperLayerIndex < 0) return;                 // no layer, nothing to do
        if (!HasSwordEquipped()) return;                  // require sword in HandRight
        if (Time.time < _nextSwordTime) return;           // cooldown

        animator.SetTrigger(_hashAttack);                 // fires AnyState->AttackUpper on UpperBody
        _nextSwordTime = Time.time + swordCooldown;
    }

    bool HasSwordEquipped()
    {
        if (!inventory || inventory.slotOrder == null) return false;
        int ix = System.Array.IndexOf(inventory.slotOrder, weaponSlot);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it);
    }
}
