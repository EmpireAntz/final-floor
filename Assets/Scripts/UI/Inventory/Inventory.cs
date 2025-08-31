using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleItem { public ItemData data; }

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

    public static bool IsEmpty(SimpleItem it) => it == null || it.data == null || it.data.icon == null;

    void OnValidate() => equipmentCapacity = (slotOrder != null && slotOrder.Length > 0) ? slotOrder.Length : 4;

    void Awake()
    {
        if (equipment == null) equipment = new List<SimpleItem>(equipmentCapacity);
        if (equipment.Count > equipmentCapacity)
            equipment.RemoveRange(equipmentCapacity, equipment.Count - equipmentCapacity);
        while (equipment.Count < equipmentCapacity) equipment.Add(null);

        if (items == null) items = new List<SimpleItem>();
        if (items.Count > capacity) items.RemoveRange(capacity, items.Count - capacity);
    }

    void NotifyChanged() => OnChanged?.Invoke();

    public bool TryAddItemData(ItemData data)
    {
        if (data == null || data.icon == null) return false;
        if (items.Count >= capacity) return false;
        items.Add(new SimpleItem { data = data });
        NotifyChanged();
        return true;
    }

    public bool TryEquipToMatchingSlot(int invIndex)
    {
        if (invIndex < 0 || invIndex >= items.Count) return false;
        var it = items[invIndex];
        if (IsEmpty(it)) return false;

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

        items.Add(it);
        chest.items.RemoveAt(chestIndex);

        NotifyChanged();
        chest.OnChanged?.Invoke();
        return true;
    }

    // Back-compat for older calls
    public bool MoveInventoryIndexToEquipmentFirstEmpty(int invIndex) => TryEquipToMatchingSlot(invIndex);
}
