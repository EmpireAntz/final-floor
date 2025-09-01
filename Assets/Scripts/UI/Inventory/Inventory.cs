using System.Collections.Generic;
using UnityEngine;

#region SimpleItem with rolled stats (lives in this file)
[System.Serializable]
public class SimpleItem
{
    public ItemData data;

    // Rolled, per-instance stats
    public bool  rolled;
    public float addDamage;
    public float addMaxHealth;
    public float addMaxStamina;
    public float addDefensePercent;
    public float addCritChancePercent;

    // Roll once from ItemData ranges
    public void EnsureRolled()
    {
        if (rolled || data == null) return;
        // These require ItemData to have StatRange fields with .Roll()
        addDamage            = data.damage.Roll();
        addMaxHealth         = data.maxHealth.Roll();
        addMaxStamina        = data.maxStamina.Roll();
        addDefensePercent    = data.defensePercent.Roll();
        addCritChancePercent = data.critChancePercent.Roll();
        rolled = true;
    }

    public static SimpleItem CreateRolled(ItemData d)
    {
        var it = new SimpleItem { data = d };
        it.EnsureRolled();
        return it;
    }
}
#endregion

public enum ContainerType { Inventory, Equipment, Chest }

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    public int capacity = 20;
    public List<SimpleItem> items = new List<SimpleItem>();

    [Header("Equipment")]
    public EquipSlot[] slotOrder = new[] {
        EquipSlot.HandRight, EquipSlot.Head, EquipSlot.Chest, EquipSlot.Feet
    };
    public int equipmentCapacity = 4;
    public List<SimpleItem> equipment = new List<SimpleItem>();

    public System.Action OnChanged;

    // icon-less counts as empty to keep your UI behavior
    public static bool IsEmpty(SimpleItem it) => it == null || it.data == null || it.data.icon == null;

    void OnValidate() =>
        equipmentCapacity = (slotOrder != null && slotOrder.Length > 0) ? slotOrder.Length : 4;

    void Awake()
    {
        if (equipment == null) equipment = new List<SimpleItem>(equipmentCapacity);
        if (equipment.Count > equipmentCapacity)
            equipment.RemoveRange(equipmentCapacity, equipment.Count - equipmentCapacity);
        while (equipment.Count < equipmentCapacity) equipment.Add(null);

        if (items == null) items = new List<SimpleItem>();
        if (items.Count > capacity) items.RemoveRange(capacity, items.Count - capacity);

        // Safety: ensure anything already present is rolled once
        EnsureAllRolled();
    }

    void EnsureAllRolled()
    {
        if (items != null)
            foreach (var it in items) if (!IsEmpty(it)) it.EnsureRolled();
        if (equipment != null)
            for (int i = 0; i < equipment.Count; i++)
                if (!IsEmpty(equipment[i])) equipment[i].EnsureRolled();
    }

    void NotifyChanged() => OnChanged?.Invoke();

    // --- Add items to inventory (now rolled) ---
    public bool TryAddItemData(ItemData data)
    {
        if (data == null || data.icon == null) return false;
        if (items.Count >= capacity) return false;

        items.Add(SimpleItem.CreateRolled(data)); // <-- roll once here
        NotifyChanged();
        return true;
    }

    // --- Equip to the matching slot (unchanged externally) ---
    public bool TryEquipToMatchingSlot(int invIndex)
    {
        if (invIndex < 0 || invIndex >= items.Count) return false;
        var it = items[invIndex];
        if (IsEmpty(it)) return false;

        it.EnsureRolled(); // safety

        int target = FindSlotIndex(it.data.equipSlot);
        if (target < 0) return false;
        if (!IsEmpty(equipment[target])) return false;

        equipment[target] = it;
        items.RemoveAt(invIndex);
        NotifyChanged();
        return true;
    }

    int FindSlotIndex(EquipSlot slot)
    {
        if (slotOrder == null) return -1;
        for (int i = 0; i < slotOrder.Length; i++)
            if (slotOrder[i] == slot) return i;
        return -1;
    }

    public bool MoveEquipmentIndexToInventoryFirstEmpty(int eqIndex)
    {
        if (eqIndex < 0 || eqIndex >= equipment.Count) return false;
        var itm = equipment[eqIndex];
        if (IsEmpty(itm)) return false;
        if (items.Count >= capacity) return false;

        items.Add(itm);
        equipment[eqIndex] = null;
        NotifyChanged();
        return true;
    }

    // Chest -> Inventory
    public bool MoveFromChestToInventoryFirstEmpty(ChestContainer chest, int chestIndex)
    {
        if (!chest) return false;
        if (chestIndex < 0 || chestIndex >= chest.items.Count) return false;
        if (items.Count >= capacity) return false;

        var it = chest.items[chestIndex];
        if (IsEmpty(it)) return false;

        it.EnsureRolled(); // in case chest added raw items

        items.Add(it);
        chest.items.RemoveAt(chestIndex);

        NotifyChanged();
        chest.OnChanged?.Invoke();
        return true;
    }

    // Back-compat
    public bool MoveInventoryIndexToEquipmentFirstEmpty(int invIndex) => TryEquipToMatchingSlot(invIndex);
}
