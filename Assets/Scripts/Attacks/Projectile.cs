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
    public float wobbleAmplitude = 0f;
    public float wobbleHz = 4f;

    [Header("Visual")]
    public Transform visual;          // rotate this (child). If null, uses self.
    public bool faceVelocity = true;  // aim sprite to velocity
    public float spriteSpin = 0f;     // deg/sec
    public float spriteAngleOffset = 0f; // fix art baseline (0° or 45°, etc.)

    [Header("Hits")]
    public int damage = 1;
    public int pierce = 0;
    public LayerMask hitMask = ~0;
    public GameObject impactVfx;

    [Header("Owner")]
    public GameObject owner;
    public bool ignoreOwner = true;

    // runtime
    Vector2 _dir = Vector2.up;
    Vector2 _perp;
    Vector3 _spawnPos;
    float _age;
    int _hits;
    SpriteRenderer _sr;
    Collider2D _col;
    float _spinAccum; // track spin

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0f)
            _dir = direction.normalized;
        _perp = new Vector2(-_dir.y, _dir.x);
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

        Vector2 dir = (boomerang && t01 >= boomerangAt) ? -_dir : _dir;

        float speedMul = useSpeedCurve ? speedOverLife.Evaluate(t01) : 1f;
        float speed = Mathf.Max(0f, baseSpeed * speedMul);

        Vector2 wobble = Vector2.zero;
        if (wobbleAmplitude > 0f && wobbleHz > 0f)
        {
            float s = Mathf.Sin(_age * Mathf.PI * 2f * wobbleHz);
            wobble = _perp * (wobbleAmplitude * s);
        }

        Vector2 v = dir * speed + wobble;
        transform.position += (Vector3)(v * Time.deltaTime);

        // combine facing + spin + art offset on the visual transform
        _spinAccum += spriteSpin * Time.deltaTime;

        if (faceVelocity && v.sqrMagnitude > 0.000001f)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; // 0° = right
            float finalZ = (ang - 90f) + spriteAngleOffset + _spinAccum; // make 0° = up, then offset, then spin
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
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        if (ignoreOwner && owner && other.attachedRigidbody && other.attachedRigidbody.gameObject == owner) return;
        if (ignoreOwner && owner && other.gameObject == owner) return;

        _hits++;
        // hook damage here...

        if (_hits > pierce)
            Kill();
    }

    void Kill()
    {
        if (impactVfx) Instantiate(impactVfx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 p = Application.isPlaying ? _spawnPos : transform.position;
        Vector3 d = (Application.isPlaying ? (Vector3)_dir : transform.up) * 0.8f;
        Gizmos.DrawLine(p, p + d);
    }
#endif
}
