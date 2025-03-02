public interface IInteractable
{
    string InteractionText { get; set; }
    void Interact(PlayerController player);
}