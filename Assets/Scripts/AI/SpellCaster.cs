using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SpellCaster : MonoBehaviour
{
    public Transform firePoint;             // empty child on player
    public GameObject projectilePrefab;     // prefab with SpellProjectile + Rigidbody2D
    [Tooltip("Seconds between shots while holding")]
    public float cooldown = 0.12f;
    public Camera cam;                      // assign or auto-uses Camera.main

    float cd;

    void Awake() { if (!cam) cam = Camera.main; }

    void Update()
    {
        if (!firePoint || !projectilePrefab || !cam) return;

        cd -= Time.deltaTime;

        bool pressed = ClickDown();
        bool held    = ClickHeld();

        // Fire instantly on press
        if (pressed) TryFire();

        // While held, fire whenever cooldown elapses
        if (held && cd <= 0f) TryFire();
    }

    void TryFire()
    {
        Vector3 m = MouseWorld();
        Vector2 dir = (Vector2)(m - firePoint.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        var proj = go.GetComponent<SpellProjectile>();
        if (proj) proj.Launch(dir);
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
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
        w.z = firePoint.position.z;
        return w;
    }
}
