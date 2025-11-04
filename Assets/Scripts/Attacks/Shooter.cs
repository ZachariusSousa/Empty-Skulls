using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Shooter : MonoBehaviour
{
    // ===== GROUPS =====
    [System.Serializable]
    public struct WiringGroup
    {
        [Header("Prefab")]
        public GameObject projectilePrefab;
    }

    public enum FireMode { Semi, Auto, Burst }
    public enum DamageMode { UseEffATT, FixedOne }

    [System.Serializable]
    public struct ControlGroup
    {
        [Header("Player vs AI")]
        [Tooltip("Player uses input; AI/enemies call FireOnce()/FireBurst().")]
        public bool driveByInput;

        [Header("Player Input (if driveByInput)")]
        public Camera cam;
        public bool useMouseAim;
        public KeyCode fireKey;
    }

    [System.Serializable]
    public struct FireGroup
    {
        [Header("Pattern")]
        public FireMode fireMode;          // Semi, Auto, Burst
        [Min(1)] public float rpm;         // single RPM value
        [Min(1)] public int pelletsPerShot;
        [Tooltip("Total degrees across the cone (centered)")]
        public float spreadAngle;

        [Header("Burst")]
        [Min(1)] public int burstCount;    // spacing uses RPM
    }

    [System.Serializable]
    public struct StatsGroup
    {
        [Header("Damage & Scaling")]
        public bool useStats;              // get EntityStats
        public DamageMode damageMode;      // UseEffATT or FixedOne(=1)
        [Tooltip("RPM = rpm + EffDEX * rpmPerDEX")]
        public float rpmPerDEX;
    }

    [System.Serializable]
    public struct ProjectileGroup
    {
        [Header("Hit & Optional Overrides")]
        public LayerMask hitMask;
        [Tooltip("Optional; -1 = ignore. If your Projectile exposes an int 'defCap', set it here.")]
        public int optionalDefCap;
    }

    // ===== INSTANCES (defaults) =====
    public WiringGroup wiring = new WiringGroup
    {
        projectilePrefab = null
    };

    public ControlGroup control = new ControlGroup
    {
        driveByInput = false,
        cam = null,
        useMouseAim = true,
        fireKey = KeyCode.Mouse0
    };

    public FireGroup fire = new FireGroup
    {
        fireMode = FireMode.Auto,
        rpm = 360f,
        pelletsPerShot = 1,
        spreadAngle = 0f,
        burstCount = 3
    };

    public StatsGroup stats = new StatsGroup
    {
        useStats = true,
        damageMode = DamageMode.UseEffATT,
        rpmPerDEX = 6f
    };

    public ProjectileGroup projectile = new ProjectileGroup
    {
        hitMask = ~0,
        optionalDefCap = -1
    };

    // ===== RUNTIME =====
    float _cooldown;
    float _burstTimer;
    int _burstLeft;
    Vector2 _aim = Vector2.right;
    EntityStats _stats;

    void Awake()
    {
        if (stats.useStats) _stats = GetComponentInParent<EntityStats>();
        if (!control.cam && control.driveByInput) control.cam = Camera.main;
        if (_aim.sqrMagnitude < 0.0001f) _aim = transform.right;
    }

    void Update()
    {
        // tick timers
        if (_cooldown > 0f)    _cooldown   -= Time.deltaTime;
        if (_burstTimer > 0f)  _burstTimer -= Time.deltaTime;

        if (!control.driveByInput) return;
        if (!wiring.projectilePrefab) return;

        if (control.useMouseAim && control.cam)
        {
            Vector2 m = MouseWorld();
            Vector2 toM = m - (Vector2)transform.position;
            if (toM.sqrMagnitude > 0.0001f) _aim = toM.normalized;
        }

        #if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool held    = mouse != null && mouse.leftButton.isPressed;
        #else
        bool pressed = Input.GetKeyDown(control.fireKey);
        bool held    = Input.GetKey(control.fireKey);
        #endif

        switch (fire.fireMode)
        {
            case FireMode.Semi:
                if (pressed) TryFire();
                break;

            case FireMode.Auto:
                if ((pressed || held) && ReadyToFire()) FireOnce();
                break;

            case FireMode.Burst:
                if (pressed && _burstLeft <= 0)
                {
                    _burstLeft = Mathf.Max(1, fire.burstCount);
                    _burstTimer = 0f;
                }
                if (_burstLeft > 0 && _burstTimer <= 0f)
                {
                    FireOnce();
                    _burstLeft--;
                    // spacing uses same seconds/shot as RPM
                    _burstTimer = (_burstLeft > 0) ? SecondsPerShot() : 0f;
                }
                break;
        }
    }

    // ===== Public API (AI/enemies) =====
    public void AimAtWorld(Vector2 worldPos)
    {
        Vector2 d = worldPos - (Vector2)transform.position;
        if (d.sqrMagnitude > 0.0001f) _aim = d.normalized;
    }

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f) _aim = dir.normalized;
    }

    public void FireOnce()
    {
        if (!ReadyToFire()) return;
        _cooldown = SecondsPerShot();
        ShootPellets();
    }

    public IEnumerator FireBurst(int count)
    {
        count = Mathf.Max(1, count);
        float dt = SecondsPerShot();
        for (int i = 0; i < count; i++)
        {
            FireOnce();
            if (i < count - 1) yield return new WaitForSeconds(dt);
        }
    }

    // ===== Internals =====
    void TryFire()
    {
        if (ReadyToFire()) FireOnce();
    }

    bool ReadyToFire() => _cooldown <= 0f;

    float SecondsPerShot()
    {
        float rpm = Mathf.Max(1f, fire.rpm);
        if (stats.useStats && _stats)
            rpm += _stats.EffDEX * stats.rpmPerDEX;

        return Mathf.Max(0.01f, 60f / rpm);
    }

    void ShootPellets()
    {
        if (!wiring.projectilePrefab) return;

        int count = Mathf.Max(1, fire.pelletsPerShot);
        Vector2 baseDir = (_aim.sqrMagnitude > 0.0001f) ? _aim : (Vector2)transform.right;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i / (float)(count - 1) - 0.5f);
            float ang = fire.spreadAngle * t;
            Vector2 dir = (Quaternion.Euler(0, 0, ang) * baseDir).normalized;

            SpawnProjectile((Vector2)transform.position, dir);
        }
    }

    void SpawnProjectile(Vector2 origin, Vector2 dir)
    {
        var go = Instantiate(wiring.projectilePrefab, origin, Quaternion.identity);
        var p  = go.GetComponent<Projectile>();
        if (!p) return;

        // common wiring expected by your Projectile
        p.owner   = gameObject;
        p.hitMask = projectile.hitMask;

        // Damage + stats
        int dmg = 1; // fixed path = ONE
        if (stats.useStats && _stats && stats.damageMode == DamageMode.UseEffATT)
            dmg = Mathf.Max(0, _stats.EffATT);

        p.damage = dmg;
        p.ownerStats = _stats ? _stats : null;

        // Optional defCap pass-through if your Projectile exposes it
        if (projectile.optionalDefCap >= 0)
        {
            var field = p.GetType().GetField("defCap");
            if (field != null && field.FieldType == typeof(int))
                field.SetValue(p, projectile.optionalDefCap);
        }

        p.Launch(dir);
    }

    Vector2 MouseWorld()
    {
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current;
        Vector2 screen = m != null ? (Vector2)m.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        Vector2 screen = (Vector2)Input.mousePosition;
#endif
        float depth = control.cam
            ? Mathf.Abs(control.cam.transform.position.z - transform.position.z)
            : 10f;
        var w = control.cam ? control.cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth)) : Vector3.zero;
        return new Vector2(w.x, w.y);
    }

    void OnValidate()
    {
        fire.rpm            = Mathf.Max(1f, fire.rpm);
        fire.pelletsPerShot = Mathf.Max(1, fire.pelletsPerShot);
        fire.burstCount     = Mathf.Max(1, fire.burstCount);
    }
}
