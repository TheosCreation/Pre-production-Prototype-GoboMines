using TMPro;
using UnityEngine;

public class PlayerHud : UiPage
{
    [SerializeField] private UiBar weightBar;
    [SerializeField] private TMP_Text interactionTextBox;
    [SerializeField] private TMP_Text ammoLeftTextBox;
    [SerializeField] private TMP_Text ammoReserveTextBox;
    [SerializeField] private TMP_Text connectionCodeTextBox;
    public FlashImage damageFlash;

    public void UpdateWeightBar(float weight, float maxWeight)
    {
        //weightBar.textBox.text = $"Weight {weight}/{maxWeight}";
        //weightBar.UpdateBar(weight / maxWeight);
    }

    public void UpdateAmmo(int ammo, int ammoReserve)
    {
        if(ammoReserve <= 0 && ammo <= 0)
        {
            ammoLeftTextBox.text = "";
            ammoReserveTextBox.text = "";
        }
        else
        {
            ammoLeftTextBox.text = ammo.ToString();
            ammoReserveTextBox.text = ammoReserve.ToString();
        }
    }

    public void UpdateInteractText(string interactionText)
    {
        interactionTextBox.text = interactionText;
    }

    public void UpdateConnectionCodeText(string connectionCodeText)
    {
        connectionCodeTextBox.text = "Connection Code: " + connectionCodeText;
    }
}
