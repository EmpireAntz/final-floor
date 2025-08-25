// InventoryUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject panel;
    public Transform invGridParent;
    public Transform equipGridParent;
    public GameObject slotPrefab;
    public Inventory inventory;

    [Header("Behavior")]
    public bool pauseWhenOpen = false;

    [Header("Debug Seed (optional)")]
    public ItemData debugTestItem; // assign an ItemData to auto-seed

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();

        if (Application.isPlaying && inventory != null && inventory.items.Count == 0 && debugTestItem != null)
            inventory.TryAddItemData(debugTestItem);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (!panel) return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = show;
        if (pauseWhenOpen) Time.timeScale = show ? 0f : 1f;

        if (show) RefreshAll();
    }

    public void RefreshAll()
    {
        if (!slotPrefab || inventory == null) return;

        if (invGridParent)
        {
            Clear(invGridParent);
            for (int i = 0; i < inventory.items.Count; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, inventory.items[i]);

            for (int i = inventory.items.Count; i < inventory.capacity; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, null);
        }

        if (equipGridParent)
        {
            Clear(equipGridParent);
            for (int i = 0; i < inventory.equipmentCapacity; i++)
                BuildSlot(equipGridParent, ContainerType.Equipment, i, inventory.equipment[i]);
        }
    }

    void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    void BuildSlot(Transform parent, ContainerType container, int index, SimpleItem item)
{
    var go = Instantiate(slotPrefab, parent);

    // background (keep prefab visuals)
    var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
    var bg  = go.GetComponent<Image>()  ?? go.AddComponent<Image>();
    bg.raycastTarget = true;  // do NOT change bg.color here

    // ensure we have a child "Icon" Image for the item sprite
    Image iconImg = go.transform.Find("Icon")?.GetComponent<Image>();
    if (!iconImg)
    {
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        iconImg = iconGO.GetComponent<Image>();
        var rt = iconImg.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    bool empty = Inventory.IsEmpty(item);

    // icon handling
    var sprite = (!empty) ? item.data.icon : null;
    iconImg.sprite = sprite;
    iconImg.enabled = sprite != null;
    iconImg.preserveAspect = true;
    iconImg.color = Color.white;
    iconImg.raycastTarget = false; // let the Button receive clicks

    // click handler
    btn.onClick.RemoveAllListeners();
    btn.onClick.AddListener(() => OnSlotClicked(container, index));
}


    void OnSlotClicked(ContainerType container, int index)
    {
        if (!inventory) return;

        bool moved = false;
        if (container == ContainerType.Inventory)
        {
            if (index >= inventory.items.Count) return;
            moved = inventory.MoveInventoryIndexToEquipmentFirstEmpty(index);
            if (!moved) Debug.Log("Equipment is full.");
        }
        else
        {
            moved = inventory.MoveEquipmentIndexToInventoryFirstEmpty(index);
            if (!moved) Debug.Log("Inventory is full.");
        }

        if (moved) RefreshAll();
    }
}
