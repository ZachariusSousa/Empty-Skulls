using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class LootBag : MonoBehaviour
{
    [Header("Table & Capacity")]
    public LootTable lootTable;
    [Min(1)] public int capacity = 8;

    [Header("Lifecycle")]
    public float despawnSeconds = 60f;
    public float openDistance = 3.5f; // auto-close UI if player walks away

    [Header("Runtime (read-only)")]
    public ItemStack[] slots;     // unique per bag, do not share!
    public bool rolled;

    float _life;

    public System.Action<LootBag> onChanged;

    void Awake()
    {
        if (slots == null || slots.Length != capacity)
            slots = new ItemStack[capacity];
    }

    void OnEnable()
    {
        // Optional deterministic seed: scene time + position
        if (!rolled && lootTable != null)
        {
            lootTable.Seed((int)(Time.time * 1000f) ^ transform.position.GetHashCode());
            Populate();
        }
    }

    void Update()
    {
        if (despawnSeconds > 0f)
        {
            _life += Time.deltaTime;
            if (_life >= despawnSeconds) Destroy(gameObject);
        }
    }

    public void Populate()
{
    if (rolled || lootTable == null) return;
    rolled = true;

    // Optional deterministic seed (keeps your old behavior)
    lootTable.Seed((int)(Time.time * 1000f) ^ transform.position.GetHashCode());

    var drops = lootTable.RollBag();
    if (drops == null || drops.Count == 0)
    {
        // No bag to show — destroy this bag gameObject (or just early-return if you spawn bag only on success)
        Destroy(gameObject);
        return;
    }

    // Fill slots left→right
    int i = 0;
    for (; i < drops.Count && i < capacity; i++)
        slots[i] = drops[i];

    // Clear the rest
    for (; i < capacity; i++)
        slots[i] = default;

    onChanged?.Invoke(this);
}


    public bool TryTake(int index, out ItemStack stack)
    {
        stack = default;
        if (index < 0 || index >= slots.Length) return false;
        if (!slots[index].IsValid) return false;

        stack = slots[index];
        slots[index] = default;
        onChanged?.Invoke(this);
        return true;
    }

    public bool TryPlace(int index, ItemStack stack)
    {
        if (!stack.IsValid) return false;
        if (index < 0 || index >= slots.Length) return false;
        if (slots[index].IsValid) return false;

        slots[index] = stack;
        onChanged?.Invoke(this);
        return true;
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsValid) return false;
        return true;
    }
}
