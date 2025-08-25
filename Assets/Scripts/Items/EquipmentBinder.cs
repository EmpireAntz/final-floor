using UnityEngine;

public class EquipmentBinder : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;
    [Tooltip("Right-hand bone (e.g., mixamorig:RightHand)")]
    public Transform rightHandBone;
    [Tooltip("Left-hand bone if needed")]
    public Transform leftHandBone;

    GameObject rightHandInstance;
    GameObject leftHandInstance;
    ItemData   rightHandItem;
    ItemData   leftHandItem;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
    }

    void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    public void Refresh()
    {
        if (!inventory) return;
        SimpleItem rightItem = FindEquipped(EquipSlot.HandRight);
        SimpleItem leftItem  = FindEquipped(EquipSlot.HandLeft);

        BindSlot(ref rightHandInstance, ref rightHandItem, rightHandBone, rightItem?.data);
        BindSlot(ref leftHandInstance,  ref leftHandItem,  leftHandBone,  leftItem?.data);
    }

    SimpleItem FindEquipped(EquipSlot slot)
    {
        for (int i = 0; i < inventory.equipment.Count; i++)
        {
            var it = inventory.equipment[i];
            if (!Inventory.IsEmpty(it) && it.data.equipSlot == slot) return it;
        }
        return null;
    }

    void BindSlot(ref GameObject currentInstance, ref ItemData currentItem, Transform parent, ItemData targetItem)
    {
        if (currentItem == targetItem) return;

        if (currentInstance) Destroy(currentInstance);
        currentInstance = null;
        currentItem = null;

        if (targetItem == null || parent == null || targetItem.heldPrefab == null) return;

        currentInstance = Instantiate(targetItem.heldPrefab, parent, false);
        currentInstance.transform.localPosition    = targetItem.localPosition;
        currentInstance.transform.localEulerAngles = targetItem.localEulerAngles;
        currentInstance.transform.localScale       = targetItem.localScale;

        var rb = currentInstance.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
        foreach (var c in currentInstance.GetComponentsInChildren<Collider>()) c.enabled = false;

        currentItem = targetItem;
    }
}
