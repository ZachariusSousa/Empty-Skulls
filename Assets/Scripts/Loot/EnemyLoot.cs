using UnityEngine;

[DisallowMultipleComponent]
public class EnemyLoot : MonoBehaviour
{
    [Header("Table & Prefab")]
    public LootTable lootTable;
    public GameObject lootBagPrefab;

    [Header("Spawn")]
    public bool seedForDeterminism = true;

    bool _dropped;
    // If pooling: void OnEnable() => _dropped = false;

    public void DropLootAt(Vector3 worldPos)
    {
        if (_dropped) return;
        _dropped = true;

        if (!lootTable || !lootBagPrefab) return;

        var bagGO = Instantiate(lootBagPrefab, worldPos, Quaternion.identity);
        if (!bagGO.TryGetComponent<LootBag>(out var bag))
        {
            Debug.LogError("[EnemyLoot] LootBag prefab missing LootBag.");
            return;
        }

        bag.lootTable = lootTable;

        int? seed = null;
        if (seedForDeterminism)
            seed = unchecked((int)(Time.time * 1000f) ^ worldPos.GetHashCode());

        bag.Populate(seed); // ← single, authoritative roll
    }
}
