using UnityEngine;

[DisallowMultipleComponent]
public class LootBag : MonoBehaviour
{
    [Header("Table & Capacity")]
    public LootTable lootTable;
    [Min(1)] public int capacity = 8;

    [Header("Lifecycle")]
    public float despawnSeconds = 60f;
    public float openDistance = 3.5f;

    [Header("Runtime (read-only)")]
    public ItemStack[] slots;
    public bool rolled;

    float _life;
    public System.Action<LootBag> onChanged;

    void Awake()
    {
        if (slots == null || slots.Length != capacity)
            slots = new ItemStack[capacity];
    }

    void Start() { /* no auto-populate here */ }

    void Update()
    {
        if (despawnSeconds > 0f)
        {
            _life += Time.deltaTime;
            if (_life >= despawnSeconds) Destroy(gameObject);
        }
    }

    // Single source of truth for rolling
    public void Populate(int? seed = null)
    {
        if (rolled || lootTable == null) return;
        rolled = true;

        if (seed.HasValue) lootTable.Seed(seed.Value);

        var drops = lootTable.RollBag();
        if (drops == null || drops.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (slots == null || slots.Length != capacity)
            slots = new ItemStack[capacity];

        int i = 0;
        for (; i < drops.Count && i < capacity; i++) slots[i] = drops[i];
        for (; i < capacity; i++) slots[i] = default;

        onChanged?.Invoke(this);
    }


    public bool IsEmpty()
    {
        if (slots == null) return true;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsValid) return false;
        return true;
    }

    public bool TryTake(int index, out ItemStack stack)
    {
        stack = default;

        if (slots == null) return false;
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

        if (slots == null) return false;
        if (index < 0 || index >= slots.Length) return false;
        if (slots[index].IsValid) return false; // only place into empty

        slots[index] = stack;

        onChanged?.Invoke(this);
        return true;
    }

}
