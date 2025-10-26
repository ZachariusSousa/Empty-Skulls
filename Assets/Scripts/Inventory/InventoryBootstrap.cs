using UnityEngine;

public class InventoryBootstrap : MonoBehaviour
{
    public ItemSlotUI[] slots;
    public Item[] startingItems;

    void Awake()
    {
        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<ItemSlotUI>(includeInactive: true);
    }

    void Start()
    {
        if (slots == null || startingItems == null) return;

        foreach (var it in startingItems)
        {
            if (!it) continue;

            bool placed = false;

            // First pass: try exact equipment slots if the item is equippable
            if (it.isEquippable)
            {
                for (int i = 0; i < slots.Length && !placed; i++)
                {
                    var s = slots[i];
                    if (s && s.IsEmpty && s.category == SlotCategory.Equip && s.IsCompatible(it))
                    {
                        s.SetItem(it);
                        placed = true;
                    }
                }
            }

            // Second pass: try any compatible inventory slot
            for (int i = 0; i < slots.Length && !placed; i++)
            {
                var s = slots[i];
                if (s && s.IsEmpty && s.category == SlotCategory.Inventory && s.IsCompatible(it))
                {
                    s.SetItem(it);
                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"[InventoryBootstrap] No compatible empty slot for '{it.name}' (isEquippable={it.isEquippable}, equipSlot={it.equipSlot}, kind={it.kind}).");
            }
        }
    }
}
