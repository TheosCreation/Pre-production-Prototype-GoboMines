using TMPro;
using UnityEngine;

public class MoneyDisplayText : MonoBehaviour
{
    TMP_Text m_textbox;

    private void Start()
    {
        m_textbox = GetComponent<TMP_Text>();
        CurrencyManager.Instance.OnMoneyChanged += UpdateText;
    }

    private void UpdateText(float money)
    {
        m_textbox.text = "Total Cash: " + money.ToString();
    }
}
