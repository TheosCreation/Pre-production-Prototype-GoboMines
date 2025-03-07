using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shop : NetworkBehaviour, IInteractable
{
    [SerializeField] private List<Item> purchasableItems = new List<Item>();
    [SerializeField] private string interactionText = "";
    public string InteractionText { get => interactionText; set => interactionText = value; }

    public void Interact(PlayerController player)
    {
        if (purchasableItems == null || purchasableItems.Count == 0)
        {
            Debug.LogWarning("No items available for purchase.");
            return;
        }

        // For example, select the first item from the list.
        Item selectedItem = purchasableItems[0];
        if (CurrencyManager.Instance.GetTotalMoney() < selectedItem.itemSO.saleValue) return;
        CurrencyManager.Instance.TakeMoney(selectedItem.itemSO.saleValue);
        if (selectedItem.holdable)
        {
            // Ensure that the item prefab is assigned.
            if (selectedItem != null)
            {
                // Instantiate the prefab at the player's item holder position.
                Item itemInstance = Instantiate(selectedItem, player.itemHolder.transform.position, Quaternion.identity);
                player.itemHolder.Add(itemInstance);
                itemInstance.Init();
            }
            else
            {
                Debug.LogError("Selected holdable item has no prefab assigned.");
            }
        }
        else
        {
            // For non-holdable items, add directly to the inventory.
            player.inventory.AddItemToInventory(selectedItem.itemSO, 1);
        }
    }
}