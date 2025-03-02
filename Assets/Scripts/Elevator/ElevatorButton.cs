using UnityEngine;

public class ElevatorButton : MonoBehaviour, IInteractable
{
    public bool isPressed = false;
    private bool isDisabled = false;
    [SerializeField] private ElevatorManager elevatorManager;

    public void Interact(PlayerController player)
    {
        if (isDisabled) return;

        isPressed = true;
        isDisabled = true;
        elevatorManager.ToggleElevator();

    }

    // Remove After Actual Implementation - Just for testing :)
    private void Update()
    {
        if (isPressed) 
        {

            elevatorManager.ToggleElevator();
            isPressed = false;
        }
    }

    public void ResetButton()
    {
        isDisabled = false;
        isPressed = false;
    }
}