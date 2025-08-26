// ItemData.cs
using UnityEngine;

public enum ItemCategory { Misc, Weapon, Armor, Consumable, KeyItem }
public enum EquipSlot     { None, HandRight, HandLeft, Head, Chest, Legs, Feet, Back }

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName = "Item";
    public Sprite icon;
    public ItemCategory category = ItemCategory.Misc;

    [Header("Equipping")]
    public EquipSlot equipSlot = EquipSlot.None;

    [Header("Stat Bonuses")]
    [Tooltip("Extra damage the item grants when equipped (use on weapons).")]
    public float addDamage = 0f;

    [Tooltip("Extra max health the item grants when equipped (use on armor).")]
    public float addMaxHealth = 0f;


    // Weapons (hand-held)
    public GameObject heldPrefab;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    // Armor (skinned to skeleton)
    public GameObject skinnedPrefab; // set this for helmets/chest/boots
}
