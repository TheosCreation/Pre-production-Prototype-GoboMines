using System;
using UnityEngine;

public class LocalClientHandler : Singleton<LocalClientHandler>
{
    private Camera tempCamera;

    protected override void Awake()
    {
        base.Awake();

        tempCamera = GetComponentInChildren<Camera>();
    }

    public void TempCamera(bool enabled)
    {
        tempCamera.gameObject.SetActive(enabled);
    }

    public void HandlePlayerSpawned(ulong clientId)
    {
        TempCamera(false);
        PauseManager.Instance.SetPaused(false);
        UiManager.Instance.OpenPlayerHud();
        //apply settings to the player look sensitivity and stuff
    }
}
