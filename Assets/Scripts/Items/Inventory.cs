using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemSO, int> items = new Dictionary<ItemSO, int>();

    public void AddItemToInventory(ItemSO item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] += amount;
        }
        else
        {
            items.Add(item, amount);
        }

        UiManager.Instance.NotifyItem(item, amount);
        UiManager.Instance.inventoryPage.UpdateInventory(items);
    }
}