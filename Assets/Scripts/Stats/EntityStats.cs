using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class StatIntEvent : UnityEvent<string, int> {}
[System.Serializable] public class DeathEvent   : UnityEvent {}

[DisallowMultipleComponent]
public class EntityStats : MonoBehaviour
{
    [Header("Core")]
    [Min(1)] public int maxHP = 100;
    [SerializeField] protected int hp = 100;

    [Header("Combat")]
    public int att = 10;   // outgoing damage
    public int def = 0;    // flat reduction (cap applied in ApplyDamage)

    [Header("Bonuses (runtime)")]
    [SerializeField] protected int b_maxHP, b_att, b_def;

    [Header("Events")]
    public StatIntEvent onStatChanged; // (name, value)
    public DeathEvent   onDeath;       // fired when HP hits 0 (before EnemyDeath.HandleDeath)

    // Effective values
    public int EffMaxHP => maxHP + b_maxHP;
    public int EffATT   => att   + b_att;
    public int EffDEF   => def   + b_def;

    // Convenience
    public int HP    => hp;
    public int MaxHP => EffMaxHP;
    public bool IsDead => hp <= 0;

    protected virtual void Awake()
    {
        hp = Mathf.Clamp(hp, 0, EffMaxHP);
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        hp = Mathf.Clamp(hp + amount, 0, EffMaxHP);
        onStatChanged?.Invoke("hp", hp);
    }

    /// <summary>Flat DEF with minimum 1 damage; DEF capped (default 25). Triggers Die() at 0 HP.</summary>
    public virtual void ApplyDamage(int rawDamage, int defCap = 25)
    {
        if (rawDamage <= 0 || IsDead) return;

        int effDef = Mathf.Min(EffDEF, defCap);
        int final  = Mathf.Max(1, rawDamage - effDef);

        hp = Mathf.Clamp(hp - final, 0, EffMaxHP);
        onStatChanged?.Invoke("hp", hp);

        if (hp == 0) Die();
    }

    protected virtual void Die()
    {
        // Let listeners react first (e.g., stop AI, play animations)
        try { onDeath?.Invoke(); } catch {}

        // Delegate to Death component if present
        var death = GetComponent<EnemyDeath>();
        if (death != null)
        {
            death.HandleDeath(this);
            return;
        }

        // Fallback (no EnemyDeath component attached): just destroy
        Destroy(gameObject);
    }

    // ---- Optional helpers ----
    public virtual void Set(string stat, int value)
    {
        switch (stat.ToLowerInvariant())
        {
            case "hp":
                hp = Mathf.Clamp(value, 0, EffMaxHP);
                onStatChanged?.Invoke("hp", hp);
                if (hp == 0) Die();
                break;

            case "maxhp":
                maxHP = Mathf.Max(1, value);
                hp = Mathf.Min(hp, EffMaxHP);
                onStatChanged?.Invoke("maxHP", maxHP);
                onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
                onStatChanged?.Invoke("hp", hp);
                if (hp == 0) Die();
                break;

            case "att":
                att = Mathf.Max(0, value);
                onStatChanged?.Invoke("att", att);
                break;

            case "def":
                def = Mathf.Max(0, value);
                onStatChanged?.Invoke("def", def);
                break;

            default:
                Debug.LogWarning($"[EntityStats] Unknown stat '{stat}'");
                break;
        }
    }

    public virtual void SetBonus(string stat, int value)
    {
        int oldEffMax = EffMaxHP;
        switch (stat.ToLowerInvariant())
        {
            case "maxhp": b_maxHP = value; onStatChanged?.Invoke("maxHP_bonus", b_maxHP); break;
            case "att":   b_att   = value; onStatChanged?.Invoke("att_bonus", b_att);     break;
            case "def":   b_def   = value; onStatChanged?.Invoke("def_bonus", b_def);     break;
            default:
                Debug.LogWarning($"[EntityStats] Unknown bonus '{stat}'");
                break;
        }

        if (EffMaxHP != oldEffMax)
        {
            hp = Mathf.Clamp(hp, 0, EffMaxHP);
            onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
            onStatChanged?.Invoke("hp", hp);
            if (hp == 0) Die();
        }
    }
}
