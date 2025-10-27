using UnityEngine;

[System.Serializable]
public struct ItemStack
{
    public Item item;
    public int count;

    public ItemStack(Item item, int count = 1)
    {
        this.item = item;
        this.count = Mathf.Max(1, count);
    }

    public bool IsValid => item != null && count > 0;
}
