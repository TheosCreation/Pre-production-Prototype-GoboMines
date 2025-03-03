using UnityEngine;
using UnityEngine.UI;

public class HostSession : MonoBehaviour
{
    private Button m_button;

    private void Awake()
    {
        m_button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        m_button.onClick.AddListener(JoinSession);
    }

    private void OnDisable()
    {
        m_button.onClick.RemoveListener(JoinSession);
    }

    public async void JoinSession()
    {
        UiManager.Instance.OpenLoadingPage();
        // Await the completion of the asynchronous StartSessionAsHost method
        await SessionManager.Instance.StartSessionAsHost();
        GridManager.Instance.InitializeGrid();
        GameManager.Instance.onHostEvent.Invoke();
    }
}