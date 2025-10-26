using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    Canvas _rootCanvas;
    CanvasGroup _cg;
    ItemSlotUI _fromSlot;
    InventoryUI _inventory;    // cached from parent

    // Drag ghost
    GameObject _ghostGO;
    RectTransform _ghostRT;
    Image _ghostImg;

    void Awake()
    {
        var c = GetComponentInParent<Canvas>();
        _rootCanvas = c ? c.rootCanvas : null;   // use the root canvas
        _cg = GetComponent<CanvasGroup>();
        _fromSlot = GetComponentInParent<ItemSlotUI>();
        _inventory = _fromSlot ? _fromSlot.inventory : GetComponentInParent<InventoryUI>();

        if (_rootCanvas == null)
            Debug.LogError("[DraggableItemUI] No Canvas found in parents.");
    }

    // ---------- Double-click to auto-equip ----------
    public void OnPointerClick(PointerEventData e)
    {
        if (e.clickCount >= 2)
        {
            if (_fromSlot && _inventory)
                _inventory.TryAutoEquip(_fromSlot);
        }
    }

    // ---------- Drag & drop ----------
    public void OnBeginDrag(PointerEventData e)
    {
        _fromSlot = transform.GetComponentInParent<ItemSlotUI>();
        if (_fromSlot == null || _fromSlot.IsEmpty || _fromSlot.item?.icon == null) return;

        // Create a ghost image to drag
        _ghostGO = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        _ghostRT = _ghostGO.GetComponent<RectTransform>();
        _ghostImg = _ghostGO.GetComponent<Image>();

        _ghostRT.SetParent(_rootCanvas.transform, false);
        _ghostImg.sprite = _fromSlot.item.icon;
        _ghostImg.raycastTarget = false; // let raycasts pass through
        _ghostImg.preserveAspect = true;
        _ghostImg.color = new Color(1f, 1f, 1f, 0.9f);

        UpdateGhostPos(e);

        // Hide the source icon while dragging (visual "pick up")
        if (_fromSlot.icon) _fromSlot.icon.enabled = false;

        // Let raycasts hit slots behind while dragging
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ghostRT == null) return;
        UpdateGhostPos(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = true;

        ItemSlotUI target = null;
        if (e.pointerCurrentRaycast.gameObject)
            target = e.pointerCurrentRaycast.gameObject.GetComponentInParent<ItemSlotUI>();

        bool swapped = false;

        if (_fromSlot != null && !_fromSlot.IsEmpty && target != null)
        {
            if (target.IsCompatible(_fromSlot.item))
            {
                var tmp = target.item;
                target.SetItem(_fromSlot.item);
                _fromSlot.SetItem(tmp);
                swapped = true;
            }
        }

        // If not swapped (dropped nowhere/invalid), restore the source icon
        if (!swapped && _fromSlot != null)
            _fromSlot.SetItem(_fromSlot.item);

        if (_ghostGO) Destroy(_ghostGO);
        _ghostGO = null; _ghostRT = null; _ghostImg = null;
    }

    void UpdateGhostPos(PointerEventData e)
    {
        RectTransform canvasRT = _rootCanvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, e.position, _rootCanvas.worldCamera, out var localPos))
        {
            _ghostRT.anchoredPosition = localPos;
        }
    }
}
