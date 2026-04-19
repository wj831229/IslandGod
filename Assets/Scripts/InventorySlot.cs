using System;

[Serializable]
public class InventorySlot
{
    public string itemName;
    public int count;
    public int maxStack;

    public InventorySlot(string itemName, int maxStack = 20)
    {
        this.itemName = itemName;
        this.count    = 1;
        this.maxStack = maxStack;
    }

    public bool IsFull => count >= maxStack;
}
