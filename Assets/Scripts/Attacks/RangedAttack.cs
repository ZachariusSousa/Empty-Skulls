using UnityEngine;
using System.Collections;

public class RangedAttack : EnemyAttackBase
{
    [Header("Projectile")]
    public Projectile projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 3f;
    public LayerMask projectileHitMask;

    [Header("Pattern")]
    public int projectilesPerShot = 1;       // e.g., 3 for cone/slash
    public float spreadAngle = 0f;           // degrees total (centered on target)
    public int burstCount = 1;               // shots in a burst
    public float burstInterval = 0.08f;      // time between shots in a burst
    public float aimJitter = 0f;             // random +/- degrees per proj

    [Header("Optional Overrides (affect spawned Projectile)")]
    [Tooltip("-1 = don't override; otherwise set on spawn")]
    public int overridePierceCount = -1;
    [Tooltip("<=0 = don't override; otherwise set on spawn")]
    public float overrideMaxTravelDistance = -1f;
    [Tooltip("Multiply speed per shot randomly within [1 - r, 1 + r]")]
    [Range(0f, 0.9f)] public float randomSpeedVariance = 0f;

    protected override void OnAttack(Transform target)
    {
        StartCoroutine(FireRoutine(target));
    }

    IEnumerator FireRoutine(Transform target)
    {
        if (warmup > 0f) yield return new WaitForSeconds(warmup);

        for (int b = 0; b < burstCount; b++)
        {
            FireOneVolley(target);
            if (b < burstCount - 1) yield return new WaitForSeconds(burstInterval);
        }
    }

    void FireOneVolley(Transform target)
    {
        if (!projectilePrefab) return;

        Vector2 origin = GetMuzzlePosition();
        Vector2 baseDir = (target ? (Vector2)(target.position - transform.position) : Vector2.right).normalized;

        for (int i = 0; i < projectilesPerShot; i++)
        {
            float t = (projectilesPerShot == 1) ? 0f : (i / (float)(projectilesPerShot - 1) - 0.5f);
            float spread = spreadAngle * t;
            float jitter = (aimJitter > 0f) ? Random.Range(-aimJitter, aimJitter) : 0f;
            float delta = spread + jitter;

            // Rotate the baseDir by (delta) degrees to get the shot direction
            Vector2 shotDir = (Quaternion.Euler(0, 0, delta) * baseDir).normalized;

            // Spawn with no rotation; Projectile will face velocity itself
            var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);

            // Push settings onto the projectile
            proj.owner = gameObject;
            proj.hitMask = projectileHitMask;
            proj.lifetime = projectileLifetime;

            // Speed: map RangedAttack.projectileSpeed -> Projectile.baseSpeed
            float speedMul = (randomSpeedVariance > 0f)
                ? Random.Range(1f - randomSpeedVariance, 1f + randomSpeedVariance)
                : 1f;
            proj.baseSpeed = projectileSpeed * speedMul;

            // Optional pierce override (only if you want it)
            if (overridePierceCount >= 0) proj.pierce = overridePierceCount;

            // Launch!
            proj.Launch(shotDir);
        }
    }

}
