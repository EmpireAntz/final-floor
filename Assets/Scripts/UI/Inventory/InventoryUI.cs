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
    public Sprite debugTestSprite; // drag any sprite to auto-seed one item at play

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();

        // seed one test item if empty, for quick visual testing
        if (Application.isPlaying && inventory != null && inventory.items.Count == 0 && debugTestSprite != null)
            inventory.AddTestItem(debugTestSprite);
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

        // Inventory
        if (invGridParent)
        {
            Clear(invGridParent);
            for (int i = 0; i < inventory.items.Count; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, inventory.items[i]);

            for (int i = inventory.items.Count; i < inventory.capacity; i++)
                BuildSlot(invGridParent, ContainerType.Inventory, i, null);
        }

        // Equipment (fixed-size)
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

        // root needs Button + Image
        var btn = go.GetComponent<Button>();
        if (!btn) btn = go.AddComponent<Button>();
        var bg = go.GetComponent<Image>();
        if (!bg) { bg = go.AddComponent<Image>(); bg.color = new Color(0,0,0,0); }
        bg.raycastTarget = true;

        // find Icon image (child named "Icon" or first child Image)
        Image iconImg = null;
        var iconTf = go.transform.Find("Icon");
        if (iconTf) iconImg = iconTf.GetComponent<Image>();
        if (!iconImg)
        {
            var imgs = go.GetComponentsInChildren<Image>(true);
            foreach (var im in imgs) { if (im.gameObject != go) { iconImg = im; break; } }
        }

        bool empty = Inventory.IsEmpty(item);

        if (iconImg)
        {
            // stretch to fill slot
            var rt = iconImg.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            iconImg.sprite = empty ? null : item.icon;
            iconImg.enabled = !empty && item.icon != null;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false; // let Button receive clicks
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSlotClicked(container, index));
    }

    // click: Inventory -> Equipment (first empty), Equipment -> Inventory (first empty)
    void OnSlotClicked(ContainerType container, int index)
    {
        if (!inventory) return;

        bool moved = false;
        if (container == ContainerType.Inventory)
        {
            if (index >= inventory.items.Count) return; // clicked padded empty
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
