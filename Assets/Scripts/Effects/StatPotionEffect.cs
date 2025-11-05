using UnityEngine;

public enum StatKind { MaxHP, MaxMP, ATT, DEF, SPD, DEX, VIT, WIS }

[CreateAssetMenu(menuName = "EmptySkulls/ItemEffects/Stat Potion")]
public class StatPotionEffect : ItemEffect
{
    public StatKind stat = StatKind.ATT;
    public int amount = 1;
    public int cap = -1;

    public override bool Apply(PlayerStats target)
    {
        if (!target || amount == 0) return false;

        string key;
        int current;
        switch (stat)
        {
            case StatKind.MaxHP: key = "maxhp"; current = target.baseStats.maxHP; break;
            case StatKind.MaxMP: key = "maxmp"; current = target.baseStats.maxMP; break;
            case StatKind.ATT:   key = "att";   current = target.baseStats.att;   break;
            case StatKind.DEF:   key = "def";   current = target.baseStats.def;   break;
            case StatKind.SPD:   key = "spd";   current = target.baseStats.spd;   break;
            case StatKind.DEX:   key = "dex";   current = target.baseStats.dex;   break;
            case StatKind.VIT:   key = "vit";   current = target.baseStats.vit;   break;
            default:             key = "wis";   current = target.baseStats.wis;   break;
        }

        int targetValue = current + amount;
        if (cap >= 0) targetValue = Mathf.Min(targetValue, cap);
        if (targetValue <= current) return false;

        target.Set(key, targetValue);
        return true;
    }
}
