using UnityEngine;

[DisallowMultipleComponent]
public class PlayerEquipment : MonoBehaviour
{
    [Header("Equip Slots (auto if left empty)")]
    public ItemSlotUI weaponSlot;
    public ItemSlotUI armorSlot;
    public ItemSlotUI chipSlot;
    public ItemSlotUI abilitySlot;

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Debug")]
    public bool verbose = true;

    string _lastTotalsKey;

    void Awake()
    {
        // Find PlayerStats
        if (!stats)
        {
            stats = GetComponentInParent<PlayerStats>();
            if (!stats)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged) stats = tagged.GetComponent<PlayerStats>();
            }
        }

        // Auto-wire equip slots from the player's InventoryUI
        if (!weaponSlot || !armorSlot || !chipSlot || !abilitySlot)
        {
            var inv = InventoryUI.FindPlayerInventory();
            if (inv && inv.slots != null)
            {
                foreach (var s in inv.slots)
                {
                    if (!s || s.role != SlotRole.Equip) continue;
                    switch (s.equipSlot)
                    {
                        case EquipSlotKind.Weapon:  if (!weaponSlot)  weaponSlot  = s; break;
                        case EquipSlotKind.Armor:   if (!armorSlot)   armorSlot   = s; break;
                        case EquipSlotKind.Chip:    if (!chipSlot)    chipSlot    = s; break;
                        case EquipSlotKind.Ability: if (!abilitySlot) abilitySlot = s; break;
                    }
                }
            }
        }

        if (verbose)
        {
            Debug.Log($"[PlayerEquipment] Initialized → Stats={stats?.name ?? "NULL"}");
            Debug.Log($"[PlayerEquipment] Slots => Weapon={NameOf(weaponSlot)} Armor={NameOf(armorSlot)} Chip={NameOf(chipSlot)} Ability={NameOf(abilitySlot)}");
        }
    }

    void LateUpdate()
    {
        if (!stats) return;

        int att=0, def=0, spd=0, dex=0, vit=0, wis=0, bMaxHP=0, bMaxMP=0;

        Acc(weaponSlot ? weaponSlot.item : null);
        Acc(armorSlot  ? armorSlot.item  : null);
        Acc(chipSlot   ? chipSlot.item   : null);
        Acc(abilitySlot? abilitySlot.item: null);

        stats.SetBonus("att",   att);
        stats.SetBonus("def",   def);
        stats.SetBonus("spd",   spd);
        stats.SetBonus("dex",   dex);
        stats.SetBonus("vit",   vit);
        stats.SetBonus("wis",   wis);
        stats.SetBonus("maxhp", bMaxHP);
        stats.SetBonus("maxmp", bMaxMP);

        var totalsKey = $"{att},{def},{spd},{dex},{vit},{wis},{bMaxHP},{bMaxMP}|{weaponSlot?.item?.name},{armorSlot?.item?.name},{chipSlot?.item?.name},{abilitySlot?.item?.name}";

        if (totalsKey != _lastTotalsKey)
        {
            _lastTotalsKey = totalsKey;
            if (verbose)
            {
                Debug.Log($"[PlayerEquipment] Updated totals → ATT {att} DEF {def} SPD {spd} DEX {dex} VIT {vit} WIS {wis} +HP {bMaxHP} +MP {bMaxMP}");
            }
        }

        void Acc(Item it)
        {
            if (!it) return;
            att    += it.bonusATT;
            def    += it.bonusDEF;
            spd    += it.bonusSPD;
            dex    += it.bonusDEX;
            vit    += it.bonusVIT;
            wis    += it.bonusWIS;
            bMaxHP += it.bonusMaxHP;
            bMaxMP += it.bonusMaxMP;
        }
    }

    static string NameOf(Object o) => o ? o.name : "None";
}
