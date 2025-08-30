using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
    public Color emptyBgColor = Color.white;

    [Header("Equipment Placeholders")]
    public Sprite defaultEquipPlaceholder;
    public Sprite weaponPlaceholder;   // EquipSlot.HandRight
    public Sprite headPlaceholder;     // EquipSlot.Head
    public Sprite chestPlaceholder;    // EquipSlot.Chest
    public Sprite feetPlaceholder;     // EquipSlot.Feet
    public Color placeholderTint = new Color(1f, 1f, 1f, 0.35f);

    [Header("Tooltip (Inspector-wired)")]
    public RectTransform tooltipRoot;     // Panel under your InventoryUI Canvas
    public TMP_Text tooltipLabel;    // TMP text inside the tooltip

    [Header("Tooltip Layout")]
    public bool tooltipFollowCursor = true;
    public Vector2 tooltipOffset = new Vector2(16, -16);
    public Vector2 tooltipPivot = new Vector2(0f, 1f); // top-left
    [Range(0.25f, 3f)]
    public float tooltipScale = 1f;

    [Header("Tooltip Colors")]
    public Color nameColor = Color.white;
    public Color damageColor = new Color(0.31f, 1f, 0.31f, 1f);
    public Color healthColor = new Color(0.31f, 0.69f, 1f, 1f);

    [Header("Tier Colors")]
    public Color tier1Color = new Color(0.80f, 0.80f, 0.80f, 1f);
    public Color tier2Color = new Color(0.40f, 0.85f, 1.00f, 1f);
    public Color tier3Color = new Color(1.00f, 0.84f, 0.31f, 1f);

    [Header("Tooltip Formatting (editable)")]
    public bool showName = true;
    public bool showTier = true;
    public bool roundValues = true;
    public bool hideZeroValues = true;

    [TextArea] public string nameFormat = "<color={NameColor}><b>{Name}</b></color>\n";
    [TextArea] public string tierFormat = "Tier: <color={TierColor}>{Tier}</color>\n";
    [TextArea] public string damageFormat = "Damage: <color={DamageColor}>+{Damage}</color>\n";
    [TextArea] public string healthFormat = "Health: <color={HealthColor}>+{Health}</color>\n";
    public bool tierAsRoman = true; // I, II, III or 1,2,3

    [Header("Debug Seed (optional)")]
    public bool enableDebugSeed = false;
    public ItemData debugTestItem;

    // --- internals ---
    Canvas _canvas;
    RectTransform _canvasRT;
    CanvasGroup _tipCG;
    Vector2 _lastPointerPos;

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
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();

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
        Cursor.visible = show;
        if (pauseWhenOpen) Time.timeScale = show ? 0f : 1f;

        if (show) RefreshAll();
        else HideTooltip();
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

        HideTooltip();
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
        var bg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
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

        if (!empty) sprite = item.data.icon;

        if (container == ContainerType.Equipment && sprite == null)
            sprite = GetEquipPlaceholderForIndex(index);

        iconImg.sprite = sprite;
        iconImg.enabled = (sprite != null);
        iconImg.preserveAspect = true;

        bool isPlaceholder = (container == ContainerType.Equipment) && empty && sprite != null;
        iconImg.color = isPlaceholder ? placeholderTint : Color.white;
        bg.color = empty ? emptyBgColor : filledBgColor;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSlotClicked(container, index));

        // Tooltip bindings
        var trigger = go.GetComponent<EventTrigger>();
        if (!trigger) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        if (!empty) // only hover real items
        {
            AddTrigger(trigger, EventTriggerType.PointerEnter, e =>
            {
                var p = (PointerEventData)e;
                _lastPointerPos = p.position;
                ShowTooltip(BuildTooltipText(item), p.position);
            });

            AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideTooltip());
        }
    }

    // ---- Placeholders ----
    Sprite GetEquipPlaceholderForIndex(int index)
    {
        if (inventory == null || inventory.slotOrder == null ||
            index < 0 || index >= inventory.slotOrder.Length)
            return defaultEquipPlaceholder;

        switch (inventory.slotOrder[index])
        {
            case EquipSlot.HandRight: return weaponPlaceholder ? weaponPlaceholder : defaultEquipPlaceholder;
            case EquipSlot.Head: return headPlaceholder ? headPlaceholder : defaultEquipPlaceholder;
            case EquipSlot.Chest: return chestPlaceholder ? chestPlaceholder : defaultEquipPlaceholder;
            case EquipSlot.Feet: return feetPlaceholder ? feetPlaceholder : defaultEquipPlaceholder;
            default: return defaultEquipPlaceholder;
        }
    }

    void OnSlotClicked(ContainerType container, int index)
    {
        if (!inventory) return;

        if (container == ContainerType.Inventory)
        {
            if (index >= inventory.items.Count) return;
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

        _canvas = tooltipRoot.GetComponentInParent<Canvas>();
        _canvasRT = _canvas ? _canvas.GetComponent<RectTransform>() : null;
        _tipCG = tooltipRoot.GetComponent<CanvasGroup>();
        if (!_tipCG) _tipCG = tooltipRoot.gameObject.AddComponent<CanvasGroup>();

        // layout / transform from inspector
        tooltipRoot.pivot = tooltipPivot;
        tooltipRoot.localScale = Vector3.one * Mathf.Max(0.01f, tooltipScale);

        // invisible & non-blocking
        _tipCG.alpha = 0f;
        _tipCG.interactable = false;
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

        // apply current scale in case you change it at runtime
        tooltipRoot.localScale = Vector3.one * Mathf.Max(0.01f, tooltipScale);
        tooltipRoot.pivot = tooltipPivot;

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

        // measure & clamp
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

        // color hex tokens
        string nameHex = ToHex(nameColor);
        string dmgHex = ToHex(damageColor);
        string hpHex = ToHex(healthColor);
        string tierHex = ToHex(GetTierColor(d));

        var sb = new StringBuilder(128);

        if (showName && !string.IsNullOrEmpty(d.displayName))
        {
            string line = nameFormat
                .Replace("{NameColor}", nameHex)
                .Replace("{Name}", d.displayName);
            sb.Append(line);
        }

        if (showTier)
        {
            string tStr = tierAsRoman ? TierToRoman(d) : TierToNumber(d);
            string line = tierFormat
                .Replace("{TierColor}", tierHex)
                .Replace("{Tier}", tStr);
            sb.Append(line);
        }

        if (!hideZeroValues || Mathf.Abs(d.addDamage) > 0.0001f)
        {
            string line = damageFormat
                .Replace("{DamageColor}", dmgHex)
                .Replace("{Damage}", F(d.addDamage));
            sb.Append(line);
        }

        if (!hideZeroValues || Mathf.Abs(d.addMaxHealth) > 0.0001f)
        {
            string line = healthFormat
                .Replace("{HealthColor}", hpHex)
                .Replace("{Health}", F(d.addMaxHealth));
            sb.Append(line);
        }

        return sb.ToString().TrimEnd('\n', '\r', ' ');
    }

    static string ToHex(Color c)
    {
        Color32 c32 = c;
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
    }

    static string TierToRoman(ItemData d)
    {
        // expects enum ItemTier { Tier1=1, Tier2=2, Tier3=3 }
        int n = (int)d.tier;
        return n switch { 1 => "I", 2 => "II", 3 => "III", _ => n.ToString() };
    }

    static string TierToNumber(ItemData d)
    {
        int n = (int)d.tier;
        return n.ToString();
    }

    Color GetTierColor(ItemData d)
    {
        switch (d.tier)
        {
            case ItemTier.Tier1: return tier1Color;
            case ItemTier.Tier2: return tier2Color;
            case ItemTier.Tier3: return tier3Color;
            default:             return tier1Color; // fallback
        }
    }

    
    
}
