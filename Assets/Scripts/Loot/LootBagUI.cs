using UnityEngine;
using System.Collections.Generic;

public class LootBagUI : MonoBehaviour
{
    [Header("Slot Discovery")]
    public Transform[] lootSlotRoots;            // parents containing ItemSlotUI for loot panel
    public bool includeInactive = true;

    [Header("Debug")]
    public bool logDebug;

    ItemSlotUI[] _uiSlots;
    LootBag _bag;

    void Awake()
    {
        var list = new List<ItemSlotUI>(32);
        foreach (var r in lootSlotRoots)
        {
            if (!r) continue;
            list.AddRange(r.GetComponentsInChildren<ItemSlotUI>(includeInactive));
        }
        _uiSlots = list.ToArray();
        if (logDebug) Debug.Log($"[LootBagUI] Wired {_uiSlots.Length} loot slots.");
        HideAll();
    }

    void HideAll()
    {
        foreach (var s in _uiSlots)
            if (s) s.Clear(); // Clear visuals; your ItemSlotUI should handle empty state
    }

    public void Bind(LootBag bag)
    {
        Unbind();
        _bag = bag;
        if (_bag != null)
        {
            _bag.onChanged += RefreshFromBag;
            RefreshFromBag(_bag);
            gameObject.SetActive(true);
        }
    }

    public void Unbind()
    {
        if (_bag != null)
        {
            _bag.onChanged -= RefreshFromBag;
            _bag = null;
        }
        HideAll();
        gameObject.SetActive(false);
    }

    void RefreshFromBag(LootBag b)
    {
        if (b == null) return;

        for (int i = 0; i < _uiSlots.Length; i++)
        {
            if (!_uiSlots[i]) continue;

            Item item = (i < b.slots.Length && b.slots[i].IsValid) ? b.slots[i].item : null;
            _uiSlots[i].SetItem(item);
        }
    }

    // Example: player clicked on a loot slot to move to player inventory
    public void OnLootSlotClicked(int index, InventoryUI playerInventory, ItemSlotUI playerTargetSlot = null)
    {
        if (_bag == null) return;
        if (!_bag.TryTake(index, out var stack)) return;

        // (A) If you have a stack-aware player inventory, place stack there.
        // For now, we just place the Item (count ignored) into first compatible slot:
        if (playerTargetSlot != null && playerTargetSlot.IsEmpty && playerTargetSlot.IsCompatible(stack.item))
        {
            playerTargetSlot.SetItem(stack.item);
        }
        else
        {
            // naive: find first empty inventory slot in player's UI that is compatible
            var slots = playerInventory.slots;
            foreach (var s in slots)
            {
                if (s.category == SlotCategory.Inventory && s.IsEmpty && s.IsCompatible(stack.item))
                {
                    s.SetItem(stack.item);
                    break;
                }
            }
        }
        // UI will auto-refresh via _bag.onChanged
    }

    // Example: placing back into the bag from a UI slot (drag/drop handler can call this)
    public bool TryPlaceIntoBag(int bagIndex, Item item)
    {
        if (_bag == null || item == null) return false;
        var st = new ItemStack(item, 1);
        return _bag.TryPlace(bagIndex, st);
    }
}
