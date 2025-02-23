using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInventoryUi : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text textBox;
    public void SetIconImage(Sprite icon)
    {
        image.sprite = icon;
    }

    public void SetItemAmount(int amount)
    {
        textBox.text = amount.ToString();
    }

}
