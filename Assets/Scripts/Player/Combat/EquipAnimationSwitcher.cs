using UnityEngine;

public class EquipAnimationSwitcher : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;                  // drag your Inventory
    public Animator animator;                    // player Animator

    [Header("Controllers")]
    public RuntimeAnimatorController baseController;        // your current controller (punch)
    public AnimatorOverrideController swordOverride;        // the AOC you made

    [Header("Slot to check")]
    public EquipSlot weaponSlot = EquipSlot.HandRight;      // which slot enables sword set

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (!animator)   animator   = GetComponentInChildren<Animator>();
        ApplyNow();
    }

    void OnEnable()
    {
        if (inventory) inventory.OnChanged += ApplyNow;     // react to equip/unequip
    }
    void OnDisable()
    {
        if (inventory) inventory.OnChanged -= ApplyNow;
    }

    // Return true if something is equipped in the chosen slot
    bool HasWeaponEquipped()
    {
        if (inventory == null || inventory.slotOrder == null) return false;
        int ix = System.Array.IndexOf(inventory.slotOrder, weaponSlot);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it); // non-empty = treat as weapon equipped
    }

    public void ApplyNow()
    {
        if (!animator || !baseController) return;

        var wantsSword = HasWeaponEquipped() && swordOverride != null;
        var next = wantsSword ? (RuntimeAnimatorController)swordOverride : baseController;

        if (animator.runtimeAnimatorController != next)
            animator.runtimeAnimatorController = next;
    }
}
