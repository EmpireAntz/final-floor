using UnityEngine;

public class EquipUpperBodyLayer : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;               // your Inventory
    public Animator animator;                 // player Animator

    [Header("Config")]
    public string upperBodyLayerName = "UpperBody";
    public EquipSlot weaponSlot = EquipSlot.HandRight;
    public string attackStateTag = "Attack";  // tag the base-layer attack state with this
    public float fadeInSpeed  = 8f;           // weight per second
    public float fadeOutSpeed = 6f;
    public float maxWeight    = 1f;

    int _upperLayerIndex = -1;
    int _attackTagHash;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)  animator  = GetComponentInChildren<Animator>();

        _upperLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
        if (_upperLayerIndex < 0)
            Debug.LogError($"Animator has no layer named '{upperBodyLayerName}'.");

        _attackTagHash = Animator.StringToHash(attackStateTag);

        // optional: start at 0
        if (_upperLayerIndex >= 0) animator.SetLayerWeight(_upperLayerIndex, 0f);
    }

    void OnEnable()
    {
        if (inventory) inventory.OnChanged += OnInventoryChanged;
    }
    void OnDisable()
    {
        if (inventory) inventory.OnChanged -= OnInventoryChanged;
    }

    void OnInventoryChanged()
    {
        // you could react instantly here if you want; we handle it in Update with smooth fade
    }

    void Update()
    {
        if (_upperLayerIndex < 0 || animator == null) return;

        // Is a weapon equipped?
        bool hasWeapon = HasItemInSlot(weaponSlot);

        // Is the BASE layer currently playing an "Attack" tagged state?
        var st = animator.GetCurrentAnimatorStateInfo(0); // base layer = 0
        bool baseIsAttacking = st.tagHash == _attackTagHash || animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).tagHash == _attackTagHash;

        // Target weight
        float target = (hasWeapon && baseIsAttacking) ? maxWeight : 0f;

        // Smooth towards it
        float current = animator.GetLayerWeight(_upperLayerIndex);
        float speed = (target > current) ? fadeInSpeed : fadeOutSpeed;
        float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        if (!Mathf.Approximately(current, next))
            animator.SetLayerWeight(_upperLayerIndex, next);
    }

    bool HasItemInSlot(EquipSlot slot)
    {
        if (inventory == null || inventory.slotOrder == null) return false;
        int ix = System.Array.IndexOf(inventory.slotOrder, slot);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it);
    }
}
