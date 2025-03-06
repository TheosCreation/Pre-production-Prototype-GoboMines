using System.Collections.Generic;
using Unity.Netcode;
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

    public void DropAllItems()
    {
        // Iterate over each item and its quantity in the inventory.
        foreach (var kvp in items)
        {
            ItemSO item = kvp.Key;
            int count = kvp.Value;

            for (int i = 0; i < count; i++)
            {
                // Determine a drop position near the player with a small random offset.
                Vector3 dropPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                Quaternion dropRotation = Quaternion.identity;

                // Instantiate the prefab associated with the ore.
                Item droppedOre = Instantiate(item.itemPrefab, dropPosition, dropRotation);

                // Retrieve the NetworkObject component and spawn it on the network.
                NetworkObject netObj = droppedOre.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }
                else
                {
                    Debug.LogWarning("Dropped ore does not have a NetworkObject component.");
                }
            }
        }

        items.Clear();
        UiManager.Instance.inventoryPage.UpdateInventory(items);
        UiManager.Instance.playerHud.UpdateWeightBar(0, maxWeight);
    }
}