using UnityEngine;

public class ElevatorButton : MonoBehaviour, IInteractable
{
    [SerializeField] private ElevatorManager elevatorManager;

    [SerializeField] private string interactionText;
    public string InteractionText { get => interactionText; set => interactionText = value; }

    public void Interact(PlayerController player)
    {
        elevatorManager.ToggleElevatorServerRpc();
    }
}