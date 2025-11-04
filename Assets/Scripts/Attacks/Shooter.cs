using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Shooter : MonoBehaviour
{
    [System.Serializable]
    public struct WiringGroup
    {
        public GameObject projectilePrefab;
        public Transform muzzle;
    }

    public enum FireMode { Auto, Burst }
    public enum DamageMode { UseEffATT, FixedOne }

    [System.Serializable]
    public struct ControlGroup
    {
        public bool driveByInput;
        public Camera cam;
        public bool useMouseAim;
        public KeyCode fireKey;
    }

    [System.Serializable]
    public struct FireGroup
    {
        public FireMode fireMode;
        [Min(1)] public float rpm;
        [Min(1)] public int pelletsPerShot;
        public float spreadAngle;
        [Min(1)] public int burstCount;
    }

    [System.Serializable]
    public struct StatsGroup
    {
        public bool useStats;
        public DamageMode damageMode;
        public float rpmPerDEX;
    }

    [System.Serializable]
    public struct ProjectileGroup
    {
        public LayerMask hitMask;
        public int optionalDefCap;
    }

    public WiringGroup wiring = new WiringGroup { projectilePrefab = null, muzzle = null };
    public ControlGroup control = new ControlGroup { driveByInput = false, cam = null, useMouseAim = true, fireKey = KeyCode.Mouse0 };
    public FireGroup fire = new FireGroup { fireMode = FireMode.Auto, rpm = 360f, pelletsPerShot = 1, spreadAngle = 0f, burstCount = 3 };
    public StatsGroup stats = new StatsGroup { useStats = true, damageMode = DamageMode.UseEffATT, rpmPerDEX = 6f };
    public ProjectileGroup projectile = new ProjectileGroup { hitMask = ~0, optionalDefCap = -1 };

    [Header("Equipment Link (optional)")]
    public ItemSlotUI weaponSlot;

    float _cooldown;
    float _burstTimer;
    int _burstLeft;
    Vector2 _aim = Vector2.right;
    EntityStats _stats;

    Item _appliedItem;
    GameObject _cachedOverridePrefab;

    FireGroup _baseFire;
    StatsGroup _baseStats;
    GameObject _baseProjectilePrefab;

    static void ApplyClassDefaults(Item it, ref FireGroup fire, ref StatsGroup stats)
    {
        switch (it.weaponClass)
        {
            case WeaponClass.MachineGun:
                fire.fireMode = it.isBurstFire ? FireMode.Burst : FireMode.Auto;
                if (fire.rpm <= 1f) fire.rpm = 480f;
                if (fire.pelletsPerShot < 1) fire.pelletsPerShot = 1;
                if (fire.spreadAngle <= 0f) fire.spreadAngle = 4f;
                if (stats.rpmPerDEX <= 0f) stats.rpmPerDEX = 8f;
                break;
            case WeaponClass.Sniper:
                fire.fireMode = it.isBurstFire ? FireMode.Burst : FireMode.Auto;
                if (fire.rpm <= 1f) fire.rpm = 60f;
                if (fire.pelletsPerShot < 1) fire.pelletsPerShot = 1;
                if (fire.spreadAngle < 0f) fire.spreadAngle = 0f;
                if (stats.rpmPerDEX <= 0f) stats.rpmPerDEX = 3f;
                break;
            case WeaponClass.Shotgun:
                fire.fireMode = it.isBurstFire ? FireMode.Burst : FireMode.Auto;
                if (fire.rpm <= 1f) fire.rpm = 120f;
                if (fire.pelletsPerShot < 1) fire.pelletsPerShot = 5;
                if (fire.spreadAngle <= 0f) fire.spreadAngle = 18f;
                if (stats.rpmPerDEX <= 0f) stats.rpmPerDEX = 5f;
                break;
            default:
                break;
        }
    }

    void Awake()
    {
        if (stats.useStats) _stats = GetComponentInParent<EntityStats>();
        if (!control.cam && control.driveByInput) control.cam = Camera.main;
        if (_aim.sqrMagnitude < 0.0001f) _aim = transform.right;

        if (!weaponSlot)
        {
            var pe = GetComponentInParent<PlayerEquipment>();
            if (pe) weaponSlot = pe.weaponSlot;
        }

        _baseFire = fire;
        _baseStats = stats;
        _baseProjectilePrefab = wiring.projectilePrefab;

        SubscribeSlot(true);
        SyncItemIfChanged();
    }

    void OnEnable()  => SubscribeSlot(true);
    void OnDisable() => SubscribeSlot(false);

    void SubscribeSlot(bool add)
    {
        if (!weaponSlot) return;
        if (add) weaponSlot.onItemChanged += OnWeaponChanged;
        else     weaponSlot.onItemChanged -= OnWeaponChanged;
    }

    void OnWeaponChanged(ItemSlotUI s, Item oldIt, Item newIt)
    {
        _appliedItem = null;
        ApplyItemToShooter(newIt);
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        if (_burstTimer > 0f) _burstTimer -= Time.deltaTime;

        SyncItemIfChanged();
        if (!HasEquippedWeapon()) return;

        if (!control.driveByInput) return;
        if (!GetActiveProjectilePrefab()) return;

        if (control.useMouseAim && control.cam)
        {
            Vector2 m = MouseWorld();
            Vector2 toM = m - (Vector2)MuzzlePos();
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
                    _burstTimer = (_burstLeft > 0) ? SecondsPerShot() : 0f;
                }
                break;
        }
    }

    public void AimAtWorld(Vector2 worldPos)
    {
        Vector2 d = worldPos - (Vector2)MuzzlePos();
        if (d.sqrMagnitude > 0.0001f) _aim = d.normalized;
    }

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f) _aim = dir.normalized;
    }

    public void FireOnce()
    {
        if (!HasEquippedWeapon()) return;
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

    void TryFire()
    {
        if (ReadyToFire()) FireOnce();
    }

    bool ReadyToFire() => _cooldown <= 0f && HasEquippedWeapon();

    float SecondsPerShot()
    {
        float rpmVal = Mathf.Max(1f, fire.rpm);
        if (stats.useStats && _stats)
            rpmVal += _stats.EffDEX * Mathf.Max(0f, stats.rpmPerDEX);
        return Mathf.Max(0.01f, 60f / rpmVal);
    }

    void ShootPellets()
    {
        var prefab = GetActiveProjectilePrefab();
        if (!prefab) return;

        int count = Mathf.Max(1, fire.pelletsPerShot);
        Vector2 baseDir = (_aim.sqrMagnitude > 0.0001f) ? _aim : (Vector2)transform.right;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i / (float)(count - 1) - 0.5f);
            float ang = fire.spreadAngle * t;
            Vector2 dir = (Quaternion.Euler(0, 0, ang) * baseDir).normalized;
            SpawnProjectile((Vector2)MuzzlePos(), dir);
        }
    }

    void SpawnProjectile(Vector2 origin, Vector2 dir)
    {
        var prefab = GetActiveProjectilePrefab();
        if (!prefab) return;

        var spawnPos = new Vector3(origin.x, origin.y, 0f);
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        var p  = go.GetComponent<Projectile>();
        if (!p) return;

        p.owner   = gameObject;
        p.hitMask = projectile.hitMask;

        int dmg = 1;
        if (stats.useStats && _stats && stats.damageMode == DamageMode.UseEffATT)
            dmg = Mathf.Max(0, _stats.EffATT);
        p.damage = dmg;
        p.ownerStats = _stats ? _stats : null;

        var it = GetWeaponItem();

        if (it && it.pierceDEF)
            p.defCap = 0;
        else if (projectile.optionalDefCap >= 0)
            p.defCap = projectile.optionalDefCap;

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
        var cam = control.cam ? control.cam : Camera.main;
        float depth = cam ? Mathf.Abs(cam.transform.position.z - transform.position.z) : 10f;
        var w = cam ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth)) : Vector3.zero;
        return new Vector2(w.x, w.y);
    }

    Transform MuzzleTransform() => wiring.muzzle ? wiring.muzzle : transform;
    Vector3 MuzzlePos() => MuzzleTransform().position;

    void OnValidate()
    {
        fire.rpm = Mathf.Max(1f, fire.rpm);
        fire.pelletsPerShot = Mathf.Max(1, fire.pelletsPerShot);
        fire.burstCount = Mathf.Max(1, fire.burstCount);
    }

    void SyncItemIfChanged()
    {
        var it = GetWeaponItem();
        if (it == _appliedItem) return;
        _appliedItem = it;
        ApplyItemToShooter(it);
    }

    Item GetWeaponItem()
    {
        if (weaponSlot && weaponSlot.item && weaponSlot.item.isEquippable &&
            weaponSlot.item.equipSlot == EquipSlotKind.Weapon)
            return weaponSlot.item;
        return null;
    }

    GameObject GetActiveProjectilePrefab()
    {
        if (_cachedOverridePrefab) return _cachedOverridePrefab;
        return wiring.projectilePrefab;
    }

    void ApplyItemToShooter(Item it)
    {
        _cachedOverridePrefab = null;

        if (!it)
        {
            fire = _baseFire;
            stats.rpmPerDEX = _baseStats.rpmPerDEX;
            wiring.projectilePrefab = _baseProjectilePrefab;
            return;
        }

        var f = fire;
        var s = stats;

        if (it.pelletsToFire > 0) f.pelletsPerShot = it.pelletsToFire;
        if (it.bulletSpread >= 0f) f.spreadAngle = it.bulletSpread;
        if (it.isBurstFire) f.fireMode = FireMode.Burst;
        if (it.dexMultiplier > 0f) s.rpmPerDEX = it.dexMultiplier;

        ApplyClassDefaults(it, ref f, ref s);

        fire = f;
        stats = s;

        if (it.overrideProjectilePrefab)
            _cachedOverridePrefab = it.overrideProjectilePrefab;
        else
            wiring.projectilePrefab = _baseProjectilePrefab;
    }

    bool HasEquippedWeapon()
{
    // If there’s no weaponSlot wired, assume this is an NPC/enemy shooter: allow firing.
    if (!weaponSlot) return true;
    return _appliedItem != null;
}

}
