using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("All slots under this UI (auto-wired if empty)")]
    public ItemSlotUI[] slots;

    void Awake()
    {
        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<ItemSlotUI>(includeInactive: true);
    }

    /// Move an equippable item from 'fromSlot' into the first compatible equipment slot.
    /// Prefers an empty compatible equipment slot; otherwise swaps with the first compatible.
    public bool TryAutoEquip(ItemSlotUI fromSlot)
    {
        if (!fromSlot || fromSlot.IsEmpty) return false;

        var it = fromSlot.item;
        if (it == null || !it.isEquippable) return false;

        // Pass 1: empty compatible equipment slot
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s && s.category == SlotCategory.Equip && s.IsEmpty && s.IsCompatible(it))
            {
                s.SetItem(it);
                fromSlot.Clear();
                return true;
            }
        }

        // Pass 2: swap with first compatible equipment slot
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s && s.category == SlotCategory.Equip && s.IsCompatible(it))
            {
                var tmp = s.item;
                s.SetItem(it);
                fromSlot.SetItem(tmp);
                return true;
            }
        }

        return false;
    }
}
