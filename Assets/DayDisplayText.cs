using TMPro;
using UnityEngine;

public class DayDisplayText : MonoBehaviour
{
    TMP_Text m_textbox;

    private void Start()
    {
        m_textbox = GetComponent<TMP_Text>();
    }

    public void UpdateText(int day)
    {
        m_textbox.text = "Day: " + day.ToString();
    }
}