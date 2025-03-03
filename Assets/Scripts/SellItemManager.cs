using UnityEngine;
using System.Collections.Generic;

public class SellItemManager : MonoBehaviour
{
    private List<Item> itemsInRange = new List<Item>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        Item item = other.GetComponent<Item>();
        if (item != null && !itemsInRange.Contains(item) && item.canSell)
        {
            itemsInRange.Add(item);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Item item = other.GetComponent<Item>();
        if (item != null && itemsInRange.Contains(item) && item.canSell)
        {
            itemsInRange.Remove(item);
        }
    }

    public void SellItems()
    {
        foreach (Item item in new List<Item>(itemsInRange))
        {
            SellItem(item);
        }
        itemsInRange.Clear(); // Clear the list after selling
    }

    private void SellItem(Item item)
    {
        CurrencyManager.Instance.AddMoney(item.itemSO.saleValue);

        Destroy(item.gameObject);
    }
}