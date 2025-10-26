using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUseHandler : MonoBehaviour, IPointerClickHandler
{
    public PlayerStats player;           // assign your PlayerStats (auto-find if null)
    public ItemSlotUI[] inventorySlots;  // assign all inventory slots here

    [Header("Input")]
    public bool useOnRightClick = true;
    public bool useOnDoubleLeft = true;
    public float doubleClickMaxDelay = 0.25f;

    float _lastClickTime;

    void Awake()
    {
        if (!player)
        {
            var t = GameObject.FindGameObjectWithTag("Player");
            if (t) player = t.GetComponent<PlayerStats>();
        }
    }

    // This is attached to the inventory grid, so clicks bubble here.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!player) return;

        bool right = eventData.button == PointerEventData.InputButton.Right;
        bool left  = eventData.button == PointerEventData.InputButton.Left;

        bool doUse = false;

        if (useOnRightClick && right) doUse = true;

        if (useOnDoubleLeft && left)
        {
            float now = Time.unscaledTime;
            if (now - _lastClickTime <= doubleClickMaxDelay) doUse = true;
            _lastClickTime = now;
        }

        if (!doUse) return;

        // Figure out which slot we clicked
        var go = eventData.pointerPressRaycast.gameObject;
        if (!go) return;
        var slot = go.GetComponentInParent<ItemSlotUI>();
        if (!slot || slot.IsEmpty) return;

        TryUse(slot);
    }

    void TryUse(ItemSlotUI slot)
    {
        var item = slot.item;
        if (!item) return;

        // Only allow use for non-equippable or explicitly consumable items
        if (item.isEquippable && item.kind != ItemKind.Consumable)
            return;

        bool anyApplied = false;

        if (item.onUseEffects != null)
        {
            foreach (var eff in item.onUseEffects)
            {
                if (!eff) continue;
                bool applied = eff.Apply(player);
                anyApplied = anyApplied || applied;
            }
        }

        if (anyApplied)
        {
            // No stacking yet → clear the slot after use
            slot.SetItem(null);
        }
    }
}
