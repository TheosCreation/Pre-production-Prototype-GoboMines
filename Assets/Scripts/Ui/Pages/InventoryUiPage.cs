using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryUiPage : UiPage
{
    [SerializeField] private Transform grid;
    private Dictionary<ItemSO, ItemInventoryUi> displayedItems = new Dictionary<ItemSO, ItemInventoryUi>();
    [SerializeField] private ItemInventoryUi itemDisplayPrefab;

    public void UpdateInventory(Dictionary<ItemSO, int> items)
    {
        if(items.Count == 0)
        {
            foreach (var displayedItem in displayedItems)
            {
                Destroy(displayedItem.Value.gameObject);
            }

            displayedItems.Clear();
        }

        foreach (var item in items)
        {
            if (displayedItems.ContainsKey(item.Key))
            {
                displayedItems[item.Key].SetItemAmount(item.Value);
            }
            else
            {
                ItemInventoryUi itemDisplay = Instantiate(itemDisplayPrefab, grid);
                itemDisplay.SetIconImage(item.Key.icon);
                itemDisplay.SetItemAmount(item.Value);
                displayedItems[item.Key] = itemDisplay;
            }
        }
    }

    public void NotifyPlayerDropAllItems()
    {
        foreach (var player in NetworkSpawnHandler.Instance.playersConnected)
        {
            if (player.IsOwner) // or use player.IsLocalPlayer if that's what you have
            {
                player.DropAllItems();
                break; // Exit once the local player's items are dropped
            }
        }
    }
}
