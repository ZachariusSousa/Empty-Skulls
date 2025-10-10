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
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projectilesPerShot; i++)
        {
            float t = (projectilesPerShot == 1) ? 0f : (i / (float)(projectilesPerShot - 1) - 0.5f);
            float spread = spreadAngle * t;
            float jitter = (aimJitter > 0f) ? Random.Range(-aimJitter, aimJitter) : 0f;

            float angle = baseAngle + spread + jitter;
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            float speed = projectileSpeed;
            if (randomSpeedVariance > 0f)
            {
                float r = Random.Range(1f - randomSpeedVariance, 1f + randomSpeedVariance);
                speed *= r;
            }

            Vector2 vel = rot * Vector2.right * speed;
            var proj = Instantiate(projectilePrefab, origin, rot);

            // Basic init
            proj.Initialize(damage, vel, projectileLifetime, projectileHitMask, gameObject);

            // Optional overrides for melee-style behavior, etc.
            if (overridePierceCount >= 0) proj.pierceCount = overridePierceCount;
            if (overrideMaxTravelDistance > 0f) proj.maxTravelDistance = overrideMaxTravelDistance;
        }
    }
}
