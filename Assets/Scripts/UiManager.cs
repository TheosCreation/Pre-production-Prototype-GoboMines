using System;
using UnityEngine;

public class UiManager : Singleton<UiManager>
{
    [SerializeField] private ConnectionPage connectionPage;
    [SerializeField] private PlayerHud playerHud;
    //[SerializeField] private PauseMenu pauseScreen;
    public void OpenPlayerHud()
    {
        playerHud.SetActive(true);
        connectionPage.SetActive(false);
    }

    public void PauseMenu(bool isPaused)
    {
        //pauseScreen.SetActive(true);
    }

}
