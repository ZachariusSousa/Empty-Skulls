using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LootBagUI : MonoBehaviour
{
    [Header("Auto-wiring (like InventoryUI)")]
    [Tooltip("Roots to search for ItemSlotUI in addition to this object.")]
    public Transform[] extraRoots;

    [Tooltip("Also search the entire scene for ItemSlotUI (inactive included).")]
    public bool searchWholeScene = false; // usually false for a chest panel

    [Tooltip("Include inactive children when wiring.")]
    public bool includeInactive = true;

    [Header("Debug")]
    public bool logDebug;

    ItemSlotUI[] _uiSlots;   // auto-discovered once
    LootBag _bag;

    bool _painting;

    public LootBag CurrentBag => _bag;

    void Awake()
    {
        EnsureWired();
        HideAllSafe();
    }

    void OnEnable()
    {
        // If opened from inactive, ensure wiring now too
        EnsureWired();
    }

    // ---------- Wiring ----------
    void EnsureWired()
    {
        if (_uiSlots != null && _uiSlots.Length > 0) return;

        var list = new List<ItemSlotUI>(64);

        // 1) Children of THIS object
        var local = GetComponentsInChildren<ItemSlotUI>(includeInactive);
        if (local != null && local.Length > 0) list.AddRange(local);

        // 2) Extra roots
        if (extraRoots != null)
        {
            foreach (var root in extraRoots)
            {
                if (!root) continue;
                var arr = root.GetComponentsInChildren<ItemSlotUI>(includeInactive);
                if (arr != null && arr.Length > 0) list.AddRange(arr);
            }
        }

        // 3) Optional whole-scene (usually keep off for a chest panel)
        if (searchWholeScene)
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<ItemSlotUI>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var all = Resources.FindObjectsOfTypeAll<ItemSlotUI>();
#endif
            if (all != null && all.Length > 0) list.AddRange(all);
        }

        // Dedup
        var seen = new HashSet<ItemSlotUI>();
        var unique = new List<ItemSlotUI>(list.Count);
        foreach (var s in list)
            if (s && !seen.Contains(s)) { seen.Add(s); unique.Add(s); }

        _uiSlots = unique.ToArray();

        if (logDebug)
            Debug.Log($"[LootBagUI] Wired {_uiSlots.Length} loot slots under '{name}'.");
    }

     void WireLootDragSync()
    {
        if (_uiSlots == null) return;

        for (int i = 0; i < _uiSlots.Length; i++)
        {
            var ui = _uiSlots[i];
            if (!ui) continue;

            int idx = i; // capture for lambda

            // Replace any previous handler to avoid duplicates
            ui.onItemChanged = (slot, oldItem, newItem) =>
            {
                if (_painting || _bag == null) return;

                // User removed item from this UI slot (dragged out) → take from bag
                if (oldItem != null && newItem == null)
                {
                    _bag.TryTake(idx, out var _);
                }
                // User dropped an item back into this UI slot → place into bag
                else if (oldItem == null && newItem != null)
                {
                    _bag.TryPlace(idx, new ItemStack(newItem, 1));
                }
                // Replacements (old!=null && new!=null): treat as take then place
                else if (oldItem != null && newItem != null)
                {
                    // keep it simple & safe
                    _bag.TryTake(idx, out var _);
                    _bag.TryPlace(idx, new ItemStack(newItem, 1));
                }
            };
        }
    }

    // ---------- Visibility / binding ----------
    public void Bind(LootBag bag)
    {
        Unbind();

        _bag = bag;
        if (_bag != null)
        {
            _bag.onChanged += RefreshFromBag;
            EnsureWired();
            WireLootDragSync();        // NEW: keep bag in sync with drag/drop
            RefreshFromBag(_bag);
            gameObject.SetActive(true);
        }
        else
        {
            HideAllSafe();
            gameObject.SetActive(false);
        }
    }

    public void Unbind()
    {
        if (_bag != null)
        {
            _bag.onChanged -= RefreshFromBag;
            _bag = null;
        }
        HideAllSafe();
        gameObject.SetActive(false);
    }

    // ---------- Painting ----------
    void HideAllSafe()
    {
        if (_uiSlots == null) return;
        foreach (var s in _uiSlots)
            if (s) s.Clear();
    }

    void RefreshFromBag(LootBag b)
    {
        if (b == null) { HideAllSafe(); return; }
        EnsureWired();
        if (_uiSlots == null) return;

        _painting = true;                 // NEW: prevent feedback loops
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            var ui = _uiSlots[i];
            if (!ui) continue;

            Item item = (i < b.slots.Length && b.slots[i].IsValid) ? b.slots[i].item : null;
            ui.SetItem(item);

            // Optional: if you have a Button on the icon, toggle interactivity
            if (ui.icon)
            {
                var btn = ui.icon.GetComponent<Button>();
                if (btn) btn.interactable = (item != null);
            }
        }
        _painting = false;                // NEW
    }

    // ---------- Optional: click handler to move 1 item to player's first compatible inv slot ----------
    public void OnLootSlotClicked(int index, InventoryUI playerInventory, ItemSlotUI playerTargetSlot = null)
    {
        if (_bag == null) return;
        if (!_bag.TryTake(index, out var stack)) return;

        if (playerTargetSlot && playerTargetSlot.IsEmpty && playerTargetSlot.IsCompatible(stack.item))
        {
            playerTargetSlot.SetItem(stack.item);
        }
        else
        {
            // use your existing helper
            var target = playerInventory ? playerInventory.FindFirstEmptyInventorySlotCompatible(stack.item) : null;
            if (target) target.SetItem(stack.item);
            else
            {
                // couldn't place -> put it back
                _bag.TryPlace(index, new ItemStack(stack.item, 1));
                return;
            }
        }
        // UI will refresh via onChanged
    }

    public bool TryPlaceIntoBag(int bagIndex, Item item)
    {
        if (_bag == null || item == null) return false;
        return _bag.TryPlace(bagIndex, new ItemStack(item, 1));
    }
}
