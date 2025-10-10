using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage;
    public LayerMask hitMask;

    [Header("Lifetime")]
    public float lifetime = 3f;
    [Tooltip("If > 0, destroy after traveling this distance (ignoring 'lifetime').")]
    public float maxTravelDistance = -1f;

    [Header("Pierce")]
    [Tooltip("How many targets this projectile can pass through before dying. 0 = die on first hit.")]
    public int pierceCount = 0;

    Rigidbody2D _rb;
    GameObject _owner;
    Vector2 _spawnPos;

    public void Initialize(int dmg, Vector2 velocity, float life, LayerMask mask, GameObject owner)
    {
        damage = dmg;
        lifetime = life;
        hitMask = mask;
        _owner = owner;

        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        _rb.linearVelocity = velocity;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _spawnPos = transform.position;

        if (maxTravelDistance <= 0f)
        {
            // Time-based lifetime
            Destroy(gameObject, lifetime);
        }
        // else distance-based lifetime handled in Update()
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (maxTravelDistance > 0f)
        {
            float dist = Vector2.Distance(_spawnPos, transform.position);
            if (dist >= maxTravelDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner && other.gameObject == _owner)
        {
            return;
        }

        if (hitMask.value != 0)
        {
            int otherBit = 1 << other.gameObject.layer;
            if ((hitMask.value & otherBit) == 0)
            {
                return;
            }
        }

        DamageHelper.ApplyDamage(other.gameObject, damage, _owner);
    }

    }
