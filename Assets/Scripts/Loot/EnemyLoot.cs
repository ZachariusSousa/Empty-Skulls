using UnityEngine;

[DisallowMultipleComponent]
public class EnemyLoot : MonoBehaviour
{
    [Header("Table & Prefab")]
    public LootTable lootTable;
    public GameObject lootBagPrefab;

    bool _dropped;

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
        bag.Populate(); 
    }
}
