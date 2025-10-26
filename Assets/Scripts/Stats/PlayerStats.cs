using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable] public class StatIntEvent : UnityEvent<string, int> {}   // (statName, newValue)
[Serializable] public class DeathEvent : UnityEvent {}

public class PlayerStats : MonoBehaviour
{
    [Header("Core")]
    [Min(1)] public int level = 1;
    public int xp = 0;
    public int xpToNext = 50;

    [Header("Vitals")]
    public int maxHP = 100;
    public int hp = 100;
    public int maxMP = 100;
    public int mp = 100;

    [Header("Combat/Movement")]
    public int att = 10;   // damage power
    public int def = 0;    // flat damage reduction (cap applied in TakeDamage)
    public int spd = 25;   // move speed rating
    public int dex = 10;   // fire rate / attack speed rating
    public int vit = 10;   // HP regen rating
    public int wis = 10;   // MP regen / ability power
    public int luck = 0;   // crit chance or loot bias

    [Header("Events")]
    public StatIntEvent onStatChanged;    // invoke on any write
    public DeathEvent onDeath;
    public UnityEvent onLevelUp;

    [Header("Bonuses (runtime)")]
    [SerializeField] int b_att, b_def, b_spd, b_dex, b_vit, b_wis, b_luck, b_maxHP, b_maxMP;

    // Effective values (base + bonus)
    public int EffATT   => att   + b_att;
    public int EffDEF   => def   + b_def;
    public int EffSPD   => spd   + b_spd;
    public int EffDEX   => dex   + b_dex;
    public int EffVIT   => vit   + b_vit;
    public int EffWIS   => wis   + b_wis;
    public int EffLUCK  => luck  + b_luck;
    public int EffMaxHP => maxHP + b_maxHP;
    public int EffMaxMP => maxMP + b_maxMP;

    // Convenience reads
    public int MaxHP => maxHP;
    public int HP => hp;
    public int MaxMP => maxMP;
    public int MP => mp;

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

        // Simple growth curve
        maxHP += 10;
        maxMP += 5;
        att += 1;
        spd += 1;
        dex += 1;
        vit += 1;
        wis += 1;

        // Refill on level (use effective max)
        hp = EffMaxHP;
        mp = EffMaxMP;

        // Next XP requirement
        xpToNext = Mathf.RoundToInt(xpToNext * 1.25f) + 10;

        // Notify base and effective max changes for UI
        onStatChanged?.Invoke("maxHP", maxHP);
        onStatChanged?.Invoke("maxMP", maxMP);
        onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
        onStatChanged?.Invoke("xpToNext", xpToNext);
        onStatChanged?.Invoke("hp", hp);
        onStatChanged?.Invoke("mp", mp);

