using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class StatIntEvent   : UnityEvent<string, int> {}
[System.Serializable] public class DeathEvent     : UnityEvent {}
[System.Serializable] public class DamageNumEvent : UnityEvent<int, Vector3, bool> {} // (final, pos, crit)

[DisallowMultipleComponent]
public class EntityStats : MonoBehaviour
{
    // ===== GROUPS =====
    [System.Serializable]
    public struct BaseStatsGroup
    {
        [Header("Base Stats (RotMG-style)")]
        [Min(1)] public int maxHP; // 1
        [Min(0)] public int maxMP; // 2
        [Min(0)] public int att;   // 3
        [Min(0)] public int def;   // 4
        [Min(0)] public int spd;   // 5
        [Min(0)] public int dex;   // 6
        [Min(0)] public int vit;   // 7
        [Min(0)] public int wis;   // 8
    }

    [System.Serializable]
    public struct CurrentGroup
    {
        [Header("Current (Runtime)")]
        [SerializeField] public int hp;
        [SerializeField] public int mp;
    }

    [System.Serializable]
    public struct BonusGroup
    {
        [Header("Bonuses (Runtime)")]
        [SerializeField] public int b_maxHP;
        [SerializeField] public int b_maxMP;
        [SerializeField] public int b_att;
        [SerializeField] public int b_def;
        [SerializeField] public int b_spd;
        [SerializeField] public int b_dex;
        [SerializeField] public int b_vit;
        [SerializeField] public int b_wis;
    }

    [System.Serializable]
    public struct DamageNumbersGroup
    {
        [Header("Damage Numbers")]
        public bool showDamageNumbers;
        public Color normalHitColor;
        public Color critHitColor;
    }

    [System.Serializable]
    public struct EventsGroup
    {
        [Header("Events")]
        public StatIntEvent onStatChanged; // (name, value)
        public DeathEvent   onDeath;
        public DamageNumEvent onDamaged;   // (finalDamage, hitPos, crit)
    }

    // ===== INSTANCES =====
    public BaseStatsGroup baseStats = new BaseStatsGroup
    {
        maxHP = 100, maxMP = 0, att = 10, def = 0, spd = 0, dex = 0, vit = 0, wis = 0
    };
    public CurrentGroup current = new CurrentGroup { hp = 100, mp = 0 };
    public BonusGroup bonus; // defaults 0
    public DamageNumbersGroup dmgNums = new DamageNumbersGroup
    {
        showDamageNumbers = true,
        normalHitColor = Color.white,
        critHitColor = new Color(1f, 0.9f, 0.25f)
    };
    public EventsGroup eventsGroup;

    // ===== EFFECTIVE VALUES =====
    public int EffMaxHP => Mathf.Max(1, baseStats.maxHP + bonus.b_maxHP);
    public int EffMaxMP => Mathf.Max(0, baseStats.maxMP + bonus.b_maxMP);
    public int EffATT   => Mathf.Max(0, baseStats.att   + bonus.b_att);
    public int EffDEF   => Mathf.Max(0, baseStats.def   + bonus.b_def);
    public int EffSPD   => Mathf.Max(0, baseStats.spd   + bonus.b_spd);
    public int EffDEX   => Mathf.Max(0, baseStats.dex   + bonus.b_dex);
    public int EffVIT   => Mathf.Max(0, baseStats.vit   + bonus.b_vit);
    public int EffWIS   => Mathf.Max(0, baseStats.wis   + bonus.b_wis);

    // Shortcuts
    public int HP => current.hp;
    public int MP => current.mp;
    public bool IsDead => current.hp <= 0;

    protected virtual void Awake()
    {
        current.hp = Mathf.Clamp(current.hp, 0, EffMaxHP);
        current.mp = Mathf.Clamp(current.mp, 0, EffMaxMP);
    }

