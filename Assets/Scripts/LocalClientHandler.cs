using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalClientHandler : Singleton<LocalClientHandler>
{
    private Camera tempCamera;
    private int currentCameraIndex = 0;
    private Camera playerCamera = null;

    protected override void Awake()
    {
        base.Awake();
        tempCamera = GetComponentInChildren<Camera>();

        InputManager.Instance.Input.Player.Interact.started += OnCycleCamera;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (InputManager.Instance != null && InputManager.Instance.Input != null)
            InputManager.Instance.Input.Player.Interact.started -= OnCycleCamera;
    }

    private void OnCycleCamera(InputAction.CallbackContext ctx)
    {
        if (!tempCamera.gameObject.activeSelf)
            return;

        if (NetworkSpawnHandler.Instance.playersConnected.Count == 0)
            return;

        currentCameraIndex = (currentCameraIndex + 1) % NetworkSpawnHandler.Instance.playersConnected.Count;
        SetCameraToPlayer(currentCameraIndex);

    }
    private void Update()
    {
        if(playerCamera!=null)
        {     
            tempCamera.transform.position = playerCamera.transform.position;
            tempCamera.transform.rotation = playerCamera.transform.rotation;
        }
    }
    public void TempCamera(bool enabled)
    {
        tempCamera.gameObject.SetActive(enabled);
    }

    public void SetCameraToPlayer(int index)
    {
        if (NetworkSpawnHandler.Instance.playersConnected.Count == 0)
            return;

        currentCameraIndex = index % NetworkSpawnHandler.Instance.playersConnected.Count;
        tempCamera.gameObject.SetActive(true);

        playerCamera = NetworkSpawnHandler.Instance.playersConnected[currentCameraIndex].playerLook.playerCamera;

      
    }

    public void HandlePlayerSpawned(ulong clientId)
    {
        TempCamera(false);
        PauseManager.Instance.SetPaused(false);
        UiManager.Instance.OpenPlayerHud();
    }
}
