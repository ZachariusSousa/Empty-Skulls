using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUseHandler : MonoBehaviour, IPointerClickHandler
{
    public PlayerStats player;  // assign or auto-find

    [Header("Input")]
    public bool useOnRightClick = false;   // keep false to require double-click
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

    public void OnPointerClick(PointerEventData e)
    {
        if (!player) return;

        bool doUse = false;

        if (useOnRightClick && e.button == PointerEventData.InputButton.Right)
            doUse = true;

        if (useOnDoubleLeft && e.button == PointerEventData.InputButton.Left)
        {
            float now = Time.unscaledTime;
            if (now - _lastClickTime <= doubleClickMaxDelay) doUse = true;
            _lastClickTime = now;
        }

        if (!doUse) return;

        var go = e.pointerPressRaycast.gameObject;
        if (!go) return;

        var slot = go.GetComponentInParent<ItemSlotUI>();
        if (!slot || slot.IsEmpty) return;

        // Only consume from Inventory
        if (slot.role != SlotRole.Inventory) return;

        TryUse(slot);
    }

    public bool TryUse(ItemSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.item == null)
            return false;

        var item = slot.item;
        if (item.kind != ItemKind.Consumable)
            return false;

        bool applied = false;

        // Apply all effects; if any succeed, mark as applied
        foreach (var eff in item.onUseEffects)
        {
            if (eff.Apply(player))
                applied = true;
        }

        return applied;
    }
}
