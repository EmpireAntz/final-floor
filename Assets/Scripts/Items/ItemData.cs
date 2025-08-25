using UnityEngine;

public enum ItemCategory { Misc, Weapon, Armor, Consumable, KeyItem }
public enum EquipSlot     { None, HandRight, HandLeft, Head, Body, Back }

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // e.g. "sword_basic"
    public string displayName = "Item";
    public Sprite icon;
    public ItemCategory category = ItemCategory.Misc;

    [Header("Equipping")]
    public EquipSlot equipSlot = EquipSlot.None;
    public GameObject heldPrefab;     // the model shown in the hand
    public Vector3 localPosition;     // offsets when parented to the hand
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;
}
