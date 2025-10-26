using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Auto-wiring")]
    [Tooltip("Roots to search for ItemSlotUI in addition to this object.")]
    public Transform[] extraRoots;

    [Tooltip("Also search the entire scene for ItemSlotUI (inactive included).")]
    public bool searchWholeScene = true;

    [Header("All slots discovered (read-only at runtime)")]
    public ItemSlotUI[] slots;

    [Header("Debug")]
    public bool logDebug = false;

    void Awake()
    {
        AutoWireSlots();
    }

    void AutoWireSlots()
    {
        var list = new List<ItemSlotUI>(64);

        // 1) Children of this object
        var local = GetComponentsInChildren<ItemSlotUI>(includeInactive: true);
        if (local != null && local.Length > 0) list.AddRange(local);

        // 2) Extra roots
        if (extraRoots != null)
        {
            foreach (var root in extraRoots)
            {
                if (!root) continue;
                var arr = root.GetComponentsInChildren<ItemSlotUI>(includeInactive: true);
                if (arr != null && arr.Length > 0) list.AddRange(arr);
            }
        }

        // 3) Whole scene
        if (searchWholeScene)
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<ItemSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = Resources.FindObjectsOfTypeAll<ItemSlotUI>();
#endif
            if (all != null && all.Length > 0) list.AddRange(all);
        }

        // Dedup
        var seen = new HashSet<ItemSlotUI>();
        var unique = new List<ItemSlotUI>(list.Count);
        foreach (var s in list)
        {
            if (s && !seen.Contains(s))
            {
                seen.Add(s);
                unique.Add(s);
            }
        }

        slots = unique.ToArray();

        if (logDebug)
        {
            int equip = 0, inv = 0;
            foreach (var s in slots) { if (!s) continue; if (s.category == SlotCategory.Equip) equip++; else inv++; }
        }
    }

    // ===== Helpers =====
    ItemSlotUI FindFirstEmptyEquipSlotCompatible(Item item)
    {
        foreach (var s in slots)
            if (s && s.category == SlotCategory.Equip && s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    ItemSlotUI FindFirstCompatibleEquipSlotOccupied(Item item)
    {
        foreach (var s in slots)
            if (s && s.category == SlotCategory.Equip && !s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    ItemSlotUI FindFirstEmptyInventorySlotCompatible(Item item)
    {
        foreach (var s in slots)
            if (s && s.category == SlotCategory.Inventory && s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    // ===== Public API =====

    /// Auto-equip an item from any slot into an Equip slot.
    public bool TryAutoEquip(ItemSlotUI fromSlot)
    {
        if (fromSlot == null || fromSlot.IsEmpty) return false;
        var it = fromSlot.item;
        if (it == null || !it.isEquippable) return false;

        if (slots == null || slots.Length == 0) AutoWireSlots();

        var emptyEquip = FindFirstEmptyEquipSlotCompatible(it);
        if (emptyEquip != null)
        {
            emptyEquip.SetItem(it);
            fromSlot.Clear();
            return true;
        }

        var occupiedEquip = FindFirstCompatibleEquipSlotOccupied(it);
        if (occupiedEquip != null)
        {
            var tmp = occupiedEquip.item;
            occupiedEquip.SetItem(it);
            fromSlot.SetItem(tmp);
            return true;
        }

        return false;
    }

    /// Unequip an item from an Equip slot into the first compatible empty Inventory slot.
    public bool TryUnequip(ItemSlotUI fromEquipSlot)
    {
        if (fromEquipSlot == null || fromEquipSlot.IsEmpty) return false;
        if (fromEquipSlot.category != SlotCategory.Equip) return false;

        var it = fromEquipSlot.item;
        if (it == null) return false;

        if (slots == null || slots.Length == 0) AutoWireSlots();

        var targetInv = FindFirstEmptyInventorySlotCompatible(it);
        if (targetInv != null)
        {
            targetInv.SetItem(it);
            fromEquipSlot.Clear();
            return true;
        }

        return false;
    }
}
