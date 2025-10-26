using UnityEngine;
using UnityEngine.UI;

public enum SlotCategory { Inventory, Equip }

[DisallowMultipleComponent]
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI (auto)")]
    public Image background;   // Image on this object
    public Image icon;         // Image on child "Icon"

    [Header("Slot Rules")]
    public SlotCategory category = SlotCategory.Inventory;
    public EquipSlotKind equipSlot = EquipSlotKind.None;  // used when category == Equip
    public ItemKind[] allowedKinds;                       // optional filter for Inventory

    [Header("Links (auto)")]
    public InventoryUI inventory;                         // auto-found in parent

    [HideInInspector] public Item item;

    public bool IsEmpty => item == null;

    void Awake()
    {
        if (!background) background = GetComponent<Image>();

        if (!icon)
        {
            var t = transform.Find("Icon"); // direct child only
            if (t) icon = t.GetComponent<Image>();
        }

        if (!inventory) inventory = GetComponentInParent<InventoryUI>();

        EnsureIconComponents();
        InitEmptyLook();
    }

#if UNITY_EDITOR
    void Reset() { AutoWire_EditorOnly(); EnsureIconComponents(); InitEmptyLook(); }
    void OnValidate() { AutoWire_EditorOnly(); EnsureIconComponents(); }

    // Editor-only auto-wiring so we never spawn/replace at runtime
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

        icon.raycastTarget = true;   // must be true so clicks & drags hit the Icon
        icon.preserveAspect = true;
    }

    void InitEmptyLook()
    {
        if (!icon) return;
        if (item == null)
        {
            icon.sprite = null;
            icon.enabled = false;    // hides Unity's default white sprite
        }
    }

    public bool IsCompatible(Item it)
    {
        if (it == null) return true;

        if (category == SlotCategory.Equip)
            return it.isEquippable && it.equipSlot == equipSlot;

        if (allowedKinds != null && allowedKinds.Length > 0)
        {
            for (int i = 0; i < allowedKinds.Length; i++)
                if (allowedKinds[i] == it.kind) return true;
            return false;
        }
        return true;
    }

    public void SetItem(Item newItem)
    {
        item = newItem;
        if (!icon) return;

        if (item != null && item.icon != null)
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
    }

    public void Clear() => SetItem(null);
}
