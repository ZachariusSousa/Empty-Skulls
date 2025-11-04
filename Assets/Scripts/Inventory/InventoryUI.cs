// InventoryUI.cs
using UnityEngine;
using System.Collections.Generic;

public enum InventoryOwner { Player, LootBag, Other }

public class InventoryUI : MonoBehaviour
{
    [Header("Ownership")]
    public InventoryOwner owner = InventoryOwner.Player;

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

        // 3) Whole scene (optional)
        if (searchWholeScene)
        {
            // no version-specific fallbacks; this works in play mode + editor (includes inactive)
            var all = Resources.FindObjectsOfTypeAll<ItemSlotUI>();
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
            int equip = 0, inv = 0, bag = 0;
            foreach (var s in slots)
            {
                if (!s) continue;
                switch (s.role)
                {
                    case SlotRole.Equip: equip++; break;
                    case SlotRole.Inventory: inv++; break;
                    case SlotRole.LootBag: bag++; break;
                }
            }
        }
    }

    // ===== Helpers =====

    public ItemSlotUI FindFirstEmptyEquipSlotCompatible(Item item)
    {
        foreach (var s in slots)
            if (s && s.role == SlotRole.Equip && s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    public ItemSlotUI FindFirstCompatibleEquipSlotOccupied(Item item)
    {
        foreach (var s in slots)
            if (s && s.role == SlotRole.Equip && !s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    public ItemSlotUI FindFirstEmptyInventorySlotCompatible(Item item)
    {
        foreach (var s in slots)
            if (s && s.role == SlotRole.Inventory && s.IsEmpty && s.IsCompatible(item))
                return s;
        return null;
    }

    // ===== Public API =====

    /// Try to move an equippable item into an Equip slot; if none free, stash into first empty PlayerInventory slot.
    public bool TryAutoEquip(ItemSlotUI fromSlot)
    {
        if (fromSlot == null || fromSlot.IsEmpty) return false;

        var it = fromSlot.item;
        if (it == null || !it.isEquippable) return false;

        if (slots == null || slots.Length == 0) AutoWireSlots();

        // 1) Equip if there is an EMPTY compatible equip slot
        var emptyEquip = FindFirstEmptyEquipSlotCompatible(it);
        if (emptyEquip != null)
        {
            emptyEquip.SetItem(it);
            fromSlot.Clear();
            return true;
        }

        // 2) If a compatible equip slot is OCCUPIED, SWAP
        var occupiedEquip = FindFirstCompatibleEquipSlotOccupied(it);
        if (occupiedEquip != null)
        {
            var equipped = occupiedEquip.item;   // currently equipped item
            occupiedEquip.SetItem(it);           // place new item into equip slot
            fromSlot.SetItem(equipped);          // put the old one back into the source slot
            return true;
        }

        // 3) No compatible equip slot → stash into first empty Player inventory slot (if different from source)
        var invEmpty = FindFirstEmptyInventorySlotCompatible(it);
        if (invEmpty != null && invEmpty != fromSlot)
        {
            invEmpty.SetItem(it);
            fromSlot.Clear();
            return true;
        }

        return false;
    }
    /// Unequip from an Equip slot to first empty Inventory slot.
    public bool TryUnequip(ItemSlotUI fromEquipSlot)
    {
        if (fromEquipSlot == null || fromEquipSlot.IsEmpty) return false;
        if (fromEquipSlot.role != SlotRole.Equip) return false;

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

    // === Static helper to find the Player's inventory in scene ===
    public static InventoryUI FindPlayerInventory()
    {
        var all = FindObjectsOfType<InventoryUI>(true);
        foreach (var inv in all)
            if (inv && inv.owner == InventoryOwner.Player) return inv;
        return null;
    }
}
