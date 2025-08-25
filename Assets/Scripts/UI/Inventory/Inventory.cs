using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleItem
{
    public Sprite icon;   // no quantity anymore
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

    // empty if no object OR its icon is null
    public static bool IsEmpty(SimpleItem it) => it == null || it.icon == null;

    void Awake()
    {
        if (equipment == null) equipment = new List<SimpleItem>(equipmentCapacity);
        if (equipment.Count > equipmentCapacity)
            equipment.RemoveRange(equipmentCapacity, equipment.Count - equipmentCapacity);
        while (equipment.Count < equipmentCapacity) equipment.Add(null);

        if (items == null) items = new List<SimpleItem>();
        if (items.Count > capacity) items.RemoveRange(capacity, items.Count - capacity);
    }

    // quick test helper
    public void AddTestItem(Sprite icon)
    {
        if (items.Count >= capacity) return;
        items.Add(new SimpleItem { icon = icon });
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
