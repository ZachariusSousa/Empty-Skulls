using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot/Loot Table", order = 0)]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public Item item;
        [Range(0f, 1f)] public float dropChance = 0.1f; // independent per-item
        [Min(1)] public int minStack = 1;
        [Min(1)] public int maxStack = 1;
    }

    [Header("Bag Gate")]
    [Tooltip("Single roll per kill. If it fails, no bag at all.")]
    [Range(0f, 1f)] public float bagDropChance = 0.25f;

    [Header("Items in this bag (each rolls independently)")]
    public List<Entry> items = new List<Entry>();

    System.Random _rng;
    public void Seed(int seed) => _rng = new System.Random(seed);
    System.Random RNG => _rng ??= new System.Random();

    /// <summary>
    /// Returns null if NO BAG drops. Returns list (possibly empty if unlucky) when the bag drops.
    /// </summary>
    public List<ItemStack> RollBag()
    {
        if (RNG.NextDouble() > bagDropChance)
            return null; // no bag

        var drops = new List<ItemStack>(items.Count);

        // 1) One independent roll per item
        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            if (!e.item) continue;

            float p = Mathf.Clamp01(e.dropChance);
            if (RNG.NextDouble() <= p)
            {
                int stack = (e.minStack == e.maxStack)
                    ? e.minStack
                    : RNG.Next(e.minStack, e.maxStack + 1);

                // merge
                bool merged = false;
                for (int d = 0; d < drops.Count; d++)
                {
                    if (drops[d].item == e.item)
                    {
                        var s = drops[d];
                        s.count += stack;
                        drops[d] = s;
                        merged = true;
                        break;
                    }
                }
                if (!merged) drops.Add(new ItemStack(e.item, stack));
            }
        }

        // 2) Fallback: if empty, pick exactly one item by weight = dropChance
        if (drops.Count == 0)
        {
            // build weights from valid items with p>0
            int chosen = -1;
            double total = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var e = items[i];
                if (e.item && e.dropChance > 0f) total += e.dropChance;
            }
            if (total <= 0.0) return null; // nothing possible → no bag

            double r = RNG.NextDouble() * total;
            double cum = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var e = items[i];
                if (!e.item || e.dropChance <= 0f) continue;
                cum += e.dropChance;
                if (r <= cum)
                {
                    chosen = i;
                    break;
                }
            }
            if (chosen >= 0)
            {
                var e = items[chosen];
                int stack = (e.minStack == e.maxStack)
                    ? e.minStack
                    : RNG.Next(e.minStack, e.maxStack + 1);
                drops.Add(new ItemStack(e.item, stack));
            }
            else return null; // safety
        }

        return drops; // guaranteed non-empty if any item has p>0
    }

}
