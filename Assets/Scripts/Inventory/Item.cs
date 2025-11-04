using UnityEngine;

public enum ItemKind { Consumable, Weapon, Armor, Chip, Ability, Misc }
public enum EquipSlotKind { None, Weapon, Armor, Chip, Ability }

// simple weapon class presets
public enum WeaponClass { None, MachineGun, Sniper, Shotgun }

[CreateAssetMenu(menuName = "EmptySkulls/Item", fileName = "NewItem")]
public class Item : ScriptableObject
{
    [Header("Display")]
    public string displayName = "Item";
    [TextArea] public string description;
    public Sprite icon;
    public LootRarity rarity = LootRarity.Common;

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

    // --- Weapon-specific (optional) ---
    [Header("Weapon (optional)")]
    public WeaponClass weaponClass = WeaponClass.None;

    [Tooltip("Fire rate per level of dexterity")]
    public float dexMultiplier = 0f;

    [Tooltip("Bullets to fire per shot")]
    public int pelletsToFire = 0;

    [Tooltip("Bullet spread angle in degrees")]
    public float bulletSpread = 0f;

    [Tooltip("Whether weapon has burts firing mode")]
    public bool isBurstFire = false;

    [Tooltip("Whether weapon pierces defence")]
    public bool pierceDEF = false;

    [Tooltip("Optional projectile prefab; if left null, Shooter's wiring.projectilePrefab is used.")]
    public GameObject overrideProjectilePrefab;

    // Note: Keep weaponClass None for non-weapon items.
}