using TMPro;
using UnityEngine;

public class PlayerHud : UiPage
{
    [SerializeField] private UiBar weightBar;
    [SerializeField] private TMP_Text interactionTextBox;
    [SerializeField] private TMP_Text ammoLeftTextBox;
    [SerializeField] private TMP_Text ammoReserveTextBox;

    public void UpdateWeightBar(float weight, float maxWeight)
    {
        weightBar.textBox.text = $"Weight {weight}/{maxWeight}";
        weightBar.UpdateBar(weight / maxWeight);
    }

    public void UpdateAmmo(float ammo)
    {

    }
    public void UpdateInteractText(string interactionText)
    {
        interactionTextBox.text = interactionText;
    }
}
