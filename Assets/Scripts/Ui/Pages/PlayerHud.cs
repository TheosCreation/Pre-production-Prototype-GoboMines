using UnityEngine;

public class PlayerHud : UiPage
{
    [SerializeField] private UiBar weightBar;

    public void UpdateWeightBar(float weight, float maxWeight)
    {
        weightBar.textBox.text = $"Weight {weight}/{maxWeight}";
        weightBar.UpdateBar(weight / maxWeight);
    }
}
