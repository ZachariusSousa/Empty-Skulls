using UnityEngine;
using UnityEngine.UI;

public enum SlotCategory { Inventory, Equip }

[DisallowMultipleComponent]
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI (auto-wired)")]
    public Image background;     // Image on this GameObject
    public Image icon;           // Image on child "Icon"

    [Header("Slot Rules")]
    public SlotCategory category = SlotCategory.Inventory;

    [Tooltip("For equipment slots only: which equip slot this is (e.g., Weapon, Armor, Chip, Ability).")]
    public EquipSlotKind equipSlot = EquipSlotKind.None;

    [Tooltip("For inventory slots: leave empty to accept any kind; otherwise only these kinds are allowed.")]
    public ItemKind[] allowedKinds; // used only when category == Inventory

    [HideInInspector] public Item item;

    // cache default state (so play-mode changes don't persist)
    Sprite _defaultIconSprite;
    bool _defaultIconEnabled;

    public bool IsEmpty => item == null;

    void Reset() => AutoWire();
    void OnValidate() => AutoWire();

    void Awake()
    {
        if (!background || !icon) AutoWire();

        if (!icon)
        {
            Debug.LogError($"[ItemSlotUI] Missing 'Icon' child with Image under '{name}'. " +
                           "I tried to create one but failed. Ensure a direct child named 'Icon' has an Image.");
            return;
        }

        _defaultIconSprite = icon.sprite;
        _defaultIconEnabled = icon.enabled;
    }

    void AutoWire()
    {
        // Background must be on this same object
        if (!background) background = GetComponent<Image>();

        // Try to find a direct child named "Icon"
        var t = transform.Find("Icon");
        if (t)
        {
            if (!icon) icon = t.GetComponent<Image>();
            if (!icon)
            {
                icon = t.gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
            }
        }
        else
        {
            // If child "Icon" doesn't exist, create it with an Image
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
        }
    }

    /// <summary>
    /// Returns true if this slot can accept the given item based on slot rules.
    /// </summary>
    public bool IsCompatible(Item it)
    {
        if (it == null) return true;

        if (category == SlotCategory.Equip)
        {
            // Must be equippable and match the exact equip slot
            return it.isEquippable && it.equipSlot == equipSlot;
        }

        // Inventory slot
        if (allowedKinds != null && allowedKinds.Length > 0)
        {
            for (int i = 0; i < allowedKinds.Length; i++)
                if (allowedKinds[i] == it.kind) return true;
            return false;
        }

        // No filter → accept anything
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
            icon.preserveAspect = true;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false; // <- hides the white box when empty
        }
    }

    public void Clear() => SetItem(null);
}
