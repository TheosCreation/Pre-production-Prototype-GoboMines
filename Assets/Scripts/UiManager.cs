using System;
using UnityEngine;

public class UiManager : Singleton<UiManager>
{
    [SerializeField] private LoadingPage loadingPage;
    [SerializeField] private ConnectionPage connectionPage;
    public PlayerHud playerHud;
    public InventoryUiPage inventoryPage;
    //[SerializeField] private PauseMenu pauseScreen;
    private void Start()
    {
        OpenConnectionMenu();
    }
    public void OpenLoadingPage()
    {
        loadingPage.SetActive(true);
        connectionPage.SetActive(false);
        playerHud.SetActive(false);

    }
    public void OpenConnectionMenu()
    {
        connectionPage.SetActive(true);
        loadingPage.SetActive(false);
        playerHud.SetActive(false);
    }

    public void OpenPlayerHud()
    {
        playerHud.SetActive(true);
        connectionPage.SetActive(false);
        loadingPage.SetActive(false);
    }

    public void PauseMenu(bool isPaused)
    {
        //pauseScreen.SetActive(true);
    }

    public void NotifyItem(ItemSO item, int amount)
    {
        Debug.Log("Ui manager notified of a item added");
    }

    public void ToggleInventory()
    {
        bool inventoryPageStatus = inventoryPage.isActiveAndEnabled;
        inventoryPage.SetActive(!inventoryPageStatus);
        playerHud.SetActive(inventoryPageStatus);
        if(inventoryPageStatus)
        {
            PauseManager.Instance.UnPause();
        }
        else
        {

            PauseManager.Instance.Pause();
        }
    }
}
