using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public EquipmentSlotUI weaponSlot;
    public EquipmentSlotUI armorSlot;
    public EquipmentSlotUI chipSlot;
    public EquipmentSlotUI abilitySlot;

    public PlayerStats stats;

    void LateUpdate()
    {
        int att=0, def=0, spd=0, dex=0, vit=0, wis=0, luck=0, bMaxHP=0, bMaxMP=0;

        Acc(weaponSlot?.item);
        Acc(armorSlot?.item);
        Acc(chipSlot?.item);
        Acc(abilitySlot?.item);

        // Push to PlayerStats bonus layer
        stats.SetBonus("att",   att);
        stats.SetBonus("def",   def);
        stats.SetBonus("spd",   spd);
        stats.SetBonus("dex",   dex);
        stats.SetBonus("vit",   vit);
        stats.SetBonus("wis",   wis);
        stats.SetBonus("luck",  luck);
        stats.SetBonus("maxhp", bMaxHP);
        stats.SetBonus("maxmp", bMaxMP);

        void Acc(Item it)
        {
            if (!it) return;
            att   += it.bonusATT;
            def   += it.bonusDEF;
            spd   += it.bonusSPD;
            dex   += it.bonusDEX;
            vit   += it.bonusVIT;
            wis   += it.bonusWIS;
            luck  += it.bonusLUCK;
            bMaxHP+= it.bonusMaxHP;
            bMaxMP+= it.bonusMaxMP;
        }
    }
}
