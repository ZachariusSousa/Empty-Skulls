using System.Collections.Generic;
using UnityEngine;

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

        if (prefab)
        {
            for (int i = 0; i < warmup; i++)
            {
                var dt = Instantiate(prefab, transform);
                dt.gameObject.SetActive(false);
                _pool.Enqueue(dt);
            }
        }
        else
        {
            Debug.LogWarning("[DamageTextPool] Prefab not assigned.");
        }
    }

    public static void Spawn(int amount, Vector3 pos, Color color, bool crit = false)
    {
        if (!Instance || !Instance.prefab) return;

        DamageText dt = (Instance._pool.Count > 0)
            ? Instance._pool.Dequeue()
            : Instantiate(Instance.prefab, Instance.transform);

        dt.gameObject.SetActive(true);
        dt.Play(amount, pos, color, crit);
    }

    public static void Release(DamageText dt)
    {
        if (!dt) return;
        dt.gameObject.SetActive(false);
        if (Instance) Instance._pool.Enqueue(dt);
        else Destroy(dt.gameObject);
    }
}
