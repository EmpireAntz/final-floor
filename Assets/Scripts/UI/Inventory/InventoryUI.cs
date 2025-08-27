using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;      // NEW input system
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

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
    public Sprite defaultEquipPlaceholder;
    public Sprite weaponPlaceholder;   // EquipSlot.HandRight
    public Sprite headPlaceholder;     // EquipSlot.Head
    public Sprite chestPlaceholder;    // EquipSlot.Chest
    public Sprite feetPlaceholder;     // EquipSlot.Feet
    public Color  placeholderTint = new Color(1f,1f,1f,0.35f);

    [Header("Tooltip (Inspector-wired)")]
    public RectTransform tooltipRoot;     // Panel under your InventoryUI Canvas
    public TMP_Text      tooltipLabel;    // TMP text inside the tooltip
    public Vector2       tooltipOffset = new Vector2(16,-16);
    public bool          tooltipFollowCursor = true;

    [Header("Tooltip Formatting (editable)")]
    public bool showName = true;
    [TextArea] public string nameFormat   = "<b>{Name}</b>\n";
    public bool roundValues = true;
    public bool hideZeroValues = true;
    [TextArea] public string damageFormat = "Damage: <color=#50FF50>+{Damage}</color>\n";
    [TextArea] public string healthFormat = "Health: <color=#50B0FF>+{Health}</color>\n";

    [Header("Debug Seed (optional)")]
    public bool   enableDebugSeed = false;
    public ItemData debugTestItem;

    // --- internals ---
    Canvas _canvas;
    RectTransform _canvasRT;
    CanvasGroup _tipCG;
    Vector2 _lastPointerPos;            // NEW: cached pointer position (Input System)

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();

        if (enableDebugSeed && Application.isPlaying && inventory != null &&
            inventory.items.Count == 0 && debugTestItem != null)
            inventory.TryAddItemData(debugTestItem);

        if (inventory) inventory.OnChanged += OnInventoryChanged;

        SetupTooltip();
    }

    void OnDestroy()
    {
        if (inventory) inventory.OnChanged -= OnInventoryChanged;
    }

    void Update()
    {
        // open/close
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();

        // follow cursor while tooltip visible (NEW input system)
        if (tooltipFollowCursor && _tipCG != null && _tipCG.alpha > 0.001f)
        {
            if (Mouse.current != null)
                _lastPointerPos = Mouse.current.position.ReadValue();
            MoveTooltip(_lastPointerPos);
        }
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
        else HideTooltip();
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

        // Equipment
        if (equipGridParent)
        {
            Clear(equipGridParent);
            for (int i = 0; i < inventory.equipmentCapacity; i++)
                BuildSlot(equipGridParent, ContainerType.Equipment, i, inventory.equipment[i]);
        }

        HideTooltip(); // ensure no stale tooltip after rebuild
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

        // --- Tooltip bindings (no PointerMove; we follow in Update) ---
        var trigger = go.GetComponent<EventTrigger>();
        if (!trigger) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        if (!empty) // only bind hover for real items
        {
            AddTrigger(trigger, EventTriggerType.PointerEnter, e =>
            {
                var p = (PointerEventData)e;
                _lastPointerPos = p.position; // seed cached position
                ShowTooltip(BuildTooltipText(item), p.position);
            });

            AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideTooltip());
        }
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

    // ===================== Tooltip internals =====================

    void SetupTooltip()
    {
        if (!tooltipRoot || !tooltipLabel)
        {
            Debug.LogWarning("[InventoryUI] Tooltip references not set. No tooltips will be shown.");
            return;
        }

        _canvas   = tooltipRoot.GetComponentInParent<Canvas>();
        _canvasRT = _canvas ? _canvas.GetComponent<RectTransform>() : null;
        _tipCG    = tooltipRoot.GetComponent<CanvasGroup>();
        if (!_tipCG) _tipCG = tooltipRoot.gameObject.AddComponent<CanvasGroup>();

        // invisible & non-blocking
        _tipCG.alpha = 0f;
        _tipCG.interactable   = false;
        _tipCG.blocksRaycasts = false;

        var img = tooltipRoot.GetComponent<Image>();
        if (img) img.raycastTarget = false;
        tooltipLabel.raycastTarget = false;
    }

    void ShowTooltip(string text, Vector2 screenPos)
    {
        if (!_tipCG || tooltipLabel == null || string.IsNullOrEmpty(text)) return;
        tooltipLabel.text = text;
        _tipCG.alpha = 1f;
        MoveTooltip(screenPos);
    }

    void MoveTooltip(Vector2 screenPos)
    {
        if (_canvasRT == null || tooltipRoot == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT,
            screenPos + tooltipOffset,
            _canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null,
            out var lp
        );

        // clamp to canvas
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        Vector2 size = tooltipRoot.sizeDelta;
        Vector2 half = _canvasRT.rect.size * 0.5f;
        lp.x = Mathf.Clamp(lp.x, -half.x, half.x - size.x);
        lp.y = Mathf.Clamp(lp.y, -half.y + size.y, half.y);

        tooltipRoot.anchoredPosition = lp;
    }

    void HideTooltip()
    {
        if (_tipCG) _tipCG.alpha = 0f;
    }

    void AddTrigger(EventTrigger t, EventTriggerType type, System.Action<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
        t.triggers.Add(entry);
    }

    string BuildTooltipText(SimpleItem it)
    {
        if (Inventory.IsEmpty(it)) return "";
        var d = it.data;
        string F(float v) => roundValues ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##");

        var sb = new StringBuilder(128);
        if (showName && !string.IsNullOrEmpty(d.displayName))
            sb.Append(nameFormat.Replace("{Name}", d.displayName));

        if (!hideZeroValues || Mathf.Abs(d.addDamage) > 0.0001f)
            sb.Append(damageFormat.Replace("{Damage}", F(d.addDamage)));

        if (!hideZeroValues || Mathf.Abs(d.addMaxHealth) > 0.0001f)
            sb.Append(healthFormat.Replace("{Health}", F(d.addMaxHealth)));

        return sb.ToString().TrimEnd('\n', '\r', ' ');
    }
}
