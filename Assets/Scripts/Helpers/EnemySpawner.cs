using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    public enum AreaMode { Points, Box }

    [System.Serializable]
    public struct EnemyOption
    {
        public GameObject prefab;
        [Min(0f)] public float weight;
        [Min(0)] public int cost;
        [Min(0)] public int maxAliveOfThis;
    }

    [System.Serializable]
    public struct Area
    {
        public AreaMode mode;
        public Transform[] points;
        public Vector2 boxSize;
        public float pointJitter;
    }

    [System.Serializable]
    public struct Rules
    {
        [Min(1)] public int maxAlive;
        public float startDelay;
        public Vector2 intervalRange;
        public AnimationCurve spawnsPerMinuteOverTime;
        public int baseBudgetPerMinute;
        public float difficultyRampPerMinute;
        public float minDistanceFromPlayer;
        public float maxDistanceFromPlayer;
        public bool requireOutsideView;
        public float viewMargin;
        public int positionRetries;
    }

    [Header("Refs")]
    public Transform player;
    public Transform parentForSpawns;
    public Camera viewCam;

    [Header("Area")]
    public Area area;

    [Header("Rules")]
    public Rules rules = new Rules
    {
        maxAlive = 30,
        startDelay = 2f,
        intervalRange = new Vector2(0.6f, 1.8f),
        spawnsPerMinuteOverTime = null,
        baseBudgetPerMinute = 20,
        difficultyRampPerMinute = 0.25f,
        minDistanceFromPlayer = 6f,
        maxDistanceFromPlayer = 0f,
        requireOutsideView = false,
        viewMargin = 1.0f,
        positionRetries = 4
    };

    [Header("Enemies")]
    public EnemyOption[] options;

    [Header("Random")]
    public int seed;
    public bool useDeterministic;

    readonly List<GameObject> _alive = new List<GameObject>(128);
    readonly Dictionary<GameObject, int> _aliveByPrefab = new Dictionary<GameObject, int>(32);
    float _t;
    float _nextAt;
    float _budgetBank;
    System.Random _rng;

    void Awake()
    {
        if (useDeterministic) _rng = new System.Random(seed == 0 ? 12345 : seed);
        if (parentForSpawns == null) parentForSpawns = transform;
        if (rules.spawnsPerMinuteOverTime == null) rules.spawnsPerMinuteOverTime = AnimationCurve.Linear(0, 1, 10, 2);
        if (!viewCam) viewCam = Camera.main;
    }

    void OnEnable()
    {
        _t = 0f;
        _nextAt = rules.startDelay;
        _budgetBank = 0f;
        _alive.Clear();
        _aliveByPrefab.Clear();
    }

    void Update()
    {
        _t += Time.deltaTime;
        CleanupDead();
        AccumulateBudget();
        if (_t < _nextAt) return;
        if (_alive.Count >= rules.maxAlive) { ScheduleNext(); return; }

        var opt = PickEnemy();
        if (opt.prefab == null) { ScheduleNext(); return; }

        Vector2? pos = null;
        var retries = Mathf.Max(1, rules.positionRetries);
        while (retries-- > 0 && pos == null)
        {
            var p = PickPosition();
            if (!p.HasValue) break;

            if (player)
            {
                var minD = CurrentMinDistanceFromPlayer();
                var maxD = Mathf.Max(0f, rules.maxDistanceFromPlayer);
                var sq = ((Vector2)player.position - p.Value).sqrMagnitude;

                if (minD > 0f && sq < minD * minD) { continue; }
                if (maxD > 0f && sq > maxD * maxD) { continue; }
            }

            pos = p;
        }
        if (!pos.HasValue) { ScheduleNext(0.25f); return; }

        var go = Instantiate(opt.prefab, pos.Value, Quaternion.identity, parentForSpawns);
        Register(opt.prefab, go);
        _budgetBank -= opt.cost;
        ScheduleNext();
    }

    float CurrentMinDistanceFromPlayer()
    {
        var baseMin = Mathf.Max(0f, rules.minDistanceFromPlayer);
        if (!rules.requireOutsideView || !viewCam || !viewCam.orthographic || !player) return baseMin;

        var halfH = viewCam.orthographicSize;
        var halfW = halfH * viewCam.aspect;
        var radius = Mathf.Sqrt(halfW * halfW + halfH * halfH);
        return Mathf.Max(baseMin, radius + Mathf.Max(0f, rules.viewMargin));
    }

    void Register(GameObject prefabKey, GameObject instance)
    {
        if (!_aliveByPrefab.ContainsKey(prefabKey)) _aliveByPrefab[prefabKey] = 0;
        _aliveByPrefab[prefabKey] += 1;
        _alive.Add(instance);

        var token = instance.AddComponent<_EnemySpawnToken>();
        token.spawner = this;
        token.prefabKey = prefabKey;
        token.instance = instance;
    }

    public void Unregister(GameObject prefabKey, GameObject instance)
    {
        if (prefabKey && _aliveByPrefab.ContainsKey(prefabKey))
            _aliveByPrefab[prefabKey] = Mathf.Max(0, _aliveByPrefab[prefabKey] - 1);

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null || _alive[i] == instance)
                _alive.RemoveAt(i);
        }
    }

    void CleanupDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null) _alive.RemoveAt(i);
        }
    }

    void AccumulateBudget()
    {
        var minutes = _t / 60f;
        var ramp = 1f + rules.difficultyRampPerMinute * minutes;
        var curve = rules.spawnsPerMinuteOverTime != null ? Mathf.Max(0.01f, rules.spawnsPerMinuteOverTime.Evaluate(minutes)) : 1f;
        var perMinute = Mathf.Max(0, rules.baseBudgetPerMinute) * ramp * curve;
        _budgetBank = Mathf.Min(_budgetBank + perMinute * Time.deltaTime / 60f, rules.baseBudgetPerMinute * 3f * ramp);
    }

    EnemyOption PickEnemy()
    {
        var list = new List<EnemyOption>(options.Length);
        foreach (var o in options)
        {
            if (!o.prefab) continue;
            if (o.cost > _budgetBank + 0.0001f) continue;
            if (o.maxAliveOfThis > 0)
            {
                var alive = _aliveByPrefab.TryGetValue(o.prefab, out var n) ? n : 0;
                if (alive >= o.maxAliveOfThis) continue;
            }
            if (o.weight <= 0f) continue;
            list.Add(o);
        }
        if (list.Count == 0) return default;

        float sum = 0f;
        for (int i = 0; i < list.Count; i++) sum += list[i].weight;
        float r = Rand01() * sum, acc = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            acc += list[i].weight;
            if (r <= acc) return list[i];
        }
        return list[list.Count - 1];
    }

    Vector2? PickPosition()
    {
        if (area.mode == AreaMode.Points && area.points != null && area.points.Length > 0)
        {
            var p = area.points[RandRangeInt(0, area.points.Length)];
            if (!p) return null;
            var j = area.pointJitter <= 0f ? Vector2.zero : RandInsideCircle(area.pointJitter);
            return (Vector2)p.position + j;
        }
        var size = area.boxSize;
        if (size.x <= 0f || size.y <= 0f) size = new Vector2(10, 6);
        var half = size * 0.5f;
        var local = new Vector2(RandRange(-half.x, half.x), RandRange(-half.y, half.y));
        return (Vector2)transform.position + local;
    }

    void ScheduleNext(float? overrideSeconds = null)
    {
        var minutes = Mathf.Max(0f, _t / 60f);
        var curve = rules.spawnsPerMinuteOverTime != null ? Mathf.Max(0.01f, rules.spawnsPerMinuteOverTime.Evaluate(minutes)) : 1f;
        var baseMin = Mathf.Max(0.05f, rules.intervalRange.x);
        var baseMax = Mathf.Max(baseMin, rules.intervalRange.y);
        var scaled = Mathf.Lerp(baseMax, baseMin, Mathf.Clamp01((curve - 1f) * 0.5f + 0.5f));
        var jitter = Mathf.Lerp(baseMin, baseMax, Rand01());
        var dt = overrideSeconds.HasValue ? overrideSeconds.Value : Mathf.Clamp(jitter * scaled, baseMin, baseMax);
        _nextAt = _t + dt;
    }

    float Rand01() => useDeterministic ? (float)_rng.NextDouble() : Random.value;
    float RandRange(float a, float b) => a + (b - a) * Rand01();
    int RandRangeInt(int a, int b) => Mathf.FloorToInt(RandRange(a, b));

    Vector2 RandInsideCircle(float r)
    {
        var t = 2f * Mathf.PI * Rand01();
        var u = Rand01() + Rand01();
        var rad = u > 1f ? 2f - u : u;
        return new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * rad * r;
    }

    class _EnemySpawnToken : MonoBehaviour
    {
        public EnemySpawner spawner;
        public GameObject prefabKey;
        public GameObject instance;

        void OnDisable() { if (spawner) spawner.Unregister(prefabKey, instance); }
        void OnDestroy() { if (spawner) spawner.Unregister(prefabKey, instance); }
    }

    public void ForceSpawn(int count = 1)
    {
        for (int i = 0; i < count; i++) _nextAt = 0f;
    }
}
