using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : EntityStats
{
    [Header("Progression")]
    [Min(1)] public int level = 1;
    public int xp = 0;
    public int xpToNext = 50;

    [Header("Mana & Attributes")]
    public int maxMP = 100;
    public int mp = 100;

    public int spd = 25;
    public int dex = 10;
    public int vit = 10;
    public int wis = 10;
    public int luck = 0;

    [Header("Bonuses (runtime)")]
    [SerializeField] int b_maxMP, b_spd, b_dex, b_vit, b_wis, b_luck;

    [Header("Events")]
    public UnityEvent onLevelUp;

    // Effective
    public int EffMaxMP => maxMP + b_maxMP;
    public int EffSPD   => spd + b_spd;
    public int EffDEX   => dex + b_dex;
    public int EffVIT   => vit + b_vit;
    public int EffWIS   => wis + b_wis;
    public int EffLUCK  => luck + b_luck;

    // --- Back-compat: MP property for existing code ---
    public int MP
    {
        get => mp;
        set
        {
            mp = Mathf.Clamp(value, 0, EffMaxMP);
            onStatChanged?.Invoke("mp", mp);
        }
    }

    // --- Back-compat: TakeDamage wrapper(s) ---
    public void TakeDamage(int rawDamage) => ApplyDamage(rawDamage);
    public void TakeDamage(int rawDamage, int defCap) => ApplyDamage(rawDamage, defCap);

    protected override void Awake()
    {
        base.Awake();
        mp = Mathf.Clamp(mp, 0, EffMaxMP);
    }

    // --- XP / Level ---
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        xp += amount;
        onStatChanged?.Invoke("xp", xp);

        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        onStatChanged?.Invoke("level", level);

        // Simple growth
        maxHP += 10;
        maxMP += 5;
        att   += 1;
        spd   += 1;
        dex   += 1;
        vit   += 1;
        wis   += 1;

        hp = EffMaxHP;
        mp = EffMaxMP;

        xpToNext = Mathf.RoundToInt(xpToNext * 1.25f) + 10;

        onStatChanged?.Invoke("maxHP", maxHP);
        onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        onStatChanged?.Invoke("hp", hp);

        onStatChanged?.Invoke("maxMP", maxMP);
        onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
        onStatChanged?.Invoke("mp", mp);

        onStatChanged?.Invoke("xpToNext", xpToNext);

        onLevelUp?.Invoke();
    }

    // --- MP helpers ---
    public void RestoreMP(int amount)
    {
        if (amount <= 0 || IsDead) return;
        MP = mp + amount; // uses property to clamp + event
    }

    public void UseMP(int amount)
    {
        if (amount <= 0 || IsDead) return;
        MP = mp - amount; // uses property to clamp + event
    }

    // --- Player-only bonuses; falls back to base for others ---
    public void SetPlayerBonus(string stat, int value)
    {
        switch (stat.ToLowerInvariant())
        {
            case "maxmp": b_maxMP = value; onStatChanged?.Invoke("maxMP_bonus", b_maxMP); break;
            case "spd":   b_spd   = value; onStatChanged?.Invoke("spd_bonus", b_spd);     break;
            case "dex":   b_dex   = value; onStatChanged?.Invoke("dex_bonus", b_dex);     break;
            case "vit":   b_vit   = value; onStatChanged?.Invoke("vit_bonus", b_vit);     break;
            case "wis":   b_wis   = value; onStatChanged?.Invoke("wis_bonus", b_wis);     break;
            case "luck":  b_luck  = value; onStatChanged?.Invoke("luck_bonus", b_luck);   break;
            default:
                SetBonus(stat, value);
                return;
        }
        mp = Mathf.Clamp(mp, 0, EffMaxMP);
        onStatChanged?.Invoke("mp", mp);
        onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
    }

    // --- Passive regen (optional) ---
    float _hpAcc, _mpAcc;
    void Update()
    {
        _hpAcc += EffVIT * Time.deltaTime / 5f;
        _mpAcc += EffWIS * Time.deltaTime / 5f;

        if (_hpAcc >= 1f) { int add = Mathf.FloorToInt(_hpAcc); _hpAcc -= add; Heal(add); }
        if (_mpAcc >= 1f) { int add = Mathf.FloorToInt(_mpAcc); _mpAcc -= add; RestoreMP(add); }
    }
}
