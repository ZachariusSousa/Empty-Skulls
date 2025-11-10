using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class EnemyAgent : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public string targetTag = "Player";
    public float detectRadius = 12f;
    public float loseRadius = 16f;

    [Header("Distances")]
    public float minDistance = 1.5f;
    public float maxDistance = 6f;
    public float keepDistance = 3f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float acceleration = 12f;
    public float strafeSpeed = 2.5f;
    public float strafeSwitchInterval = 1.25f;

    [Header("Line of Sight")]
    public bool requireLineOfSight = false;
    public LayerMask losMask;

    [Header("Shooting")]
    public Shooter shooter;
    public float minAttackDistance = 1.5f;
    public float maxAttackDistance = 7.5f;
    public bool useBursts = false;
    public int burstCount = 3;
    public float burstInterval = 0.08f; // ignored; Shooter uses rpm
    [Tooltip("Extra delay between attack cycles (in seconds), optional.")]
    public float extraAttackPause = 0f;

    [Header("Animation")]
    public Animator anim;
    public SpriteRenderer sprite;

    Rigidbody2D _rb;
    Vector2 _vel;
    float _strafeTimer;
    int _strafeDir = 1;

    // local timer so we only shoot+animate once per computed RPM window
    float _fireTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!shooter) shooter = GetComponent<Shooter>();
        if (shooter) shooter.control.driveByInput = false;
        if (!anim)   anim   = GetComponentInChildren<Animator>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag(targetTag);
            if (p) target = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (_fireTimer > 0f) _fireTimer -= Time.fixedDeltaTime;

        // acquire/lose
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag(targetTag);
            if (p && Vector2.Distance(transform.position, p.transform.position) <= detectRadius)
                target = p.transform;
        }
        else if (Vector2.Distance(transform.position, target.position) > loseRadius)
            target = null;

        Vector2 desired = Vector2.zero;

        if (target)
        {
            Vector2 toT = target.position - transform.position;
            float dist  = toT.magnitude;
            Vector2 dir = toT.normalized;
            bool hasLOS = !requireLineOfSight || HasLineOfSight();

            // movement band
            if (dist > maxDistance)      desired += dir * moveSpeed;
            else if (dist < minDistance) desired -= dir * moveSpeed;
            else
            {
                _strafeTimer -= Time.fixedDeltaTime;
                if (_strafeTimer <= 0f)
                {
                    _strafeTimer = strafeSwitchInterval;
                    _strafeDir = Random.value < 0.5f ? -1 : 1;
                }
                Vector2 tangent = new Vector2(-dir.y, dir.x) * _strafeDir;
                desired += tangent * strafeSpeed;
            }

            if (shooter && hasLOS && dist >= minAttackDistance && dist <= maxAttackDistance && _fireTimer <= 0f)
            {
                float dt = ComputeSecondsPerShot();
                shooter.AimAtWorld(target.position);

                // play attack animation instantly (before projectile)
                if (anim)
                    anim.CrossFadeInFixedTime("Attack", 0f, 0, 0f); // must match your state name exactly

                // now fire
                if (useBursts && burstCount > 1)
                    StartCoroutine(shooter.FireBurst(burstCount));
                else
                    shooter.FireOnce();

                _fireTimer = dt + extraAttackPause;
            }

            // flip by facing
            if (sprite) sprite.flipX = dir.x < 0;
        }

        _vel = Vector2.MoveTowards(_vel, desired, acceleration * Time.fixedDeltaTime);
        _rb.linearVelocity = _vel;
    }

    // ---- helpers ----

    float ComputeSecondsPerShot()
    {
        // mirror Shooter.SecondsPerShot()
        float rpm = Mathf.Max(1f, shooter.fire.rpm);
        float dexPer = Mathf.Max(0f, shooter.stats.rpmPerDEX);

        float effDex = 0f;
        if (shooter.stats.useStats)
        {
            var stats = shooter.GetComponentInParent<EntityStats>();
            if (stats) effDex = stats.EffDEX;
        }

        rpm += effDex * dexPer;
        return Mathf.Max(0.01f, 60f / rpm);
    }

    IEnumerator AnimBurst(int count, float dt)
    {
        for (int i = 0; i < count; i++)
        {
            TriggerAttackAnim();
            if (i < count - 1) yield return new WaitForSeconds(dt);
        }
    }

    void TriggerAttackAnim()
    {
        if (!anim) return;
        anim.ResetTrigger("attack");
        anim.SetTrigger("attack");
    }

    bool HasLineOfSight()
    {
        if (!target) return false;
        Vector2 start = transform.position;
        Vector2 end   = target.position;
        var hit = Physics2D.Raycast(start, (end - start).normalized, Vector2.Distance(start, end), losMask);
        return hit.collider == null;
    }
}
