using UnityEngine;

public class ChestInteractable : Interactable
{
    [Header("Refs")]
    [SerializeField] private ChestContainer container;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Loot Tables (weight 70/20/10)")]
    public ItemData[] tier1; // 70%
    public ItemData[] tier2; // 20%
    public ItemData[] tier3; // 10%

    [Header("Settings")]
    public bool generateOnFirstOpen = true;
    bool _generated;

    void Reset()
    {
        if (string.IsNullOrEmpty(prompt)) prompt = "Open Chest";
        container = GetComponent<ChestContainer>();
    }

    void Awake()
    {
        if (!container) container = GetComponent<ChestContainer>() ?? gameObject.AddComponent<ChestContainer>();
        if (string.IsNullOrEmpty(prompt)) prompt = "Open Chest";
    }

    public override void Interact(GameObject user)
    {
        if (!_generated && generateOnFirstOpen)
        {
            GenerateLoot(container.capacity);
            _generated = true;
        }

        if (!inventoryUI) inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI) inventoryUI.OpenChest(container);
    }

    // ---------- loot ----------
    void GenerateLoot(int count)
    {
        container.items.Clear();
        count = Mathf.Clamp(count, 1, container.capacity);

        for (int i = 0; i < count; i++)
        {
            var data = DrawWeighted();
            if (data) container.items.Add(new SimpleItem { data = data });
        }

        container.OnChanged?.Invoke();
    }

    ItemData DrawWeighted()
    {
        float r = Random.value;
        ItemData pick =
            (r < 0.70f) ? Pick(tier1) :
            (r < 0.90f) ? Pick(tier2) : Pick(tier3);

        return pick ?? Pick(tier1) ?? Pick(tier2) ?? Pick(tier3);
    }

    static ItemData Pick(ItemData[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }
}
