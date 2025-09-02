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
    [Tooltip("States on the UpperBody layer, in order of the combo.")]
    public string[] attackStates = { "AttackUpper1", "AttackUpper2", "AttackUpper3" };
    [Tooltip("Optional: tag all your attack states with this tag to detect when attacking.")]
    public string attackStateTag = "Attack";

    [Header("Requirements")]
    public EquipSlot weaponSlot = EquipSlot.HandRight;   // must be equipped for attacks to work

    [Header("Layer Weight")]
    public float fadeInSpeed  = 10f;   // weight per second (toward 1)
    public float fadeOutSpeed = 8f;    // weight per second (toward 0)
    public float maxWeight    = 1f;
    public float crossfade    = 0.05f; // CrossFadeInFixedTime duration

    [Header("Combo / Input")]
    public float comboResetTime = 0.6f;   // wait longer than this -> reset to first attack
    public float clickCooldown  = 0.05f;  // tiny spam guard

    int   _upperLayer = -1;
    int   _attackTagHash;
    int   _step = -1;
    float _lastClick;
    float _nextClick;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)  animator  = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _upperLayer   = animator ? animator.GetLayerIndex(upperBodyLayerName) : -1;
        _attackTagHash = Animator.StringToHash(attackStateTag);

        if (_upperLayer >= 0) animator.SetLayerWeight(_upperLayer, 0f);
    }

    void Update()
    {
        if (!animator || _upperLayer < 0) return;
        if (Time.timeScale == 0f) return; // paused (eg. inventory open)

        // 1) Input
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // ignore clicks over UI
            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
                TryAttack();
        }

        // 2) Drive layer weight while an attack is playing
        var st = animator.GetCurrentAnimatorStateInfo(_upperLayer);
        bool attacking = st.tagHash == _attackTagHash
                         || IsAnyAttackState(st);
        float target  = attacking ? maxWeight : 0f;
        float current = animator.GetLayerWeight(_upperLayer);
        float speed   = (target > current) ? fadeInSpeed : fadeOutSpeed;
        float next    = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        if (!Mathf.Approximately(current, next))
            animator.SetLayerWeight(_upperLayer, next);
    }

    void TryAttack()
    {
        if (Time.time < _nextClick) return;
        if (!HasItemInSlot(weaponSlot)) return; // require sword equipped

        // reset combo if you waited too long
        if (Time.time - _lastClick > comboResetTime)
            _step = -1;

        // advance 0 -> 1 -> 2 -> 0 ...
        _step = (_step + 1) % attackStates.Length;

        // fire chosen state on the upper-body layer from the start
        animator.CrossFadeInFixedTime(attackStates[_step], crossfade, _upperLayer, 0f);

        _lastClick  = Time.time;
        _nextClick  = Time.time + clickCooldown;
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
        // In case you don’t use tags, we also check names
        for (int i = 0; i < attackStates.Length; i++)
            if (st.IsName(attackStates[i]))
                return true;
        return false;
    }
}
