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

    [Header("Combat/Movement (RotMG-ish)")]
    public int att = 10;   // damage power
    public int def = 0;    // flat damage reduction (cap inside TakeDamage)
    public int spd = 25;   // movespeed rating
    public int dex = 10;   // fire rate/attack speed rating
    public int vit = 10;   // hp regen rating
    public int wis = 10;   // mp regen/ability power
    public int luck = 0;   // crit chance or loot bias (your call)

    [Header("Events")]
    public StatIntEvent onStatChanged;    // invoke on any write
    public DeathEvent onDeath;
    public UnityEvent onLevelUp;

    // --- READ helpers ---
    public int MaxHP => maxHP;
    public int HP => hp;
    public int MaxMP => maxMP;
    public int MP => mp;

    // --- WRITE helpers (clamped & evented) ---
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

        // Simple growth curve (tweak as you like)
        maxHP += 10;
        maxMP += 5;
        att += 1;
        spd += 1;
        dex += 1;
        vit += 1;
        wis += 1;

        // Refill on level
        hp = maxHP;
        mp = maxMP;

        // Next XP requirement (mildly increasing)
        xpToNext = Mathf.RoundToInt(xpToNext * 1.25f) + 10;
        onStatChanged?.Invoke("maxHP", maxHP);
        onStatChanged?.Invoke("maxMP", maxMP);
        onStatChanged?.Invoke("xpToNext", xpToNext);
        onStatChanged?.Invoke("hp", hp);
        onStatChanged?.Invoke("mp", mp);

        onLevelUp?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
        onStatChanged?.Invoke("hp", hp);
    }

    public void UseMP(int amount)
    {
        if (amount <= 0) return;
        mp = Mathf.Clamp(mp - amount, 0, maxMP);
        onStatChanged?.Invoke("mp", mp);
    }

    public void RestoreMP(int amount)
    {
        if (amount <= 0) return;
        mp = Mathf.Clamp(mp + amount, 0, maxMP);
        onStatChanged?.Invoke("mp", mp);
    }

    // Simple flat-DEF mitigation with a minimum hit of 1 and a DEF cap
    public void TakeDamage(int rawDamage, int defCap = 25)
    {
        if (rawDamage <= 0 || hp <= 0) return;
        int effectiveDEF = Mathf.Min(def, defCap);
        int final = Mathf.Max(1, rawDamage - effectiveDEF);

        hp = Mathf.Clamp(hp - final, 0, maxHP);
        onStatChanged?.Invoke("hp", hp);

        if (hp == 0) onDeath?.Invoke();
    }

    // Generic read/write by name (handy for pickups/UI)
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
            default: Debug.LogWarning($"Unknown stat '{stat}'"); return 0;
        }
    }

    public void Set(string stat, int value)
    {
        switch (stat.ToLowerInvariant())
        {
            case "level": level = Mathf.Max(1, value); onStatChanged?.Invoke("level", level); break;
            case "xp": xp = Mathf.Max(0, value); onStatChanged?.Invoke("xp", xp); break;
            case "xptonext": xpToNext = Mathf.Max(1, value); onStatChanged?.Invoke("xpToNext", xpToNext); break;
            case "hp": hp = Mathf.Clamp(value, 0, maxHP); onStatChanged?.Invoke("hp", hp); break;
            case "maxhp": maxHP = Mathf.Max(1, value); hp = Mathf.Min(hp, maxHP); onStatChanged?.Invoke("maxHP", maxHP); onStatChanged?.Invoke("hp", hp); break;
            case "mp": mp = Mathf.Clamp(value, 0, maxMP); onStatChanged?.Invoke("mp", mp); break;
            case "maxmp": maxMP = Mathf.Max(0, value); mp = Mathf.Min(mp, maxMP); onStatChanged?.Invoke("maxMP", maxMP); onStatChanged?.Invoke("mp", mp); break;
            case "att": att = Mathf.Max(0, value); onStatChanged?.Invoke("att", att); break;
            case "def": def = Mathf.Max(0, value); onStatChanged?.Invoke("def", def); break;
            case "spd": spd = Mathf.Max(0, value); onStatChanged?.Invoke("spd", spd); break;
            case "dex": dex = Mathf.Max(0, value); onStatChanged?.Invoke("dex", dex); break;
            case "vit": vit = Mathf.Max(0, value); onStatChanged?.Invoke("vit", vit); break;
            case "wis": wis = Mathf.Max(0, value); onStatChanged?.Invoke("wis", wis); break;
            case "luck": luck = value; onStatChanged?.Invoke("luck", luck); break;
            default: Debug.LogWarning($"Unknown stat '{stat}'"); break;
        }
    }

    // Simple periodic regen (optional): call from Update or a coroutine.
    public void TickRegen(float dt)
    {
        // Super simple: vit HP per 5s, wis MP per 5s
        _hpAcc += vit * dt / 5f;
        _mpAcc += wis * dt / 5f;

        if (_hpAcc >= 1f) { int add = Mathf.FloorToInt(_hpAcc); _hpAcc -= add; Heal(add); }
        if (_mpAcc >= 1f) { int add = Mathf.FloorToInt(_mpAcc); _mpAcc -= add; RestoreMP(add); }
    }
    float _hpAcc, _mpAcc;

    void Update()
    {
        // Comment out if you’ll drive it elsewhere
        TickRegen(Time.deltaTime);
    }
}
