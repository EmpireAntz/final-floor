using UnityEngine;
using UnityEngine.InputSystem;  // new Input System
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject panel;       // your inventory Panel (on the InventoryUI Canvas)
    public Transform gridParent;   // the Grid object under the panel
    public GameObject slotPrefab;  // SlotUI prefab (Image + TMP child)
    public Inventory inventory; // drag the Player's SimpleInventory here

    [Header("Behavior")]
    public bool pauseWhenOpen = false;   // optional

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (!panel) return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);

        // cursor & pause
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
        if (pauseWhenOpen) Time.timeScale = show ? 0f : 1f;

        if (show) Refresh();
    }

    public void Refresh()
    {
        if (!gridParent || !slotPrefab || !inventory) return;

        // clear existing
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        // fill with items
        foreach (var it in inventory.items)
        {
           var go = Instantiate(slotPrefab, gridParent);

    // Get the child "Icon" to set sprite there
            var iconTf = go.transform.Find("Icon");
            if (iconTf != null)
            {
            var iconImg = iconTf.GetComponent<UnityEngine.UI.Image>();
            if (iconImg)
            {
            iconImg.sprite = it.icon;
            iconImg.enabled = (it.icon != null); // hide if no icon
            iconImg.preserveAspect = true;
            }
    }
            var qtyText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (qtyText) qtyText.text = it.quantity > 1 ? it.quantity.ToString() : "";
        }

        // pad empty slots to capacity (optional)
        int toAdd = Mathf.Max(0, inventory.capacity - inventory.items.Count);
        for (int i = 0; i < toAdd; i++)
            Instantiate(slotPrefab, gridParent); // empty slot (no icon/qty)
    }
}
