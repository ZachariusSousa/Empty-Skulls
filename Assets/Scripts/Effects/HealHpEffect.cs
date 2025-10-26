using UnityEngine;

[CreateAssetMenu(menuName = "EmptySkulls/ItemEffects/Heal HP")]
public class HealHpEffect : ItemEffect
{
    public int amount = 50;
    public override bool Apply(PlayerStats target)
    {
        if (!target) return false;
        int before = target.HP;
        target.Heal(amount);
        return target.HP > before;
    }
}
