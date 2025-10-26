using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;              // should be the child Image that shows the item
    [HideInInspector] public Item item;  // null = empty

    public bool IsEmpty => item == null;

    void Awake()
    {
        // Auto-find an Image if not assigned (prefers a child named "Icon")
        if (icon == null)
        {
            var t = transform.Find("Icon");
            if (t) icon = t.GetComponent<Image>();
            if (icon == null) icon = GetComponentInChildren<Image>(includeInactive: true);
            if (icon == null)
                Debug.LogWarning($"[ItemSlotUI] Missing Image (Icon) on {name}. Drag a child Image into 'icon'.");
        }
    }

    public void SetItem(Item newItem)
    {
        item = newItem;

        // If icon still isn't set, just store item and bail (prevents null ref)
        if (icon == null) return;

        if (item != null)
        {
            icon.enabled = true;
            icon.sprite  = item.icon;
        }
        else
        {
            icon.sprite  = null;
            icon.enabled = false;
        }
    }

    public void Clear() => SetItem(null);
}
