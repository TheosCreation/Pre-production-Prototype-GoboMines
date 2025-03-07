using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInventoryUi : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private TMP_Text textBox2;
    public void SetIconImage(Sprite icon)
    {
        image.sprite = icon;
    }

    public void SetSellValue(int amount)
    {
        textBox2.text = "Sale Value: " + amount;
    }
    public void SetItemAmount(int amount)
    {
        textBox.text = amount.ToString();
    }

}
