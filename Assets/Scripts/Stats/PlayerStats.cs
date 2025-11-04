using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : EntityStats
{
    [System.Serializable]
    public struct ProgressionGroup
    {
        [Min(1)] public int level;
        public int xp;
        public int xpToNext;
    }

    [System.Serializable]
    public struct PlayerEventsGroup
    {
        public UnityEvent onLevelUp;
    }

    [System.Serializable]
    public struct RegenGroup
    {
        [Header("Enable")]
        public bool enableHP;
        public bool enableMP;
        [Header("Tuning")]
        [Min(0.05f)] public float tickSeconds;
        [Tooltip("Base regen per second independent of stats")]
        public float baseHPPerSec;
        public float baseMPPerSec;
        [Tooltip("Multiplier per point of VIT/WIS")]
        public float hpPerVit;
        public float mpPerWis;
        public bool useEffectiveStats;
    }

    public ProgressionGroup prog = new ProgressionGroup { level = 1, xp = 0, xpToNext = 50 };
    public PlayerEventsGroup pevents;

    public RegenGroup regen = new RegenGroup
    {
        enableHP = true,
        enableMP = true,
        tickSeconds = 1f,
        baseHPPerSec = 1.2f,
        baseMPPerSec = 0.6f,
        hpPerVit = 0.14f,
        mpPerWis = 0.07f,
        useEffectiveStats = true
    };

    float _accHP, _accMP, _tick;

    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (IsDead) return;
        float dt = Time.deltaTime;
        _tick += dt;

        if (regen.enableHP && current.hp < EffMaxHP)
        {
            int vit = regen.useEffectiveStats ? EffVIT : baseStats.vit;
            float hpPerSec = regen.baseHPPerSec + regen.hpPerVit * vit;
            _accHP += hpPerSec * dt;
        }

        if (regen.enableMP && current.mp < EffMaxMP)
        {
            int wis = regen.useEffectiveStats ? EffWIS : baseStats.wis;
            float mpPerSec = regen.baseMPPerSec + regen.mpPerWis * wis;
            _accMP += mpPerSec * dt;
        }

        if (_tick >= regen.tickSeconds)
        {
            _tick -= regen.tickSeconds;

            int hpGain = Mathf.FloorToInt(_accHP);
            int mpGain = Mathf.FloorToInt(_accMP);
            _accHP -= hpGain;
            _accMP -= mpGain;

            if (hpGain > 0) Heal(hpGain);
            if (mpGain > 0) RestoreMP(mpGain);
        }
    }

    // ===== Progression =====
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

        baseStats.maxHP += 10;
        baseStats.maxMP += 5;
        baseStats.att += 1;
        baseStats.spd += 1;
        baseStats.dex += 1;
        baseStats.vit += 1;
        baseStats.wis += 1;

        current.hp = EffMaxHP;
        current.mp = EffMaxMP;

        prog.xpToNext = Mathf.RoundToInt(prog.xpToNext * 1.25f) + 10;

        eventsGroup.onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        eventsGroup.onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
        eventsGroup.onStatChanged?.Invoke("xpToNext", prog.xpToNext);

        pevents.onLevelUp?.Invoke();
    }

    [ContextMenu("Refill HP/MP")]
    void Ctx_RefillHPMP()
    {
        current.hp = EffMaxHP;
        current.mp = EffMaxMP;
        eventsGroup.onStatChanged?.Invoke("hp", current.hp);
        eventsGroup.onStatChanged?.Invoke("mp", current.mp);
    }

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
