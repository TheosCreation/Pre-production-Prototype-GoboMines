using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class SellItemManager : NetworkBehaviour
{
    public List<Item> itemsInRange = new List<Item>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<Item>(out Item item) && item.canSell)
        {
            itemsInRange.Add(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.TryGetComponent<Item>(out Item item) && itemsInRange.Contains(item) && item.canSell)
        {
            itemsInRange.Remove(item);
        }
    }

    public void SellItems()
    {
        if (!IsServer) return;

        foreach (Item item in new List<Item>(itemsInRange))
        {
            if (item != null)
            {
                SellItem(item);
            }
        }
        itemsInRange.Clear(); // Clear the list after selling
    }

    private void SellItem(Item item)
    {
        CurrencyManager.Instance.AddMoney(item.itemSO.saleValue);

        item.GetComponent<NetworkObject>().Despawn(true);
    }
}