using UnityEngine;
using UnityEngine.UI;
using System;

public enum SlotRole { Inventory, Equip, LootBag }

[DisallowMultipleComponent]
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI (auto)")]
    public Image background;   // Image on this object
    public Image icon;         // Image on child "Icon"

    [Header("Rules")]
    public SlotRole role = SlotRole.Inventory;
    public EquipSlotKind equipSlot = EquipSlotKind.None; // only used when role == Equip

    [Header("Links (auto)")]
    public InventoryUI inventory; // auto-found in parent

    [HideInInspector] public Item item;
    public Action<ItemSlotUI, Item, Item> onItemChanged;
    public bool IsEmpty => item == null;

    void Awake()
    {
        if (!background) background = GetComponent<Image>();

        if (!icon)
        {
            var t = transform.Find("Icon");
            if (t) icon = t.GetComponent<Image>();
        }

        if (!inventory) inventory = GetComponentInParent<InventoryUI>();

        EnsureIconComponents();
        InitEmptyLook();
    }

#if UNITY_EDITOR
    void Reset() { AutoWire_EditorOnly(); EnsureIconComponents(); InitEmptyLook(); }
    void OnValidate() { AutoWire_EditorOnly(); EnsureIconComponents(); }

    void AutoWire_EditorOnly()
    {
        if (Application.isPlaying) return;

        if (!background) background = GetComponent<Image>();

        var t = transform.Find("Icon");
        if (!t)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            icon = go.GetComponent<Image>();
        }
        else
        {
            icon = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
        }
    }
#endif

    void EnsureIconComponents()
    {
        if (!icon) return;
        if (!icon.TryGetComponent<CanvasGroup>(out _))
            icon.gameObject.AddComponent<CanvasGroup>();
        icon.raycastTarget = true;
        icon.preserveAspect = true;
    }

    void InitEmptyLook()
    {
        if (!icon) return;
        if (item == null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public bool IsCompatible(Item it)
    {
        if (it == null) return true;

        // Only Equip slots enforce a rule
        if (role == SlotRole.Equip)
            return it.isEquippable && it.equipSlot == equipSlot;

        // Inventory & LootBag accept anything
        return true;
    }

    public void SetItem(Item newItem)
    {
        var old = item;
        item = newItem;

        if (!icon) return;

        if (item && item.icon)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
            icon.raycastTarget = true;
            icon.preserveAspect = true;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        onItemChanged?.Invoke(this, old, newItem);
    }

    public void Clear() => SetItem(null);
}