    // ===== CORE API =====
    public virtual void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        current.hp = Mathf.Clamp(current.hp + amount, 0, EffMaxHP);
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
    }

    public virtual void RestoreMP(int amount)
    {
        if (amount <= 0 || IsDead) return;
        current.mp = Mathf.Clamp(current.mp + amount, 0, EffMaxMP);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
    }

    public virtual void UseMP(int amount)
    {
        if (amount <= 0 || IsDead) return;
        current.mp = Mathf.Clamp(current.mp - amount, 0, EffMaxMP);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
    }

    public virtual void ApplyDamage(int rawDamage, Vector3 hitPos, bool crit = false, int defCap = 25)
    {
        if (IsDead || rawDamage <= 0) return;

        // Clamp defense to cap, like RotMG
        int effDef = Mathf.Min(EffDEF, defCap);
        int finalDamage = Mathf.Max(1, rawDamage - effDef);

        current.hp = Mathf.Clamp(current.hp - finalDamage, 0, EffMaxHP);
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);

        // Damage number
        if (dmgNums.showDamageNumbers)
        {
            var color = crit ? dmgNums.critHitColor : dmgNums.normalHitColor;
            DamageTextPool.Spawn(finalDamage, hitPos, color, crit);
        }

        // Optional event
        eventsGroup.onDamaged?.Invoke(finalDamage, hitPos, crit);

        if (current.hp <= 0)
            Die();
    }

    protected virtual void Die()
    {
        try { eventsGroup.onDeath?.Invoke(); } catch {}

        var death = GetComponent<EnemyDeath>();
        if (death != null) { death.HandleDeath(this); return; }

        Destroy(gameObject);
    }

    // ===== Helpers to set via string (optional) =====
    public virtual void Set(string stat, int value)
    {
        switch (stat.ToLowerInvariant())
        {
            case "hp":
                current.hp = Mathf.Clamp(value, 0, EffMaxHP);
                eventsGroup.onStatChanged?.Invoke("hp", current.hp);
                if (current.hp == 0) Die();
                break;
            case "mp":
                current.mp = Mathf.Clamp(value, 0, EffMaxMP);
                eventsGroup.onStatChanged?.Invoke("mp", current.mp);
                break;
            case "maxhp":
                baseStats.maxHP = Mathf.Max(1, value);
                ClampCurrents();
                eventsGroup.onStatChanged?.Invoke("maxHP", baseStats.maxHP);
                eventsGroup.onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
                break;
            case "maxmp":
                baseStats.maxMP = Mathf.Max(0, value);
                ClampCurrents();
                eventsGroup.onStatChanged?.Invoke("maxMP", baseStats.maxMP);
                eventsGroup.onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
                break;
            case "att": baseStats.att = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("att", baseStats.att); break;
            case "def": baseStats.def = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("def", baseStats.def); break;
            case "spd": baseStats.spd = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("spd", baseStats.spd); break;
            case "dex": baseStats.dex = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("dex", baseStats.dex); break;
            case "vit": baseStats.vit = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("vit", baseStats.vit); break;
            case "wis": baseStats.wis = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("wis", baseStats.wis); break;
            default:
                Debug.LogWarning($"[EntityStats] Unknown stat '{stat}'"); break;
        }
    }

    public virtual void SetBonus(string stat, int value)
    {
        int oldEffHP = EffMaxHP;
        int oldEffMP = EffMaxMP;

        switch (stat.ToLowerInvariant())
        {
            case "maxhp": bonus.b_maxHP = value; eventsGroup.onStatChanged?.Invoke("maxHP_bonus", bonus.b_maxHP); break;
            case "maxmp": bonus.b_maxMP = value; eventsGroup.onStatChanged?.Invoke("maxMP_bonus", bonus.b_maxMP); break;
            case "att":   bonus.b_att   = value; eventsGroup.onStatChanged?.Invoke("att_bonus",   bonus.b_att);   break;
            case "def":   bonus.b_def   = value; eventsGroup.onStatChanged?.Invoke("def_bonus",   bonus.b_def);   break;
            case "spd":   bonus.b_spd   = value; eventsGroup.onStatChanged?.Invoke("spd_bonus",   bonus.b_spd);   break;
            case "dex":   bonus.b_dex   = value; eventsGroup.onStatChanged?.Invoke("dex_bonus",   bonus.b_dex);   break;
            case "vit":   bonus.b_vit   = value; eventsGroup.onStatChanged?.Invoke("vit_bonus",   bonus.b_vit);   break;
            case "wis":   bonus.b_wis   = value; eventsGroup.onStatChanged?.Invoke("wis_bonus",   bonus.b_wis);   break;
            default:
                Debug.LogWarning($"[EntityStats] Unknown bonus '{stat}'"); break;
        }

        if (EffMaxHP != oldEffHP || EffMaxMP != oldEffMP)
        {
            ClampCurrents();
            eventsGroup.onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
            eventsGroup.onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
            eventsGroup.onStatChanged?.Invoke("hp", current.hp);
            eventsGroup.onStatChanged?.Invoke("mp", current.mp);
            if (current.hp == 0) Die();
        }
    }

    void ClampCurrents()
    {
        current.hp = Mathf.Clamp(current.hp, 0, EffMaxHP);
        current.mp = Mathf.Clamp(current.mp, 0, EffMaxMP);
    }

    // ===== Context menu =====
    [ContextMenu("Refill HP/MP")]
    void Ctx_Refill()
    {
        current.hp = EffMaxHP;
        current.mp = EffMaxMP;
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
    }

    // ===== BACK-COMPAT SHIMS =====
    public DeathEvent onDeath => eventsGroup.onDeath;
    public StatIntEvent onStatChanged => eventsGroup.onStatChanged;
}
