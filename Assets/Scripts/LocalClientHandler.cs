using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalClientHandler : Singleton<LocalClientHandler>
{
    private Camera tempCamera;
    private int currentCameraIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        tempCamera = GetComponentInChildren<Camera>();

        // Subscribe to the interact input event.
        InputManager.Instance.Input.Player.Interact.started += OnCycleCamera;
    }

    private void OnDestroy()
    {
        // Unsubscribe from the input event.
        if (InputManager.Instance != null && InputManager.Instance.Input != null)
            InputManager.Instance.Input.Player.Interact.started -= OnCycleCamera;
    }

    // This event handler is called when the player presses the interact key.
    private void OnCycleCamera(InputAction.CallbackContext ctx)
    {
        // Only process if the temporary camera is active.
        if (!tempCamera.gameObject.activeSelf)
            return;

        if (NetworkSpawnHandler.Instance.playersConnected.Count == 0)
            return;

        // Cycle to the next player's camera.
        currentCameraIndex = (currentCameraIndex + 1) % NetworkSpawnHandler.Instance.playersConnected.Count;
        SetCameraToPlayer(currentCameraIndex);
    }

    public void TempCamera(bool enabled)
    {
        tempCamera.gameObject.SetActive(enabled);
    }

    public void SetCameraToPlayer(int index)
    {
        if (NetworkSpawnHandler.Instance.playersConnected.Count == 0)
            return;

        // Ensure the index is valid.
        currentCameraIndex = index % NetworkSpawnHandler.Instance.playersConnected.Count;
        tempCamera.gameObject.SetActive(true);

        // Get the target player's camera.
        var playerCamera = NetworkSpawnHandler.Instance.playersConnected[currentCameraIndex].playerLook.playerCamera;

        // Update the temporary camera's transform to match the target player's camera.
        tempCamera.transform.position = playerCamera.transform.position;
        tempCamera.transform.rotation = playerCamera.transform.rotation;
    }

    public void HandlePlayerSpawned(ulong clientId)
    {
        // When a player spawns, disable the temporary camera.
        TempCamera(false);
        PauseManager.Instance.SetPaused(false);
        UiManager.Instance.OpenPlayerHud();
        // Additional settings for player look sensitivity can be applied here.
    }
}
