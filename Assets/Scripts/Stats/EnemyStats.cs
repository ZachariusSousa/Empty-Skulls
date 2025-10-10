using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 0.5f;
    public Vector2Int quantityRange = new Vector2Int(1, 1);
    public Vector2 spawnSpread = new Vector2(0.4f, 0.4f); // random offset
}

public class EnemyStats : MonoBehaviour
{
    [Header("Level & Rewards")]
    [Min(1)] public int level = 1;
    public int xpReward = 10;

    [Header("Vitals")]
    public int maxHP = 50;
    public int hp = 50;
    [Tooltip("HP regenerated per second.")]
    public float hpRegenPerSec = 0f;

    [Header("Defense (optional)")]
    public int def = 0;              // flat reduction
    public int defCap = 25;          // clamp like RotMG

    [Header("Drops")]
    public DropEntry[] drops;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent onDamaged;

    float _hpAcc;                    // regen accumulator (sub-1 hp)
    GameObject _lastDamager;         // who hit me last (to grant XP)

    void Update()
    {
        if (hp <= 0 || hpRegenPerSec <= 0f) return;
        _hpAcc += hpRegenPerSec * Time.deltaTime;
        if (_hpAcc >= 1f)
        {
            int add = Mathf.FloorToInt(_hpAcc);
            _hpAcc -= add;
            Heal(add);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || hp <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
    }

    public void TakeDamage(int rawDamage, GameObject source = null)
    {
        if (hp <= 0 || rawDamage <= 0) return;

        int effDef = Mathf.Min(def, defCap);
        int final = Mathf.Max(1, rawDamage - effDef);

        hp = Mathf.Clamp(hp - final, 0, maxHP);
        _lastDamager = source ? source : _lastDamager;
        onDamaged?.Invoke();

        if (hp == 0) Die();
    }

    void Die()
    {
        // 1) Grant XP to killer if they have PlayerStats
        if (_lastDamager)
        {
            var ps = _lastDamager.GetComponent<PlayerStats>();
            if (ps) ps.AddXP(xpReward);
        }

        // 2) Spawn drops
        if (drops != null)
        {
            foreach (var d in drops)
            {
                if (!d.prefab) continue;
                if (UnityEngine.Random.value <= d.chance)
                {
                    int q = Mathf.Clamp(UnityEngine.Random.Range(d.quantityRange.x, d.quantityRange.y + 1), 1, 999);
                    for (int i = 0; i < q; i++)
                    {
                        Vector2 off = new Vector2(
                            UnityEngine.Random.Range(-d.spawnSpread.x, d.spawnSpread.x),
                            UnityEngine.Random.Range(-d.spawnSpread.y, d.spawnSpread.y)
                        );
                        Instantiate(d.prefab, (Vector2)transform.position + off, Quaternion.identity);
                    }
                }
            }
        }

        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
