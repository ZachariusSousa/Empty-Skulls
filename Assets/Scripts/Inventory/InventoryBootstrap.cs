using UnityEngine;

public class InventoryBootstrap : MonoBehaviour
{
    public ItemSlotUI[] slots;         // assign all inventory slots in order
    public Item[] startingItems;       // optional, drag items here for testing

    void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var it = (i < startingItems.Length) ? startingItems[i] : null;
            slots[i].SetItem(it);

            // Ensure each slot has its Icon wired in inspector
            // And Icon has DraggableItemUI + CanvasGroup
        }
    }
}
