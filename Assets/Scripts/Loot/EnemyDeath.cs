using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Centralized death pipeline. Attach to any entity with EntityStats.
/// EntityStats will call HandleDeath(this) when HP hits 0.
/// </summary>
[DisallowMultipleComponent]
public class Death : MonoBehaviour
{
    [Header("Loot (optional)")]
    public LootTable lootTable;
    public GameObject lootBagPrefab;

    [Header("FX (optional)")]
    public GameObject deathVfx;
    public AudioClip  deathSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    [Header("XP Award (optional)")]
    public bool awardXpToPlayer = false;
    public int xpAmount = 0;
    public string playerTag = "Player";

    [Header("Cleanup")]
    public bool disableCollidersOnDeath = true;
    public bool disableRenderersOnDeath = false;
    public float destroyDelay = 0f;       // time before destroy

    [Header("Events")]
    public UnityEvent onBeforeDestroy;     // extra hooks (animation triggers, etc.)

    bool _ran;

    /// <summary>Called by EntityStats when HP reaches 0.</summary>
    public void HandleDeath(EntityStats who)
    {
        if (_ran) return; // ensure only once
        _ran = true;

        // 1) Disable collisions/renderers if requested
        if (disableCollidersOnDeath)
        {
            foreach (var col in who.GetComponentsInChildren<Collider2D>(true))
                col.enabled = false;
            foreach (var col3D in who.GetComponentsInChildren<Collider>(true))
                col3D.enabled = false;
        }
        if (disableRenderersOnDeath)
        {
            foreach (var r in who.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        }

        // 2) Spawn VFX/SFX
        if (deathVfx) Instantiate(deathVfx, who.transform.position, Quaternion.identity);
        if (deathSfx) AudioSource.PlayClipAtPoint(deathSfx, who.transform.position, sfxVolume);

        // 3) Drop loot (replaces EnemyLoot)
        if (lootTable && lootBagPrefab)
        {
            var bagGO = Instantiate(lootBagPrefab, who.transform.position, Quaternion.identity);
            if (bagGO.TryGetComponent<LootBag>(out var bag))
            {
                bag.lootTable = lootTable;
                bag.Populate();
            }
            else
            {
                Debug.LogError("[Death] LootBag prefab missing LootBag component.");
            }
        }

        // 4) Award XP (optional)
        if (awardXpToPlayer && xpAmount > 0)
        {
            var playerGo = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGo && playerGo.TryGetComponent<PlayerStats>(out var ps))
            {
                ps.AddXP(xpAmount);
            }
        }

        // 5) User hooks
        try { onBeforeDestroy?.Invoke(); } catch {}

        // 6) Destroy the whole entity
        Destroy(who.gameObject, Mathf.Max(0f, destroyDelay));
    }

    // Optional: auto-subscribe if someone calls EntityStats.onDeath directly
    void OnEnable()
    {
        var stats = GetComponent<EntityStats>();
        if (stats != null)
            stats.onDeath.AddListener(() => HandleDeath(stats));
    }

    void OnDisable()
    {
        var stats = GetComponent<EntityStats>();
        if (stats != null)
            stats.onDeath.RemoveAllListeners(); // keep it simple
    }
}
