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
    public bool IsOpen => panel && panel.activeSelf;


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

    // -------- Chest UI --------
    [Header("Chest UI")]
    public GameObject chestSectionRoot; // enable/disable this when chest is open/closed
    public Transform chestGridParent;   // grid (2x2) under chest section
    public Color chestEmptyBg = new Color(1,1,1,0.08f);
    ChestContainer _activeChest;

    // -------- Tooltip (optional) --------
    [Header("Tooltips")]
    public RectTransform tooltipRoot;
    public TMP_Text tooltipLabel;
    [Header("Tooltip Layout")]
    public bool tooltipFollowCursor = true;
    public Vector2 tooltipOffset = new Vector2(16, -16);
    public Vector2 tooltipPivot = new Vector2(0f, 1f);
    [Range(0.25f, 3f)] public float tooltipScale = 1f;
    [Header("Tooltip Colors")]
    public Color nameColor = Color.white;
    public Color damageColor = new Color(0.31f, 1f, 0.31f, 1f);
    public Color healthColor = new Color(0.31f, 0.69f, 1f, 1f);
    public Color staminaColor = new Color(0.75f, 1f, 0.5f, 1f);
    public Color defenseColor = new Color(1f, 0.85f, 0.35f, 1f);
    public Color critColor    = new Color(1f, 0.55f, 0.6f, 1f);

    [Header("Tier Colors")]
    public Color tier1Color = new Color(0.80f, 0.80f, 0.80f, 1f);
    public Color tier2Color = new Color(0.40f, 0.85f, 1.00f, 1f);
    public Color tier3Color = new Color(1.00f, 0.84f, 0.31f, 1f);
    [Header("Tooltip Formatting")]
    public bool showName = true, showTier = true, roundValues = true, hideZeroValues = true;
    [TextArea] public string nameFormat   = "<b><color={NameColor}>{Name}</color></b>\n";
    [TextArea] public string tierFormat   = "Tier: <color={TierColor}>{Tier}</color>\n";
    [TextArea] public string damageFormat = "Damage: <color={DamageColor}>+{Damage}</color>\n";
    [TextArea] public string healthFormat = "Health: <color={HealthColor}>+{Health}</color>\n";
    [TextArea] public string staminaFormat = "Stamina: <color={StaminaColor}>+{Stamina}</color>\n";
    [TextArea] public string defenseFormat = "Defense: <color={DefenseColor}>+{Defense}%</color>\n";
    [TextArea] public string critFormat    = "Crit Chance: <color={CritColor}>+{Crit}%</color>\n";

    public bool tierAsRoman = true;

    // internals
    Canvas _canvas; RectTransform _canvasRT; CanvasGroup _tipCG; Vector2 _lastPointerPos;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (chestSectionRoot) chestSectionRoot.SetActive(false);
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        if (inventory) inventory.OnChanged += OnInventoryChanged;
        SetupTooltip();
    }

    void OnDestroy()
    {
        if (inventory) inventory.OnChanged -= OnInventoryChanged;
        if (_activeChest) _activeChest.OnChanged -= OnChestChanged;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();

        if (tooltipFollowCursor && _tipCG != null && _tipCG.alpha > 0.001f)
        {
            if (Mouse.current != null) _lastPointerPos = Mouse.current.position.ReadValue();
            MoveTooltip(_lastPointerPos);
        }
    }

    void OnInventoryChanged() { if (panel && panel.activeSelf) RefreshAll(); }
    void OnChestChanged()     { if (panel && panel.activeSelf) RefreshAll(); }

    // -------- Public chest API --------
    public void OpenChest(ChestContainer chest)
    {
        if (_activeChest) _activeChest.OnChanged -= OnChestChanged;
        _activeChest = chest;
        if (_activeChest) _activeChest.OnChanged += OnChestChanged;

        if (panel) panel.SetActive(true);
        if (chestSectionRoot) chestSectionRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseWhenOpen) Time.timeScale = 0f;

        RefreshAll();
    }

    public void CloseChest()
    {
        if (chestSectionRoot) chestSectionRoot.SetActive(false);
        if (_activeChest)
        {
            _activeChest.OnChanged -= OnChestChanged;
            _activeChest = null;
        }
        HideTooltip();
    }

    // -------- Toggle full panel --------
    public void Toggle()
    {
        if (!panel) return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
        if (pauseWhenOpen) Time.timeScale = show ? 0f : 1f;

        if (show) RefreshAll();
        else { CloseChest(); HideTooltip(); }
    }

    // -------- Build all grids --------
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

        // Chest (2x2)
        if (chestSectionRoot && chestSectionRoot.activeSelf && chestGridParent)
        {
            Clear(chestGridParent);
            int cap = (_activeChest ? Mathf.Clamp(_activeChest.capacity, 1, 4) : 4);
            int count = _activeChest ? _activeChest.items.Count : 0;

            for (int i = 0; i < count; i++)
                BuildSlot(chestGridParent, ContainerType.Chest, i, _activeChest.items[i]);
            for (int i = count; i < cap; i++)
                BuildSlot(chestGridParent, ContainerType.Chest, i, null);
        }

        HideTooltip();
    }

    void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    void BuildSlot(Transform parent, ContainerType container, int index, SimpleItem item)
    {
        var go = Instantiate(slotPrefab, parent);

        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var bg  = go.GetComponent<Image>()  ?? go.AddComponent<Image>();
        bg.raycastTarget = true;

        // ensure Icon child
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
        Sprite spr = (!empty) ? item.data.icon : null;

        if (container == ContainerType.Equipment && spr == null)
            spr = GetEquipPlaceholderForIndex(index);

        iconImg.sprite = spr;
        iconImg.enabled = (spr != null);
        iconImg.preserveAspect = true;

        bool isPlaceholder = (container == ContainerType.Equipment) && empty && spr != null;
        iconImg.color = isPlaceholder ? placeholderTint : Color.white;

        Color emptyCol = (container == ContainerType.Chest) ? chestEmptyBg : emptyBgColor;
        bg.color = empty ? emptyCol : filledBgColor;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSlotClicked(container, index));

        // tooltip
        var trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        if (!empty)
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

    Sprite GetEquipPlaceholderForIndex(int index)
    {
        if (inventory == null || inventory.slotOrder == null || index < 0 || index >= inventory.slotOrder.Length)
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
        bool moved = false;

        if (container == ContainerType.Inventory)
        {
            if (index >= inventory.items.Count) return;
            moved = inventory.TryEquipToMatchingSlot(index);
            if (!moved) Debug.Log("No matching free slot for this item.");
        }
        else if (container == ContainerType.Equipment)
        {
            moved = inventory.MoveEquipmentIndexToInventoryFirstEmpty(index);
            if (!moved) Debug.Log("Inventory is full.");
        }
        else if (container == ContainerType.Chest)
        {
            if (!_activeChest) return;
            moved = inventory.MoveFromChestToInventoryFirstEmpty(_activeChest, index);
            if (!moved) Debug.Log("Inventory is full or slot empty.");
        }

        if (moved) RefreshAll();
    }

    // -------- Tooltip internals (unchanged from your version) --------
    void SetupTooltip()
    {
        if (!tooltipRoot || !tooltipLabel) return;
        _canvas = tooltipRoot.GetComponentInParent<Canvas>();
        _canvasRT = _canvas ? _canvas.GetComponent<RectTransform>() : null;
        _tipCG = tooltipRoot.GetComponent<CanvasGroup>() ?? tooltipRoot.gameObject.AddComponent<CanvasGroup>();

        tooltipRoot.pivot = tooltipPivot;
        tooltipRoot.localScale = Vector3.one * Mathf.Max(0.01f, tooltipScale);

        _tipCG.alpha = 0f; _tipCG.interactable = false; _tipCG.blocksRaycasts = false;
        var img = tooltipRoot.GetComponent<Image>(); if (img) img.raycastTarget = false;
        tooltipLabel.raycastTarget = false;
    }

    void ShowTooltip(string text, Vector2 screenPos)
    {
        if (!_tipCG || !tooltipLabel || string.IsNullOrEmpty(text)) return;
        tooltipLabel.text = text; _tipCG.alpha = 1f;
        tooltipRoot.localScale = Vector3.one * Mathf.Max(0.01f, tooltipScale);
        tooltipRoot.pivot = tooltipPivot;
        MoveTooltip(screenPos);
    }

    void MoveTooltip(Vector2 screenPos)
    {
        if (_canvasRT == null || tooltipRoot == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, screenPos + tooltipOffset,
            _canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null,
            out var lp
        );
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        Vector2 size = tooltipRoot.sizeDelta, half = _canvasRT.rect.size * 0.5f;
        lp.x = Mathf.Clamp(lp.x, -half.x, half.x - size.x);
        lp.y = Mathf.Clamp(lp.y, -half.y + size.y, half.y);
        tooltipRoot.anchoredPosition = lp;
    }

    void HideTooltip() { if (_tipCG) _tipCG.alpha = 0f; }

    void AddTrigger(EventTrigger t, EventTriggerType type, System.Action<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
        t.triggers.Add(entry);
    }

