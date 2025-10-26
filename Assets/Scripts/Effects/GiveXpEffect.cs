using UnityEngine;

[CreateAssetMenu(menuName = "EmptySkulls/ItemEffects/Give XP")]
public class GiveXpEffect : ItemEffect
{
    public int amount = 20;
    public override bool Apply(PlayerStats target)
    {
        if (!target) return false;
        int before = target.xp;
        target.AddXP(amount);
        return target.xp > before;
    }
}
