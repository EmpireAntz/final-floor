// Inventory.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleItem
{
    public ItemData data;  // scalable: name, icon, type
}

public enum ContainerType { Inventory, Equipment }

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    public int capacity = 20;
    public List<SimpleItem> items = new List<SimpleItem>();

    [Header("Equipment")]
    public int equipmentCapacity = 5;
    public List<SimpleItem> equipment = new List<SimpleItem>();

    // empty if no object OR its data/icon is null
    public static bool IsEmpty(SimpleItem it) => it == null || it.data == null || it.data.icon == null;

    void Awake()
    {
        if (equipment == null) equipment = new List<SimpleItem>(equipmentCapacity);
        if (equipment.Count > equipmentCapacity)
            equipment.RemoveRange(equipmentCapacity, equipment.Count - equipmentCapacity);
        while (equipment.Count < equipmentCapacity) equipment.Add(null);

        if (items == null) items = new List<SimpleItem>();
        if (items.Count > capacity) items.RemoveRange(capacity, items.Count - capacity);
    }

    // Add an item (by ItemData)
    public bool TryAddItemData(ItemData data)
    {
        if (data == null || data.icon == null) return false;
        if (items.Count >= capacity) return false;
        items.Add(new SimpleItem { data = data });
        return true;
    }

    // --- moves ---
    public bool MoveInventoryIndexToEquipmentFirstEmpty(int invIndex)
    {
        if (invIndex < 0 || invIndex >= items.Count) return false;

        int emptyEq = -1;
        for (int i = 0; i < equipmentCapacity; i++)
            if (IsEmpty(equipment[i])) { emptyEq = i; break; }

        if (emptyEq == -1) return false; // equipment full

        equipment[emptyEq] = items[invIndex];
        items.RemoveAt(invIndex); // keep compact
        return true;
    }

    public bool MoveEquipmentIndexToInventoryFirstEmpty(int eqIndex)
    {
        if (eqIndex < 0 || eqIndex >= equipment.Count) return false;
        var itm = equipment[eqIndex];
        if (IsEmpty(itm)) return false;
        if (items.Count >= capacity) return false;

        items.Add(itm);
        equipment[eqIndex] = null;
        return true;
    }
}