string BuildTooltipText(SimpleItem it)
{
    if (Inventory.IsEmpty(it)) return "";

    // make sure this instance has rolled values
    it.EnsureRolled();

    // pulled from THIS item instance (not template)
    float dmg = it.addDamage;
    float hp  = it.addMaxHealth;
    float st  = it.addMaxStamina;
    float df  = it.addDefensePercent;
    float cr  = it.addCritChancePercent;

    var d = it.data;

    string F(float v) => roundValues ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##");
    string P(float v) => roundValues ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#");

    // colors to hex
    string nameHex = ToHex(nameColor);
    string dmgHex  = ToHex(damageColor);
    string hpHex   = ToHex(healthColor);
    string stHex   = ToHex(staminaColor);
    string dfHex   = ToHex(defenseColor);
    string crHex   = ToHex(critColor);
    string tierHex = ToHex(GetTierColor(d));

    var sb = new StringBuilder(128);

    // Name
    if (showName && !string.IsNullOrEmpty(d.displayName))
        sb.Append(nameFormat.Replace("{NameColor}", nameHex)
                            .Replace("{Name}", d.displayName));

    // Tier
    if (showTier)
    {
        string tStr = tierAsRoman ? TierToRoman(d) : ((int)d.tier).ToString();
        sb.Append(tierFormat.Replace("{TierColor}", tierHex)
                            .Replace("{Tier}", tStr));
    }

    // Damage (instance)
    if (!hideZeroValues || Mathf.Abs(dmg) > 0.0001f)
        sb.Append(damageFormat.Replace("{DamageColor}", dmgHex)
                              .Replace("{Damage}", F(dmg)));

    // Health (instance)
    if (!hideZeroValues || Mathf.Abs(hp) > 0.0001f)
        sb.Append(healthFormat.Replace("{HealthColor}", hpHex)
                              .Replace("{Health}", F(hp)));

    // Stamina (instance)
    if (!hideZeroValues || Mathf.Abs(st) > 0.0001f)
        sb.Append(staminaFormat.Replace("{StaminaColor}", stHex)
                               .Replace("{Stamina}", F(st)));

    // Defense % (instance)
    if (!hideZeroValues || Mathf.Abs(df) > 0.0001f)
        sb.Append(defenseFormat.Replace("{DefenseColor}", dfHex)
                               .Replace("{Defense}", P(df)));

    // Crit % (instance)
    if (!hideZeroValues || Mathf.Abs(cr) > 0.0001f)
        sb.Append(critFormat.Replace("{CritColor}", crHex)
                            .Replace("{Crit}", P(cr)));

    return sb.ToString().TrimEnd('\n', '\r', ' ');
}


    static string ToHex(Color c){ Color32 x=c; return $"#{x.r:X2}{x.g:X2}{x.b:X2}{x.a:X2}"; }
    static string TierToRoman(ItemData d){ int n=(int)d.tier; return n switch {1=>"I",2=>"II",3=>"III",_=>n.ToString()}; }
    Color GetTierColor(ItemData d) => d.tier switch { ItemTier.Tier1 => tier1Color, ItemTier.Tier2 => tier2Color, ItemTier.Tier3 => tier3Color, _ => tier1Color };
}
