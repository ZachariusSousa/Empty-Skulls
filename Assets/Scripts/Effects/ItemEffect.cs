using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    // Return true if effect did something (so we can consume the item).
    public abstract bool Apply(PlayerStats target);
}
