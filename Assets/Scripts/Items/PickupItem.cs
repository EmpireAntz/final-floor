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
            Debug.LogWarning("PickupItem missing ItemData or its icon.");
            return;
        }

        Inventory inv = interactor.GetComponentInParent<Inventory>();
        if (!inv) inv = Object.FindObjectOfType<Inventory>();
        if (!inv) { Debug.LogWarning("No Inventory found on interactor or scene."); return; }

        bool added = inv.TryAddItemData(itemData);
        if (!added)
        {
            Debug.Log("Inventory full. Could not pick up.");
            return;
        }

        if (autoEquipIfPossible)
        {
            int newIdx = inv.items.Count - 1;
            inv.MoveInventoryIndexToEquipmentFirstEmpty(newIdx);
        }

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

        var ui = Object.FindObjectOfType<InventoryUI>();
        if (ui && ui.panel && ui.panel.activeSelf) ui.RefreshAll();

        if (destroyOnPickUp) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
