using UnityEngine;

public abstract class EnemyAttackBase : MonoBehaviour
{
    [Header("Range Gate")]
    public float minAttackDistance = 0.5f;
    public float maxAttackDistance = 6f;

    [Header("Core")]
    public int damage = 10;
    public float fireRate = 1.0f;        // shots per second
    public float warmup = 0f;            // charge-up (optional)
    public float winddown = 0f;          // post-shoot delay (optional)

    [Header("Aiming")]
    public Transform muzzle;             // optional – where projectiles/overlaps originate. fallback = transform

    protected float _nextShootTime;

    public virtual bool TryAttack(Transform target)
    {
        if (Time.time < _nextShootTime) return false;
        _nextShootTime = Time.time + (1f / Mathf.Max(0.0001f, fireRate)) + winddown;
        OnAttack(target);
        return true;
    }

    protected abstract void OnAttack(Transform target);

    protected Vector2 GetMuzzlePosition()
    {
        return muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
    }
}
