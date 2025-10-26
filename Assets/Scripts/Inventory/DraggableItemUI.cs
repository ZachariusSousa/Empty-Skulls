using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Canvas _rootCanvas;
    RectTransform _rt;
    CanvasGroup _cg;
    Transform _originalParent;
    ItemSlotUI _fromSlot;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _fromSlot = _originalParent.GetComponent<ItemSlotUI>();
        if (_fromSlot == null || _fromSlot.IsEmpty) return;

        // Pop to top so we can drag above everything
        transform.SetParent(_rootCanvas.transform, true);
        _cg.blocksRaycasts = false; // allow raycast to pass through while dragging
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_fromSlot == null || _fromSlot.IsEmpty) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position, _rootCanvas.worldCamera, out var pos);
        _rt.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;

        // Did we drop on a slot?
        var target = eventData.pointerCurrentRaycast.gameObject;
        var targetSlot = target ? target.GetComponentInParent<ItemSlotUI>() : null;

        if (targetSlot == null)
        {
            // Not dropped on a slot → snap back
            transform.SetParent(_originalParent, true);
            _rt.anchoredPosition = Vector2.zero;
            return;
        }

        // Validate equip rules if dropping onto equipment
        if (targetSlot is EquipmentSlotUI equipSlot)
        {
            if (_fromSlot.item == null || !_fromSlot.item.isEquippable ||
                _fromSlot.item.equipSlot != equipSlot.slotKind)
            {
                // invalid equip, snap back
                transform.SetParent(_originalParent, true);
                _rt.anchoredPosition = Vector2.zero;
                return;
            }
        }

        // Swap items
        var tmp = targetSlot.item;
        targetSlot.SetItem(_fromSlot.item);
        _fromSlot.SetItem(tmp);

        // Re-parent icon to the slot we just moved into
        transform.SetParent(targetSlot.icon.transform, false);
        _rt.anchoredPosition = Vector2.zero;

        // Put any old icon (if swap) back onto old slot
        if (_fromSlot.item != null)
        {
            // The icon object remains with the dragged item.
            // If you want separate icons per slot, you can instead refresh from data.
        }
    }
}
