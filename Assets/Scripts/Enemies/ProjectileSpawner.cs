using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public enum Pattern { Straight, Cone, Arc, Circle, RandomInArc }

    [Header("Rotation")]
    public Transform pivot;           // defaults to self
    public float spinSpeed = 90f;     // deg/sec

    [Header("Targeting")]
    public Transform target;          // assign the player (or any Transform)

    [System.Serializable]
    public class Module
    {
        public string label = "Gun";
        public GameObject projectilePrefab;

        public Pattern pattern = Pattern.Straight;
        public int shots = 1;
        public float fireRate = 2f;

        public float coneSpread = 30f;
        public float arcMin = -60f;
        public float arcMax = 60f;

        public float angleOffsetDeg = 0f;

        // New: aim at target instead of pivot.up
        public bool aimAtTarget = false;

        [HideInInspector] public float cd;
    }

    public Module[] modules;

    void Awake()
    {
        if (!pivot) pivot = transform;
    }

    void Update()
    {
        if (Mathf.Abs(spinSpeed) > 0.0001f)
            pivot.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);

        if (modules == null) return;

        for (int i = 0; i < modules.Length; i++)
        {
            var m = modules[i];
            if (!m.projectilePrefab) continue;

            m.cd -= Time.deltaTime;
            if (m.cd <= 0f)
            {
                Fire(m);
                m.cd = 1f / Mathf.Max(0.01f, m.fireRate);
            }
        }
    }

    void Fire(Module m)
    {
        float baseAngle;

        if (m.aimAtTarget && target)
        {
            Vector2 toTarget = (Vector2)(target.position - pivot.position);
            if (toTarget.sqrMagnitude < 0.000001f) toTarget = pivot.up;
            baseAngle = AngleOf(toTarget);
        }
        else
        {
            baseAngle = AngleOf(pivot.up);
        }

        baseAngle += m.angleOffsetDeg;

        switch (m.pattern)
        {
            case Pattern.Straight:
                SpawnAtAngle(baseAngle, m);
                break;

            case Pattern.Cone:
                FireCone(baseAngle, m);
                break;

            case Pattern.Arc:
                FireArc(baseAngle, m);
                break;

            case Pattern.Circle:
                FireCircle(m);
                break;

            case Pattern.RandomInArc:
                FireRandom(baseAngle, m);
                break;
        }
    }

    void FireCone(float baseAngle, Module m)
    {
        if (m.shots <= 0) return;
        if (m.shots == 1) { SpawnAtAngle(baseAngle, m); return; }

        float step = m.coneSpread / (m.shots - 1);
        float start = baseAngle - m.coneSpread * 0.5f;
        for (int i = 0; i < m.shots; i++)
            SpawnAtAngle(start + step * i, m);
    }

    void FireArc(float baseAngle, Module m)
    {
        if (m.shots <= 0) return;

        float start = baseAngle + m.arcMin;
        float end   = baseAngle + m.arcMax;

        if (m.shots == 1) { SpawnAtAngle((start + end) * 0.5f, m); return; }

        float step = (end - start) / (m.shots - 1);
        for (int i = 0; i < m.shots; i++)
            SpawnAtAngle(start + step * i, m);
    }

    void FireCircle(Module m)
    {
        if (m.shots <= 0) return;

        float step = 360f / m.shots;
        for (int i = 0; i < m.shots; i++)
            SpawnAtAngle(step * i, m);
    }

    void FireRandom(float baseAngle, Module m)
    {
        if (m.shots <= 0) return;

        for (int i = 0; i < m.shots; i++)
            SpawnAtAngle(baseAngle + Random.Range(m.arcMin, m.arcMax), m);
    }

    void SpawnAtAngle(float angleDeg, Module m)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg - 90f);
        var go = Instantiate(m.projectilePrefab, pivot.position, rot);

        var proj = go.GetComponent<Projectile>();
        if (proj)
            proj.Launch(DirFromDeg(angleDeg));
    }

    static float AngleOf(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    static Vector2 DirFromDeg(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    void OnDrawGizmosSelected()
    {
        var p = pivot ? pivot : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p.position, p.position + p.up * 1.2f);
    }
}
