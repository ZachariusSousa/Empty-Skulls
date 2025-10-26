using UnityEngine;

[CreateAssetMenu(menuName = "EmptySkulls/ItemEffects/Restore MP")]
public class RestoreMpEffect : ItemEffect
{
    public int amount = 40;
    public override bool Apply(PlayerStats target)
    {
        if (!target) return false;
        int before = target.MP;
        target.RestoreMP(amount);
        return target.MP > before;
    }
}
