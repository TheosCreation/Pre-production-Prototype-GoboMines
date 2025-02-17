using System.Collections.Generic;

public class MenuManager : UiPage
{
    protected readonly Stack<UiPage> navigationHistory = new Stack<UiPage>();

    public UiPage[] allUiPages;

    public void OpenPage(UiPage page)
    {
        //Debug.Log($"Pushing {page.name} onto stack");
        navigationHistory.Push(page);
        ActivatePage(page);
    }

    protected void ActivatePage(UiPage uiPageToActivate)
    {
        // Deactivate all pages first
        foreach (UiPage uiPage in allUiPages)
        {
            uiPage.SetActive(false);
        }

        // Finally, activate the selected page
        uiPageToActivate.SetActive(true);
    }

    public virtual void Back() { }
}