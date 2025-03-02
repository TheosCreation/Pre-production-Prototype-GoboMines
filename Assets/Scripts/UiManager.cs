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
        OpenLoadingPage();
    }
    public void OpenLoadingPage()
    {
        loadingPage.SetActive(true);
        connectionPage.SetActive(false);
        playerHud.SetActive(false);

    }
    public void OpenConnectionMenu()
    {
        loadingPage.SetActive(false);
        connectionPage.SetActive(true);
    }

    public void OpenPlayerHud()
    {
        playerHud.SetActive(true);
        connectionPage.SetActive(false);
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
    }
}
