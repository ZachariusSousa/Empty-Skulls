using UnityEngine;

public class Health : MonoBehaviour, IHealth
{
    public int maxHealth = 50;
    int current;

    void Awake() => current = maxHealth;

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitDirection)
    {
        current -= amount;
        // TODO: play hit VFX/SFX, knockback using hitDirection, flash sprite, etc.
        if (current <= 0)
            Die();
    }

    void Die()
    {
        // TODO: drop loot, award XP, etc.
        Destroy(gameObject);
    }
}
