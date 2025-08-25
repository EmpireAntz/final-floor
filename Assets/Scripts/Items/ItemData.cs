using UnityEngine;

public enum ItemCategory { Misc, Weapon, Armor, Consumable, KeyItem }

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    public string id;              // "sword_basic" (optional but useful later)
    public string displayName;     // "Sword"
    public Sprite icon;            // shown in inventory
    public ItemCategory category = ItemCategory.Misc;
}
