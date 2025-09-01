using UnityEngine;

public enum ItemCategory { Misc, Weapon, Armor, Consumable, KeyItem }
public enum EquipSlot     { None, HandRight, HandLeft, Head, Chest, Legs, Feet, Back }
public enum ItemTier      { Tier1 = 1, Tier2 = 2, Tier3 = 3 }

[System.Serializable]
public struct StatRange
{
    public float min;
    public float max;

    public float Roll() => Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
}

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName = "Item";
    public Sprite icon;
    public ItemCategory category = ItemCategory.Misc;
    public ItemTier tier = ItemTier.Tier1;

    [Header("Equipping")]
    public EquipSlot equipSlot = EquipSlot.None;

    [Header("Stat Ranges (roll once on spawn)")]
    public StatRange damage;           // e.g. 8..12 for swords
    public StatRange maxHealth;        // e.g. 50..80 for chest armor
    public StatRange maxStamina;       // e.g. 0..10 if you want stamina items
    [Tooltip("Percent values (e.g. 0..5 means 0% to +5%)")]
    public StatRange defensePercent;
    [Tooltip("Percent values (e.g. 0..5 means 0% to +5%)")]
    public StatRange critChancePercent;

    // Weapons (hand-held)
    [Header("Held Prefab (Weapons)")]
    public GameObject heldPrefab;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    // Armor (skinned to skeleton)
    [Header("Skinned Prefab (Armor)")]
    public GameObject skinnedPrefab;
}
