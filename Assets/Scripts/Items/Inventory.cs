using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemSO, int> items = new Dictionary<ItemSO, int>();
    private float weight = 0f;
    [SerializeField] private float maxWeight = 100f;

    public void RemoveItemFromInventory(ItemSO item, int amount)
    {
        if (items.ContainsKey(item))
        {
            // Check if the item has enough quantity to remove
            if (items[item] >= amount)
            {
                // Subtract the weight of the item multiplied by the amount from the total weight
                weight -= item.weight * amount;

                // Reduce the item quantity
                items[item] -= amount;

                // If the quantity goes to 0, remove the item from the dictionary
                if (items[item] <= 0)
                {
                    items.Remove(item);
                }

                // Notify UI elements about the item removal
                UiManager.Instance.NotifyItem(item, -amount); // Negative amount to signify removal
                UiManager.Instance.inventoryPage.UpdateInventory(items);
                UiManager.Instance.playerHud.UpdateWeightBar(weight, maxWeight);
            }
            else
            {
                Debug.LogWarning("Not enough items to remove");
            }
        }
        else
        {
            Debug.LogWarning("Item not found in inventory");
        }
    }

    public void AddItemToInventory(ItemSO item, int amount)
    {
        // Add the weight of the item multiplied by the amount to the total weight
        weight += item.weight * amount;

        if (items.ContainsKey(item))
        {
            items[item] += amount;
        }
        else
        {
            items.Add(item, amount);
        }

        // Notify UI elements about the item addition
        UiManager.Instance.NotifyItem(item, amount);
        UiManager.Instance.inventoryPage.UpdateInventory(items);
        UiManager.Instance.playerHud.UpdateWeightBar(weight, maxWeight);
    }

}