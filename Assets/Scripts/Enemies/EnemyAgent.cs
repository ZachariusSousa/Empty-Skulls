using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAgent : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;                 // If null, will find by tag
    public string targetTag = "Player";
    public float detectRadius = 12f;
    public float loseRadius = 16f;

    [Header("Distances")]
    public float minDistance = 1.5f;         // Back off if closer than this
    public float maxDistance = 6f;           // Move closer if farther than this
    public float keepDistance = 3f;          // Ideal middle distance if idle

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float acceleration = 12f;
    public float strafeSpeed = 2.5f;
    public float strafeSwitchInterval = 1.25f;

    [Header("Line of Sight (optional)")]
    public bool requireLineOfSight = false;
    public LayerMask losMask;                // walls/obstacles

    // ===== Shooter integration (no extra scripts) =====
    [Header("Shooting")]
    public Shooter shooter;                  // assign in Inspector
    public float minAttackDistance = 1.5f;   // fire if target is within [min, max]
    public float maxAttackDistance = 7.5f;
    public bool useBursts = false;           // optional burst
    public int burstCount = 3;
    public float burstInterval = 0.08f;

    Rigidbody2D _rb;
    Vector2 _vel;
    float _strafeTimer;
    int _strafeDir = 1;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!shooter) shooter = GetComponent<Shooter>();
        if (shooter) shooter.control.driveByInput = false; 

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
        // Acquire/lose target simply by distance
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag(targetTag);
            if (p && Vector2.Distance(transform.position, p.transform.position) <= detectRadius)
                target = p.transform;
        }
        else
        {
            if (Vector2.Distance(transform.position, target.position) > loseRadius)
                target = null;
        }

        Vector2 desired = Vector2.zero;

        if (target)
        {
            Vector2 toT = (target.position - transform.position);
            float dist = toT.magnitude;
            Vector2 dir = toT.normalized;

            bool hasLOS = !requireLineOfSight || HasLineOfSight();

            // Maintain a distance band [minDistance, maxDistance]
            if (dist > maxDistance)        desired += dir * moveSpeed;
            else if (dist < minDistance)   desired -= dir * moveSpeed;
            else
            {
                // In the comfy zone: strafe to feel alive
                _strafeTimer -= Time.fixedDeltaTime;
                if (_strafeTimer <= 0f)
                {
                    _strafeTimer = strafeSwitchInterval;
                    _strafeDir = Random.value < 0.5f ? -1 : 1;
                }
                Vector2 tangent = new Vector2(-dir.y, dir.x) * _strafeDir;
                desired += tangent * strafeSpeed;
            }

            // --- SHOOTER LOGIC (direct) ---
            if (shooter && hasLOS && dist >= minAttackDistance && dist <= maxAttackDistance)
            {
                shooter.AimAtWorld((Vector2)target.position);

                if (useBursts && burstCount > 1)
                {
                    // Burst fire (non-blocking)
                    StartCoroutine(shooter.FireBurst(burstCount));
                }
                else
                {
                    // Single shot (Shooter handles its own RPM/cooldown)
                    shooter.FireOnce();
                }
            }
        }

        // Smooth accel
        _vel = Vector2.MoveTowards(_vel, desired, acceleration * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = _vel;  // Unity 6
#else
        _rb.velocity = _vel;        // fallback if needed
#endif
    }

    bool HasLineOfSight()
    {
        if (!target) return false;
        Vector2 start = transform.position;
        Vector2 end = target.position;
        var hit = Physics2D.Raycast(start, (end - start).normalized, Vector2.Distance(start, end), losMask);
        return hit.collider == null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Attack band for clarity
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, minAttackDistance);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, maxAttackDistance);
    }
}
