using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SpellProjectile : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;
    public float lifetime = 1.5f;
    public bool alignToVelocity = true;

    [Header("Combat")]
    public int damage = 10;
    [Tooltip("Which layers can be damaged / stop the projectile (e.g. Enemy, Environment).")]
    public LayerMask hitMask;
    public bool destroyOnHit = true;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Ensure collider is set as trigger (best for bullets).
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Common 2D bullet settings
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnEnable() => Invoke(nameof(Despawn), lifetime);
    void OnDisable() => CancelInvoke(nameof(Despawn));

    public void Launch(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;

        if (alignToVelocity && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            float ang = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            rb.rotation = ang;
        }
    }

    // Trigger-based hit detection (recommended for fast-moving projectiles)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore layers we don't care about
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Try to find a health target on the object or its parents
        var health = other.GetComponentInParent<IHealth>();
        if (health != null)
        {
            Vector2 hitDir = rb.linearVelocity.sqrMagnitude > 0.0001f
                ? rb.linearVelocity.normalized
                : Vector2.zero;

            health.ApplyDamage(damage, transform.position, hitDir);
        }

        if (destroyOnHit)
            Despawn();
    }

    void Despawn() => Destroy(gameObject);
}

/// <summary>
/// Minimal health interface the projectile can talk to.
/// </summary>
public interface IHealth
{
    void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitDirection);
}
