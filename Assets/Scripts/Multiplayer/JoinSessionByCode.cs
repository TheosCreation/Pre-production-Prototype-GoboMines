using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinSessionByCode : MonoBehaviour
{
    private Button m_Button;
    private TMP_InputField m_InputField;

    private void Awake()
    {
        m_Button = GetComponentInChildren<Button>();

        m_Button.onClick.AddListener(EnterSession);

        m_InputField = GetComponentInChildren<TMP_InputField>();

        m_InputField.onEndEdit.AddListener(value =>
        {
            if (InputManager.Instance.Input.UI.Submit.triggered && !string.IsNullOrEmpty(value))
            {
                EnterSession();
            }
        });

        m_InputField.onValueChanged.AddListener(value =>
        {
            m_Button.interactable = !string.IsNullOrEmpty(value);
        });
    }

    private async void EnterSession()
    {
        await SessionManager.Instance.JoinSessionByCode(m_InputField.text);
    }
}
