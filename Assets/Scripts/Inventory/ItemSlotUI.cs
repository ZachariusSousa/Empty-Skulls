using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Image icon;
    [HideInInspector] public Item item;  // null = empty

    public bool IsEmpty => item == null;

    public void SetItem(Item newItem)
    {
        item = newItem;
        icon.enabled = item != null;
        icon.sprite  = item ? item.icon : null;
    }

    public void Clear() => SetItem(null);
}
