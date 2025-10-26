using UnityEngine;

public enum ItemKind { Consumable, Weapon, Armor, Chip, Ability, Misc }
public enum EquipSlotKind { None, Weapon, Armor, Chip, Ability }

[CreateAssetMenu(menuName = "EmptySkulls/Item", fileName = "NewItem")]
public class Item : ScriptableObject
{
    [Header("Display")]
    public string displayName = "Item";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Behavior")]
    public ItemKind kind = ItemKind.Consumable;
    public bool isEquippable = false;
    public EquipSlotKind equipSlot = EquipSlotKind.None; // Which equipment slot it fits

    [Header("Stat Bonuses (while equipped)")]
    public int bonusATT;
    public int bonusDEF;
    public int bonusSPD;
    public int bonusDEX;
    public int bonusVIT;
    public int bonusWIS;
    public int bonusLUCK;
    public int bonusMaxHP;
    public int bonusMaxMP;

    [Header("On-Use Effects (for Consumables, etc.)")]
    public ItemEffect[] onUseEffects;

    // Doesnt work for stacking currently
}
