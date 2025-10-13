using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SpellCaster : MonoBehaviour
{
    public Transform firePoint;                 // child on player
    public GameObject projectilePrefab;         // prefab with Projectile + Collider2D (isTrigger)
    public float cooldown = 0.12f;
    public Camera cam;

    [Header("Sprite Hooks")]
    public PlayerSpriteController spriteCtrl;
    public float shootPoseSeconds = 0.15f;

    float cd;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!spriteCtrl) spriteCtrl = GetComponentInChildren<PlayerSpriteController>();
    }

    void Update()
    {
        if (!firePoint || !projectilePrefab || !cam) return;

        cd -= Time.deltaTime;

        bool pressed = ClickDown();
        bool held    = ClickHeld();

        if (pressed) TryFire();
        if (held && cd <= 0f) TryFire();
    }

    void TryFire()
    {
        Vector3 m = MouseWorld();
        Vector2 dir = (Vector2)(m - firePoint.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        // Rotates the projectile to face the fire direction (0° = up, so -90)
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, ang - 90f);

        var go = Instantiate(projectilePrefab, firePoint.position, rot);

        // Use the new Projectile script
        var proj = go.GetComponent<Projectile>();
        if (proj)
        {
            proj.owner = gameObject; 
            proj.Launch(dir);    
        }

        if (spriteCtrl)
            spriteCtrl.TriggerShoot(shootPoseSeconds);

        cd = Mathf.Max(0.01f, cooldown);
    }

    bool ClickDown()
    {
    #if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    #else
        return Input.GetMouseButtonDown(0);
    #endif
    }

    bool ClickHeld()
    {
    #if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    #else
        return Input.GetMouseButton(0);
    #endif
    }

    Vector3 MouseWorld()
    {
    #if ENABLE_INPUT_SYSTEM
        Vector2 screen = Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
    #else
        Vector2 screen = Input.mousePosition;
    #endif
        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        w.z = firePoint.position.z;
        return w;
    }
}
