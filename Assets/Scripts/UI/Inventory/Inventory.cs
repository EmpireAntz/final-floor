using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleItem
{
    public ItemData data;  // name/icon/equipSlot/etc.
}

public enum ContainerType { Inventory, Equipment }

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    public int capacity = 20;
    public List<SimpleItem> items = new List<SimpleItem>();

    [Header("Equipment")]
    public EquipSlot[] slotOrder = new[] {
        EquipSlot.HandRight, EquipSlot.Head, EquipSlot.Chest, EquipSlot.Feet
    };
    
    [Header("Equipment Placeholders")]
    public Sprite defaultEquipPlaceholder;      // used if a specific one isn't set
    public Sprite weaponPlaceholder;            // HandRight
    public Sprite headPlaceholder;              // Head
    public Sprite chestPlaceholder;             // Chest
    public Sprite feetPlaceholder;              // Feet
    public Color placeholderTint = new Color(1f,1f,1f,0.35f); // slight fade

    public int equipmentCapacity = 4;
    public List<SimpleItem> equipment = new List<SimpleItem>();

    public System.Action OnChanged;

    // NOTE: icon-less items count as empty with this check
    public static bool IsEmpty(SimpleItem it) => it == null || it.data == null || it.data.icon == null;

    void OnValidate()
    {
        equipmentCapacity = (slotOrder != null && slotOrder.Length > 0) ? slotOrder.Length : 4;
    }

    void Awake()
    {
        // Ensure equipment list matches capacity
        if (equipment == null) equipment = new List<SimpleItem>(equipmentCapacity);
        if (equipment.Count > equipmentCapacity)
            equipment.RemoveRange(equipmentCapacity, equipment.Count - equipmentCapacity);
        while (equipment.Count < equipmentCapacity) equipment.Add(null);

        if (items == null) items = new List<SimpleItem>();
        if (items.Count > capacity) items.RemoveRange(capacity, items.Count - capacity);
    }

    void NotifyChanged() => OnChanged?.Invoke();

    // -------- Add --------
    public bool TryAddItemData(ItemData data)
    {
        if (data == null || data.icon == null) return false;
        if (items.Count >= capacity) return false;
        items.Add(new SimpleItem { data = data });
        NotifyChanged();
        return true;
    }

    // -------- Equip (slot-specific) --------
    public bool TryEquipToMatchingSlot(int invIndex)
    {
        if (invIndex < 0 || invIndex >= items.Count) return false;
        var it = items[invIndex];
        if (IsEmpty(it)) return false;

        int target = FindSlotIndex(it.data.equipSlot);
        if (target < 0) return false;                 // item has no valid slot
        if (!IsEmpty(equipment[target])) return false; // slot already taken

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

    // -------- Unequip (back to inventory first empty) --------
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

    // (Optional) debug
    public void DebugPrintInventory()
    {
        for (int i = 0; i < items.Count; i++)
            Debug.Log($"Inventory[{i}] = {items[i]?.data?.displayName ?? "(null)"}");
        for (int i = 0; i < equipment.Count; i++)
            Debug.Log($"Equipment[{i}] ({slotOrder[i]}) = {equipment[i]?.data?.displayName ?? "(empty)"}");
    }

    // Back-compat for older code (now routes to slot-matching equip)
    public bool MoveInventoryIndexToEquipmentFirstEmpty(int invIndex)
    {
        return TryEquipToMatchingSlot(invIndex);
    }


}
