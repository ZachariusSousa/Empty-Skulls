using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : EntityStats
{
    // ===== PLAYER-ONLY GROUPS =====
    [System.Serializable]
    public struct ProgressionGroup
    {
        [Header("Progression")]
        [Min(1)] public int level;
        public int xp;
        public int xpToNext;
    }

    [System.Serializable]
    public struct PlayerEventsGroup
    {
        [Header("Player Events")]
        public UnityEvent onLevelUp;
    }

    public ProgressionGroup prog = new ProgressionGroup { level = 1, xp = 0, xpToNext = 50 };
    public PlayerEventsGroup pevents;

    protected override void Awake()
    {
        base.Awake();
        // Ensure current values are clamped to effective caps after deserialization
        // (EntityStats.Awake already does this; kept for clarity)
    }

    // ===== XP / LEVEL =====
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        prog.xp += amount;
        eventsGroup.onStatChanged?.Invoke("xp", prog.xp);

        while (prog.xp >= prog.xpToNext)
        {
            prog.xp -= prog.xpToNext;
            LevelUp();
        }
    }

    void LevelUp()
    {
        prog.level++;
        eventsGroup.onStatChanged?.Invoke("level", prog.level);

        // Simple growth curve — tweak to taste
        baseStats.maxHP += 10;
        baseStats.maxMP += 5;
        baseStats.att   += 1;
        baseStats.spd   += 1;
        baseStats.dex   += 1;
        baseStats.vit   += 1;
        baseStats.wis   += 1;

        // Refill on level
        current.hp = EffMaxHP;
        current.mp = EffMaxMP;

        prog.xpToNext = Mathf.RoundToInt(prog.xpToNext * 1.25f) + 10;

        // Notify
        eventsGroup.onStatChanged?.Invoke("maxHP", baseStats.maxHP);
        eventsGroup.onStatChanged?.Invoke("maxMP", baseStats.maxMP);
        eventsGroup.onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        eventsGroup.onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
        eventsGroup.onStatChanged?.Invoke("xpToNext", prog.xpToNext);

        pevents.onLevelUp?.Invoke();
    }

    // ===== Optional passive regen (VIT/WIS) =====
    float _hpAcc, _mpAcc;
    void Update()
    {
        if (IsDead) return;

        _hpAcc += EffVIT * Time.deltaTime / 5f;
        _mpAcc += EffWIS * Time.deltaTime / 5f;

        if (_hpAcc >= 1f) { int add = Mathf.FloorToInt(_hpAcc); _hpAcc -= add; Heal(add); }
        if (_mpAcc >= 1f) { int add = Mathf.FloorToInt(_mpAcc); _mpAcc -= add; RestoreMP(add); }
    }

    [ContextMenu("Refill HP/MP")]
    void Ctx_RefillHPMP()
    {
        current.hp = EffMaxHP;
        current.mp = EffMaxMP;
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
    }

    // ===== BACK-COMPAT SHIMS (so old scripts keep compiling) =====
    // Old flat fields/events: xp, xpToNext, onStatChanged, TakeDamage(...)
    public int xp
    {
        get => prog.xp;
        set { prog.xp = Mathf.Max(0, value); eventsGroup.onStatChanged?.Invoke("xp", prog.xp); }
    }

    public int xpToNext
    {
        get => prog.xpToNext;
        set { prog.xpToNext = Mathf.Max(1, value); eventsGroup.onStatChanged?.Invoke("xpToNext", prog.xpToNext); }
    }

    public StatIntEvent onStatChanged => eventsGroup.onStatChanged;

    public void TakeDamage(int amount, int defCap) => ApplyDamage(amount, transform.position, false, defCap);

}
