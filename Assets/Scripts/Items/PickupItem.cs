// PickupItem.cs
using UnityEngine;

public class PickupItem : Interactable
{
    public ItemData itemData;
    public bool autoEquipIfPossible = false;
    public AudioClip pickupSfx;
    public bool destroyOnPickUp = true;

    private void Reset()
    {
        prompt = itemData ? $"Pick up {itemData.displayName}" : "Pick up";
    }

    public override void Interact(GameObject interactor)
    {
        if (!itemData || !itemData.icon)
        {
            Debug.LogWarning("PickupItem missing ItemData or icon.");
            return;
        }

        // Find player inventory
        Inventory inv = interactor.GetComponentInParent<Inventory>();
        if (!inv) inv = Object.FindObjectOfType<Inventory>();
        if (!inv) { Debug.LogWarning("No Inventory found."); return; }

        // Add to inventory
        bool added = inv.TryAddItemData(itemData);
        if (!added)
        {
            Debug.Log("Inventory full.");
            return;
        }

        // Optional quick auto-equip (puts the newly added item into first empty equipment slot)
        if (autoEquipIfPossible)
        {
            int newIdx = inv.items.Count - 1;
            inv.MoveInventoryIndexToEquipmentFirstEmpty(newIdx);
        }

        if (pickupSfx)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

        // Refresh UI if open
        var ui = Object.FindObjectOfType<InventoryUI>();
        if (ui && ui.panel && ui.panel.activeSelf) ui.RefreshAll();

        if (destroyOnPickUp) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
