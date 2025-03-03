using UnityEngine;

public class Terminal : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText;
    public string InteractionText { get => interactionText; set => interactionText = value; }

    public void Interact(PlayerController player)
    {

    }
}