        onLevelUp?.Invoke();
    }

    // --- Vitals & Damage ---
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, EffMaxHP);
        onStatChanged?.Invoke("hp", hp);
    }

    public void RestoreMP(int amount)
    {
        if (amount <= 0) return;
        mp = Mathf.Clamp(mp + amount, 0, EffMaxMP);
        onStatChanged?.Invoke("mp", mp);
    }

    public void UseMP(int amount)
    {
        if (amount <= 0) return;
        mp = Mathf.Clamp(mp - amount, 0, EffMaxMP);
        onStatChanged?.Invoke("mp", mp);
    }

    // Flat-DEF mitigation with a minimum hit of 1 and a DEF cap
    public void TakeDamage(int rawDamage, int defCap = 25)
    {
        if (rawDamage <= 0 || hp <= 0) return;

        int effectiveDEF = Mathf.Min(EffDEF, defCap);
        int final = Mathf.Max(1, rawDamage - effectiveDEF);

        hp = Mathf.Clamp(hp - final, 0, EffMaxHP);
        onStatChanged?.Invoke("hp", hp);

        if (hp == 0) onDeath?.Invoke();
    }

    // --- Generic getters/setters (string keyed) ---
    public int Get(string stat)
    {
        switch (stat.ToLowerInvariant())
        {
            case "level": return level;
            case "xp": return xp;
            case "xptonext": return xpToNext;

            case "hp": return hp;
            case "maxhp": return maxHP;
            case "mp": return mp;
            case "maxmp": return maxMP;

            case "att": return att;
            case "def": return def;
            case "spd": return spd;
            case "dex": return dex;
            case "vit": return vit;
            case "wis": return wis;
            case "luck": return luck;

            // Effective exposes for UI
            case "maxhp_eff": return EffMaxHP;
            case "maxmp_eff": return EffMaxMP;
            case "att_eff":   return EffATT;
            case "def_eff":   return EffDEF;
            case "spd_eff":   return EffSPD;
            case "dex_eff":   return EffDEX;
            case "vit_eff":   return EffVIT;
            case "wis_eff":   return EffWIS;
            case "luck_eff":  return EffLUCK;

            default:
                Debug.LogWarning($"Unknown stat '{stat}'");
                return 0;
        }
    }

    public void Set(string stat, int value)
    {
        switch (stat.ToLowerInvariant())
        {
            case "level":
                level = Mathf.Max(1, value);
                onStatChanged?.Invoke("level", level);
                break;

            case "xp":
                xp = Mathf.Max(0, value);
                onStatChanged?.Invoke("xp", xp);
                break;

            case "xptonext":
                xpToNext = Mathf.Max(1, value);
                onStatChanged?.Invoke("xpToNext", xpToNext);
                break;

            case "hp":
                hp = Mathf.Clamp(value, 0, EffMaxHP);
                onStatChanged?.Invoke("hp", hp);
                break;

            case "maxhp":
                maxHP = Mathf.Max(1, value);
                hp = Mathf.Min(hp, EffMaxHP);
                onStatChanged?.Invoke("maxHP", maxHP);
                onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
                onStatChanged?.Invoke("hp", hp);
                break;

            case "mp":
                mp = Mathf.Clamp(value, 0, EffMaxMP);
                onStatChanged?.Invoke("mp", mp);
                break;

            case "maxmp":
                maxMP = Mathf.Max(1, value);
                mp = Mathf.Min(mp, EffMaxMP);
                onStatChanged?.Invoke("maxMP", maxMP);
                onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
                onStatChanged?.Invoke("mp", mp);
                break;

            case "att":
                att = Mathf.Max(0, value);
                onStatChanged?.Invoke("att", att);
                break;

            case "def":
                def = Mathf.Max(0, value);
                onStatChanged?.Invoke("def", def);
                break;

            case "spd":
                spd = Mathf.Max(0, value);
                onStatChanged?.Invoke("spd", spd);
                break;

            case "dex":
                dex = Mathf.Max(0, value);
                onStatChanged?.Invoke("dex", dex);
                break;

            case "vit":
                vit = Mathf.Max(0, value);
                onStatChanged?.Invoke("vit", vit);
                break;

            case "wis":
                wis = Mathf.Max(0, value);
                onStatChanged?.Invoke("wis", wis);
                break;

            case "luck":
                luck = value;
                onStatChanged?.Invoke("luck", luck);
                break;

            default:
                Debug.LogWarning($"Unknown stat '{stat}'");
                break;
        }
    }

    // --- Bonus layer (equipment/auras) ---
    public void SetBonus(string stat, int value)
    {
        int oldEffMaxHP = EffMaxHP;
        int oldEffMaxMP = EffMaxMP;

        switch (stat.ToLowerInvariant())
        {
            case "att":   b_att   = value; onStatChanged?.Invoke("att_bonus", b_att); break;
            case "def":   b_def   = value; onStatChanged?.Invoke("def_bonus", b_def); break;
            case "spd":   b_spd   = value; onStatChanged?.Invoke("spd_bonus", b_spd); break;
            case "dex":   b_dex   = value; onStatChanged?.Invoke("dex_bonus", b_dex); break;
            case "vit":   b_vit   = value; onStatChanged?.Invoke("vit_bonus", b_vit); break;
            case "wis":   b_wis   = value; onStatChanged?.Invoke("wis_bonus", b_wis); break;
            case "luck":  b_luck  = value; onStatChanged?.Invoke("luck_bonus", b_luck); break;
            case "maxhp": b_maxHP = value; onStatChanged?.Invoke("maxHP_bonus", b_maxHP); break;
            case "maxmp": b_maxMP = value; onStatChanged?.Invoke("maxMP_bonus", b_maxMP); break;
            default:
                Debug.LogWarning($"Unknown bonus '{stat}'");
                break;
        }

        // If effective maxes changed, clamp and notify bars
        if (EffMaxHP != oldEffMaxHP)
        {
            hp = Mathf.Clamp(hp, 0, EffMaxHP);
            onStatChanged?.Invoke("hp", hp);
            onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        }
        if (EffMaxMP != oldEffMaxMP)
        {
            mp = Mathf.Clamp(mp, 0, EffMaxMP);
            onStatChanged?.Invoke("mp", mp);
            onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
        }
    }

    public void ResetAllBonuses()
    {
        b_att = b_def = b_spd = b_dex = b_vit = b_wis = b_luck = b_maxHP = b_maxMP = 0;
        onStatChanged?.Invoke("maxHP_eff", EffMaxHP);
        onStatChanged?.Invoke("maxMP_eff", EffMaxMP);
    }

    // --- Passive regen ---
    public void TickRegen(float dt)
    {
        // EffVIT/EffWIS so equipment affects regen
        _hpAcc += EffVIT * dt / 5f;
        _mpAcc += EffWIS * dt / 5f;

        if (_hpAcc >= 1f)
        {
            int add = Mathf.FloorToInt(_hpAcc);
            _hpAcc -= add;
            Heal(add);
        }

        if (_mpAcc >= 1f)
        {
            int add = Mathf.FloorToInt(_mpAcc);
            _mpAcc -= add;
            RestoreMP(add);
        }
    }

    float _hpAcc, _mpAcc;

    void Update()
    {
        // Comment out if you'll drive this elsewhere
        TickRegen(Time.deltaTime);
    }
}
