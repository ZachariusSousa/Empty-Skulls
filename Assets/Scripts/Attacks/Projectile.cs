using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Life")]
    public float lifetime = 3f;
    [Range(0f, 1f)] public float fadeLast = 0.15f;

    [Header("Motion")]
    public float baseSpeed = 8f;
    public bool useSpeedCurve = true;
    public AnimationCurve speedOverLife = AnimationCurve.Linear(0, 1, 1, 1);

    public bool boomerang = false;
    [Range(0.1f, 0.9f)] public float boomerangAt = 0.5f;

    [Header("Wobble")]
    public float wobbleAmplitude = 0f; // units
    public float wobbleHz = 4f;        // cycles/sec

    [Header("Visual")]
    public Transform visual;           // rotate this (child). If null, uses self.
    public bool faceVelocity = true;   // aim sprite to velocity
    public float spriteSpin = 0f;      // deg/sec
    public float spriteAngleOffset = 0f; // baseline offset (e.g., 0°→up, 45°, etc.)

    [Header("Hits")]
    public int damage = 1;             // base damage from the projectile
    public int pierce = 0;             // 0 = die on first hit; 1 = pass through 1 target, etc.
    public int defCap = 25;            // defense cap used by EntityStats.ApplyDamage
    public LayerMask hitMask = ~0;
    public GameObject impactVfx;

    [Header("Owner")]
    public GameObject owner;
    public bool ignoreOwner = true;
    public EntityStats ownerStats;     // optional: include shooter's attack

    // runtime
    Vector2 _dir = Vector2.up;   // forward direction (normalized)
    Vector2 _perp;               // perpendicular for wobble
    Vector3 _spawnPos;
    float _age;
    int _hits;
    SpriteRenderer _sr;
    Collider2D _col;
    float _spinAccum;

    /// <summary>Set launch direction and (optionally) assign owner & stats.</summary>
    public void Launch(Vector2 direction, EntityStats ownerStatsRef = null, GameObject ownerGO = null)
    {
        if (direction.sqrMagnitude > 0f)
            _dir = direction.normalized;
        _perp = new Vector2(-_dir.y, _dir.x);
        if (ownerStatsRef) ownerStats = ownerStatsRef;
        if (ownerGO) owner = ownerGO;
    }

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (!visual) visual = transform;
        _sr = GetComponentInChildren<SpriteRenderer>();
        _spawnPos = transform.position;

        if (_dir.sqrMagnitude < 0.0001f) _dir = transform.up;
        _perp = new Vector2(-_dir.y, _dir.x);
    }

    void Update()
    {
        _age += Time.deltaTime;
        float t01 = Mathf.Clamp01(_age / Mathf.Max(0.0001f, lifetime));

        // forward dir (boomerang flips mid-life)
        Vector2 dir = (boomerang && t01 >= boomerangAt) ? -_dir : _dir;

        // speed over life
        float speedMul = useSpeedCurve ? speedOverLife.Evaluate(t01) : 1f;
        float speed = Mathf.Max(0f, baseSpeed * speedMul);

        // wobble (sinusoidal perpendicular offset)
        Vector2 wobble = Vector2.zero;
        if (wobbleAmplitude > 0f && wobbleHz > 0f)
        {
            float s = Mathf.Sin(_age * Mathf.PI * 2f * wobbleHz);
            wobble = _perp * (wobbleAmplitude * s);
        }

        // move
        Vector2 v = dir * speed + wobble;
        transform.position += (Vector3)(v * Time.deltaTime);

        // visual rotation: face velocity + continuous spin + baseline offset
        _spinAccum += spriteSpin * Time.deltaTime;
        if (faceVelocity && v.sqrMagnitude > 0.000001f)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; // 0° = right
            float finalZ = (ang - 90f) + spriteAngleOffset + _spinAccum; // convert to "up", then offset, then spin
            visual.rotation = Quaternion.Euler(0, 0, finalZ);
        }
        else if (spriteSpin != 0f)
        {
            visual.Rotate(0, 0, spriteSpin * Time.deltaTime, Space.Self);
        }

        // fade near the end
        if (_sr && fadeLast > 0f && t01 >= 1f - fadeLast)
        {
            float f = Mathf.InverseLerp(1f - fadeLast, 1f, t01);
            var c = _sr.color; c.a = 1f - f;
            _sr.color = c;
        }

        if (_age >= lifetime)
            Kill();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1) layer filter
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        // 2) ignore owner (incl. children & attached rigidbody root)
        if (ignoreOwner && owner)
        {
            if (other.gameObject == owner) return;
            if (other.transform.IsChildOf(owner.transform)) return;
            var rb = other.attachedRigidbody ? other.attachedRigidbody.gameObject : null;
            if (rb && rb == owner) return;
        }

        // 3) find EntityStats on hit target
        var target = other.GetComponent<EntityStats>() ?? other.GetComponentInParent<EntityStats>();
        if (target != null)
        {
            // include owner's attack if provided
            int finalDamage = damage + (ownerStats ? Mathf.Max(0, ownerStats.EffATT) : 0);
            target.ApplyDamage(finalDamage, defCap);

            _hits++;
            if (_hits > pierce)
            {
                Kill();
                return;
            }
            // else: continue flying (piercing)
        }
        else
        {
            // non-damageable (e.g., wall)
            Kill();
        }
    }

    void Kill()
    {
        if (impactVfx) Instantiate(impactVfx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

}
