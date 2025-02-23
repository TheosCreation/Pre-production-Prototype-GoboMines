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
}
