using UnityEngine;

[System.Serializable]
public class HideRule {
    public EquipSlot slot = EquipSlot.Head;
    public GameObject[] targets; // which objects to toggle
    public bool invert = false;  // if true, show when equipped / hide when not
}

public class MultiHideOnEquip : MonoBehaviour
{
    public Inventory inventory;      // auto-finds if left empty
    public HideRule[] rules;

    void Awake() {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
    }
    void OnEnable()  { if (inventory) inventory.OnChanged += ApplyAll; ApplyAll(); }
    void OnDisable() { if (inventory) inventory.OnChanged -= ApplyAll; }

    void ApplyAll() {
        if (inventory == null || inventory.slotOrder == null || rules == null) return;
        foreach (var r in rules) {
            if (r == null || r.targets == null) continue;
            bool equipped = HasItemInSlot(r.slot);
            bool show = r.invert ? equipped : !equipped;
            foreach (var t in r.targets) if (t) t.SetActive(show);
        }
    }

    bool HasItemInSlot(EquipSlot s) {
        int ix = System.Array.IndexOf(inventory.slotOrder, s);
        if (ix < 0 || ix >= inventory.equipment.Count) return false;
        var it = inventory.equipment[ix];
        return !Inventory.IsEmpty(it);
    }
}
