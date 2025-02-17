using UnityEngine;

public class PauseManager : Singleton<PauseManager>
{
    [SerializeField] private bool isPaused = false;
    public bool canUnpause = true;

    private void Start()
    {
        CheckPaused();
    }

    private void CheckPaused()
    {
        if (!canUnpause) return;

        if (isPaused)
        {
            Pause();
        }
        else
        {
            UnPause();
        }
    }

    public void SetPaused(bool pausedStatus)
    {
        isPaused = pausedStatus;
        CheckPaused();
    }

    public bool TryPause()
    {
        if(isPaused)
        {
            return false;
        }
        else
        {
            isPaused = true;
            CheckPaused();
            if (UiManager.Instance != null)
            {
                UiManager.Instance.PauseMenu(isPaused);
            }
            return true;

        }
    }

    public void Pause()
    {
        InputManager.Instance.DisablePlayerInput();

        //Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        isPaused = true;
    }

    private void UnPause()
    {
        //Time.timeScale = 1;
        InputManager.Instance.EnablePlayerInput();

        //DiscordManager.ChangeActivity(GameManager.Instance.currentGamemode, GameManager.Instance.GameState.currentLevelIndex);

        Cursor.lockState = CursorLockMode.Locked;
    }
}