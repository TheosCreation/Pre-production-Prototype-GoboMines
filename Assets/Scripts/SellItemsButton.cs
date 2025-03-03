using UnityEngine;

public class SellItemsButton : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText;
    [SerializeField] private SellItemManager sellManager;
    public string InteractionText { get => interactionText; set => interactionText = value; }

    public void Interact(PlayerController player)
    {
        sellManager.SellItems();
    }
}
