using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleItem
{
    public Sprite icon;
    public int quantity = 1;
}
public class Inventory : MonoBehaviour
{
    public int capacity = 20;
    public List<SimpleItem> items = new List<SimpleItem>();

    // helper to add dummy items for testing
    public void AddTestItem(Sprite icon, int qty = 1)
    {
        if (items.Count >= capacity) return;
        items.Add(new SimpleItem { icon = icon, quantity = Mathf.Max(1, qty) });
    }
}
