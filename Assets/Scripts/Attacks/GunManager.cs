using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GunShooter : MonoBehaviour
{
    public enum FireMode { Semi, Auto, Burst }

    [Header("Wiring")]
    public Transform firePoint;                 // primary muzzle
    public Transform secondaryFirePoint;        // optional off-hand muzzle for dual wield
    public GameObject projectilePrefab;         // must have Projectile (+ Collider2D isTrigger)
    public Camera cam;

    [Header("Firing")]
    public FireMode fireMode = FireMode.Auto;
    [Tooltip("Rounds per minute; 600 RPM = 0.1s between shots")]
    public float rpm = 600f;
    [Tooltip("Projectiles per shot (shotgun pellets)")]
    public int projectilesPerShot = 1;
    [Tooltip("Base cone spread in degrees across pellets (-spread..+spread)")]
    public float spreadAngle = 0f;

    [Header("Burst")]
    public int burstCount = 3;
    public float burstInterval = 0.08f;

    [Header("Projectile Overrides")]
    public float projectileSpeed = 10f;
    [Range(0f, 0.9f)] public float randomSpeedVariance = 0f; // +/-%
    public LayerMask projectileHitMask = ~0;
    public float projectileLifetime = 3f;
    public int overridePierce = -1; // -1 means don't change

    [Header("Muzzle Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioSource fireAudio;
    public float muzzleFlashDestroyAfter = 0.5f;

    [Header("Input")]
    public bool useMouseAim = true;
    public bool holdToFire = true;
    public KeyCode fallbackFireKey = KeyCode.Mouse0;

    // ---------- Arcade feel extras ----------
    [Header("Shotgun Feel")]
    [Tooltip("Extra random jitter (±deg) applied to EACH pellet on top of spread")]
    public float pelletJitterDeg = 2.5f;

    [Tooltip("Small delay between pellets in the same shot (seconds). 0 = instant.")]
    public float pelletStagger = 0.015f;

    [Tooltip("Randomize pellet spawn position within this radius (world units)")]
    public float spawnJitterRadius = 0.03f;

    [Tooltip("Tiny pre-shot delay jitter (seconds) before pellets start spawning")]
    public float preShotJitter = 0.0f;

    [Header("Dual Wield")]
    public bool dualWield = false;
    [Tooltip("Alternate L-R-L-R between shots when dual-wielding")]
    public bool alternateHands = true;
    [Tooltip("If true, fires from BOTH muzzles each shot")]
    public bool fireBothPerShot = false;

    // Runtime state
    float _cooldown;
    Vector2 _aimDir = Vector2.right;
    int _burstShotsLeft;
    float _burstTimer;
    bool _triggerHeld;
    bool _fireLeftNext = true; // used when alternateHands = true

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (_aimDir.sqrMagnitude < 0.0001f) _aimDir = transform.right;
    }

    void Update()
    {
        if (!projectilePrefab) return;
        if (!firePoint) return;

        // Aim
        if (useMouseAim && cam)
            _aimDir = (MouseWorld() - (Vector2)firePoint.position).normalized;

        // Input
        bool pressed = TriggerPressed();
        bool held    = TriggerHeld();

        // Timers
        _cooldown -= Time.deltaTime;
        _burstTimer -= Time.deltaTime;

        switch (fireMode)
        {
            case FireMode.Semi:
                if (pressed) TryFireSingle();
                break;

            case FireMode.Auto:
                if ((pressed || (held && holdToFire)) && _cooldown <= 0f)
                    FireOneShot();
                break;

            case FireMode.Burst:
                if (pressed && _burstShotsLeft <= 0)
                {
                    _burstShotsLeft = Mathf.Max(1, burstCount);
                    _burstTimer = 0f;
                }
                if (_burstShotsLeft > 0 && _burstTimer <= 0f)
                {
                    FireOneShot();
                    _burstShotsLeft--;
                    _burstTimer = (_burstShotsLeft > 0) ? Mathf.Max(0.01f, burstInterval) : 0f;
                }
                break;
        }
    }

    // Public API (for AI/joystick etc.)
    public void SetAimDirection(Vector2 worldDirection)
    {
        if (worldDirection.sqrMagnitude > 0.0001f)
            _aimDir = worldDirection.normalized;
    }
    public void PressTrigger()   { _triggerHeld = true;  }
    public void ReleaseTrigger() { _triggerHeld = false; }

    // ---------------- Internals ----------------

    void TryFireSingle()
    {
        if (_cooldown <= 0f)
            FireOneShot();
    }

    void FireOneShot()
    {
        float secondsPerShot = Mathf.Max(0.01f, 60f / Mathf.Max(1f, rpm));
        _cooldown = secondsPerShot;

        // Decide which muzzles to use this shot
        var originA = firePoint;
        var originB = (dualWield && secondaryFirePoint) ? secondaryFirePoint : null;

        // Determine which to fire from
        if (dualWield)
        {
            if (fireBothPerShot && originB)
            {
                // fire from both
                StartCoroutine(FirePelletBurstFromMuzzle(originA));
                StartCoroutine(FirePelletBurstFromMuzzle(originB));
            }
            else if (alternateHands && originB)
            {
                var chosen = _fireLeftNext ? originA : originB;
                _fireLeftNext = !_fireLeftNext;
                StartCoroutine(FirePelletBurstFromMuzzle(chosen));
            }
            else
            {
                // default to primary muzzle
                StartCoroutine(FirePelletBurstFromMuzzle(originA));
            }
        }
        else
        {
            // single muzzle
            StartCoroutine(FirePelletBurstFromMuzzle(originA));
        }

        // Muzzle VFX/SFX (play on primary; if firing both, the coroutine also spawns flashes at each muzzle)
        if (!fireBothPerShot && muzzleFlashPrefab)
        {
            var fx = Instantiate(muzzleFlashPrefab, originA.position, originA.rotation, originA);
            if (muzzleFlashDestroyAfter > 0f) Destroy(fx, muzzleFlashDestroyAfter);
        }
        if (fireAudio) fireAudio.Play();
    }

    IEnumerator FirePelletBurstFromMuzzle(Transform muzzle)
    {
        if (!muzzle) yield break;

        // Optional tiny randomness before the shot (helps sell arcade “punch”)
        if (preShotJitter > 0f)
            yield return new WaitForSeconds(Random.Range(0f, preShotJitter));

        // If we are firing both per shot, spawn separate flash here too
        if (fireBothPerShot && muzzleFlashPrefab)
        {
            var fx = Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
            if (muzzleFlashDestroyAfter > 0f) Destroy(fx, muzzleFlashDestroyAfter);
        }

        Vector2 baseDir = (_aimDir.sqrMagnitude > 0.0001f) ? _aimDir.normalized : (Vector2)transform.right;

        int shots = Mathf.Max(1, projectilesPerShot);
        for (int i = 0; i < shots; i++)
        {
            // Deterministic spread across pellets…
            float t = (shots == 1) ? 0f : (i / (float)(shots - 1) - 0.5f); // -0.5..+0.5
            float cone = spreadAngle * t;

            // …plus random jitter per pellet
            float jitter = (pelletJitterDeg > 0f) ? Random.Range(-pelletJitterDeg, pelletJitterDeg) : 0f;

            float ang = cone + jitter;
            Vector2 shotDir = (Quaternion.Euler(0, 0, ang) * baseDir).normalized;

            // Randomize spawn position slightly around the muzzle
            Vector2 origin = (Vector2)muzzle.position;
            if (spawnJitterRadius > 0f)
                origin += Random.insideUnitCircle * spawnJitterRadius;

            SpawnOneProjectile(origin, shotDir);

            // Stagger between pellets for that crunchy “brrrrt”
            if (pelletStagger > 0f && i < shots - 1)
                yield return new WaitForSeconds(pelletStagger);
        }
    }

    void SpawnOneProjectile(Vector2 origin, Vector2 shotDir)
    {
        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);

        var proj = go.GetComponent<Projectile>();
        if (proj)
        {
            proj.owner = gameObject;
            proj.hitMask = projectileHitMask;
            proj.lifetime = projectileLifetime;

            float speedMul = (randomSpeedVariance > 0f)
                ? Random.Range(1f - randomSpeedVariance, 1f + randomSpeedVariance)
                : 1f;
            proj.baseSpeed = projectileSpeed * speedMul;

            if (overridePierce >= 0) proj.pierce = overridePierce;

            proj.Launch(shotDir);
        }
    }

    bool TriggerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        bool key = Input.GetKeyDown(fallbackFireKey);
#endif
        bool external = false; // hook if you synthesize single-press externally
        return key || external;
    }

    bool TriggerHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        bool key = Input.GetKey(fallbackFireKey);
#endif
        return key || _triggerHeld;
    }

    Vector2 MouseWorld()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 screen = Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        Vector2 screen = Input.mousePosition;
#endif
        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        return new Vector2(w.x, w.y);
    }

    void OnValidate()
    {
        rpm = Mathf.Max(1f, rpm);
        projectilesPerShot = Mathf.Max(1, projectilesPerShot);
        burstCount = Mathf.Max(1, burstCount);
        burstInterval = Mathf.Max(0.01f, burstInterval);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime);

        pelletStagger = Mathf.Max(0f, pelletStagger);
        spawnJitterRadius = Mathf.Max(0f, spawnJitterRadius);
        pelletJitterDeg = Mathf.Max(0f, pelletJitterDeg);
        preShotJitter = Mathf.Max(0f, preShotJitter);
    }
}
