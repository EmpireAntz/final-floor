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

    [Header("Slot Styling (optional)")]
    public Color filledBgColor = Color.white;
    public Color emptyBgColor  = Color.white;

    [Header("Equipment Placeholders")]
    public Sprite defaultEquipPlaceholder;      // shown if specific one not set
    public Sprite weaponPlaceholder;            // EquipSlot.HandRight
    public Sprite headPlaceholder;              // EquipSlot.Head
    public Sprite chestPlaceholder;             // EquipSlot.Chest
    public Sprite feetPlaceholder;              // EquipSlot.Feet
    public Color  placeholderTint = new Color(1f,1f,1f,0.35f);

    [Header("Debug Seed (optional)")]
    public bool enableDebugSeed = false;
    public ItemData debugTestItem;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();

        if (enableDebugSeed && Application.isPlaying && inventory != null &&
            inventory.items.Count == 0 && debugTestItem != null)
            inventory.TryAddItemData(debugTestItem);

        if (inventory) inventory.OnChanged += OnInventoryChanged;
    }

    void OnDestroy()
    {
        if (inventory) inventory.OnChanged -= OnInventoryChanged;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();
    }

    void OnInventoryChanged()
    {
        if (panel && panel.activeSelf) RefreshAll();
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

        // Inventory grid
        if (invGridParent)
        {
            Clear(invGridParent);
            for (int i = 0; i < inventory.items.Count; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, inventory.items[i]);
            for (int i = inventory.items.Count; i < inventory.capacity; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, null);
        }

        // Equipment grid
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

        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var bg  = go.GetComponent<Image>()  ?? go.AddComponent<Image>();
        bg.raycastTarget = true;

        // ensure Icon child exists
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
        Sprite sprite = null;

        // real item icon if present
        if (!empty) sprite = item.data.icon;

        // equipment placeholder if empty
        if (container == ContainerType.Equipment && sprite == null)
            sprite = GetEquipPlaceholderForIndex(index);

        iconImg.sprite = sprite;
        iconImg.enabled = (sprite != null);
        iconImg.preserveAspect = true;

        // tint placeholders a bit so real items pop
        bool isPlaceholder = (container == ContainerType.Equipment) && empty && sprite != null;
        iconImg.color = isPlaceholder ? placeholderTint : Color.white;

        // optional bg color
        bg.color = empty ? emptyBgColor : filledBgColor;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSlotClicked(container, index));
    }

    // Pick the right placeholder per equipment slot
    Sprite GetEquipPlaceholderForIndex(int index)
    {
        if (inventory == null || inventory.slotOrder == null ||
            index < 0 || index >= inventory.slotOrder.Length)
            return defaultEquipPlaceholder;

        switch (inventory.slotOrder[index])
        {
            case EquipSlot.HandRight: return weaponPlaceholder ? weaponPlaceholder : defaultEquipPlaceholder;
            case EquipSlot.Head:      return headPlaceholder   ? headPlaceholder   : defaultEquipPlaceholder;
            case EquipSlot.Chest:     return chestPlaceholder  ? chestPlaceholder  : defaultEquipPlaceholder;
            case EquipSlot.Feet:      return feetPlaceholder   ? feetPlaceholder   : defaultEquipPlaceholder;
            default:                  return defaultEquipPlaceholder;
        }
    }

    void OnSlotClicked(ContainerType container, int index)
    {
        if (!inventory) return;

        if (container == ContainerType.Inventory)
        {
            if (index >= inventory.items.Count) return; // clicked padded empty
            bool moved = inventory.TryEquipToMatchingSlot(index);
            if (!moved) Debug.Log("No matching free slot for this item.");
            else RefreshAll();
        }
        else
        {
            bool moved = inventory.MoveEquipmentIndexToInventoryFirstEmpty(index);
            if (!moved) Debug.Log("Inventory is full.");
            if (moved) RefreshAll();
        }
    }
}
