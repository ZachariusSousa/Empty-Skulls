using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItemUI : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Double-Click Settings")]
    public float doubleClickInterval = 0.3f;
    public float doubleClickMaxTravel = 10f;

    Canvas _rootCanvas;
    CanvasGroup _cg;
    ItemSlotUI _fromSlot;
    InventoryUI _inventory;

    // Drag ghost
    GameObject _ghostGO;
    RectTransform _ghostRT;
    Image _ghostImg;

    // Double-click state
    float _lastClickTime = -999f;
    Vector2 _lastClickPos;
    bool _isDragging;

    void Awake()
    {
        var c = GetComponentInParent<Canvas>();
        _rootCanvas = c ? c.rootCanvas : null;
        _cg = GetComponent<CanvasGroup>();
        _fromSlot = GetComponentInParent<ItemSlotUI>();
        _inventory = _fromSlot ? _fromSlot.inventory : GetComponentInParent<InventoryUI>();

        if (_rootCanvas == null)
            Debug.LogError("[DraggableItemUI] No Canvas found in parents.");
    }

    // ===== Double-click =====
    public void OnPointerDown(PointerEventData e)
    {
        if (_fromSlot == null) _fromSlot = GetComponentInParent<ItemSlotUI>();
        if (_inventory == null && _fromSlot) _inventory = _fromSlot.inventory;

        if (_fromSlot == null || _fromSlot.IsEmpty || _fromSlot.item == null)
        {
            _lastClickTime = -999f;
            return;
        }

        var now = Time.unscaledTime;
        var pos = e.position;

        bool isDouble =
            (now - _lastClickTime <= doubleClickInterval) &&
            ((pos - _lastClickPos).sqrMagnitude <= (doubleClickMaxTravel * doubleClickMaxTravel));

        if (isDouble && !_isDragging)
        {
            HandleDoubleClick(_fromSlot);
            _lastClickTime = -999f; // reset
        }
        else
        {
            _lastClickTime = now;
            _lastClickPos = pos;
        }
    }

    void HandleDoubleClick(ItemSlotUI slot)
{
    if (!slot || slot.IsEmpty) return;
    var item = slot.item;

    switch (slot.role)
    {
        case SlotRole.Equip:
            if (slot.inventory != null)
                slot.inventory.TryUnequip(slot);
            break;

        case SlotRole.LootBag:
        {
            var target = slot.inventory
                ? slot.inventory.FindFirstEmptyInventorySlotCompatible(item)
                : null;
            if (target != null)
            {
                target.SetItem(item);
                slot.Clear();
            }
            break;
        }

        case SlotRole.Inventory:
        default:
        {
            if (item.kind == ItemKind.Consumable)
                    {
                var useHandler = FindObjectOfType<ItemUseHandler>();
                if (useHandler != null) useHandler.TryUse(slot);
                Debug.Log($"[DraggableItemUI] Double-click used consumable '{item.name}'.");
            }
            else if (item.isEquippable && slot.inventory != null)
            {
                slot.inventory.TryAutoEquip(slot);
            }
            break;
        }
    }
}


    ItemSlotUI FindFirstEmptyInventorySlot(Item item)
    {
        // Look across all slots in the scene; pick first empty Inventory that is compatible.
        var all = Object.FindObjectsByType<ItemSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        return all.FirstOrDefault(s =>
            s && s.role == SlotRole.Inventory &&
            s.IsEmpty && s.IsCompatible(item));
    }

    // ===== Drag & drop (unchanged) =====
    public void OnBeginDrag(PointerEventData e)
    {
        _fromSlot = transform.GetComponentInParent<ItemSlotUI>();
        if (_fromSlot == null || _fromSlot.IsEmpty || _fromSlot.item?.icon == null) return;

        _isDragging = true;

        _ghostGO = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        _ghostRT = _ghostGO.GetComponent<RectTransform>();
        _ghostImg = _ghostGO.GetComponent<Image>();

        _ghostRT.SetParent(_rootCanvas.transform, false);
        _ghostImg.sprite = _fromSlot.item.icon;
        _ghostImg.raycastTarget = false;
        _ghostImg.preserveAspect = true;
        _ghostImg.color = new Color(1f, 1f, 1f, 0.9f);

        UpdateGhostPos(e);

        if (_fromSlot.icon) _fromSlot.icon.enabled = false;
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

        if (!swapped && _fromSlot != null)
            _fromSlot.SetItem(_fromSlot.item);

        if (_ghostGO) Destroy(_ghostGO);
        _ghostGO = null; _ghostRT = null; _ghostImg = null;

        _isDragging = false;
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
