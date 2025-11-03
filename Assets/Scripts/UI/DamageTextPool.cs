using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance { get; private set; }

    [Header("Setup")]
    public DamageText prefab;
    public int warmup = 16;

    readonly Queue<DamageText> _pool = new Queue<DamageText>(64);

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!prefab)
        {
            Debug.LogWarning("[DamageTextPool] Prefab not assigned.");
            return;
        }

        // Prewarm – do NOT parent to this object to avoid inheriting off-screen transforms.
        for (int i = 0; i < warmup; i++)
        {
            var dt = Instantiate(prefab);     // no parent
            dt.gameObject.SetActive(false);
            dt.transform.position = Vector3.zero;
            dt.transform.rotation = Quaternion.identity;
            dt.transform.localScale = Vector3.one;
            _pool.Enqueue(dt);
        }
    }

    public static void Spawn(int amount, Vector3 pos, Color color, bool crit = false)
    {
        if (!Instance || !Instance.prefab) return;

        DamageText dt = (Instance._pool.Count > 0)
            ? Instance._pool.Dequeue()
            : Instantiate(Instance.prefab);   // no parent

        // Ensure sane transform BEFORE play
        dt.transform.SetParent(null, worldPositionStays: true);
        dt.transform.rotation = Quaternion.identity;
        dt.transform.localScale = Vector3.one;

        dt.gameObject.SetActive(true);
        dt.Play(amount, pos, color, crit);
    }

    public static void Release(DamageText dt)
    {
        if (!dt) return;
        dt.gameObject.SetActive(false);

        // Reset transform so recycled instances don’t carry bad offsets
        dt.transform.SetParent(null, worldPositionStays: true);
        dt.transform.position = Vector3.zero;
        dt.transform.rotation = Quaternion.identity;
        dt.transform.localScale = Vector3.one;

        if (Instance) Instance._pool.Enqueue(dt);
        else Destroy(dt.gameObject);
    }
}
