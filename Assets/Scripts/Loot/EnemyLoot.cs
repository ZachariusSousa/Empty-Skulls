using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class EnemyLoot : MonoBehaviour
{
    [Header("Table & Prefab")]
    public LootTable lootTable;   // assign LootTable asset here
    public GameObject lootBagPrefab;    // prefab with LootBag component

    [Header("Spawn")]
    public bool seedForDeterminism = true;

    /// <summary>Call this from Health.Die()</summary>
    public void DropLootAt(Vector3 worldPos)
    {
        if (!lootTable || !lootBagPrefab) return;

        if (seedForDeterminism)
            lootTable.Seed((int)(Time.time * 1000f) ^ worldPos.GetHashCode());

        List<ItemStack> drops = lootTable.RollBag(); // null => no bag
        if (drops == null || drops.Count == 0) return;

        var bagGO = Object.Instantiate(lootBagPrefab, worldPos, Quaternion.identity);
        var bag = bagGO.GetComponent<LootBag>();
        if (!bag)
        {
            Debug.LogError("[EnemyLoot] LootBag prefab missing LootBag component.");
            return;
        }

        // Fill LootBag directly (don’t call Populate, since we already rolled)
        int i = 0;
        for (; i < drops.Count && i < bag.capacity; i++)
            bag.slots[i] = drops[i];
        for (; i < bag.capacity; i++)
            bag.slots[i] = default;

        bag.rolled = true;            // so it won't auto-roll on Enable
        bag.onChanged?.Invoke(bag);   // notify any UI listeners
    }
}
