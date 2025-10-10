using UnityEngine;

public static class DamageHelper
{
    // Apply damage to PlayerStats or EnemyStats if present.
    public static void ApplyDamage(GameObject target, int damage, GameObject source = null)
    {
        if (!target || damage <= 0) return;

        // Player
        var ps = target.GetComponent<PlayerStats>();
        if (ps)
        {
            ps.TakeDamage(damage);   // you already have TakeDamage(int)
            return;
        }

        // Enemy
        var es = target.GetComponent<EnemyStats>();
        if (es)
        {
            es.TakeDamage(damage, source);
            return;
        }
    }
}
