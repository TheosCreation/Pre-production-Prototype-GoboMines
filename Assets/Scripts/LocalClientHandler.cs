using System;
using UnityEngine;

public class LocalClientHandler : Singleton<LocalClientHandler>
{
    private Camera tempCamera;
    private int currentCameraIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        tempCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        // Only check for input when the temporary camera is active.
        if (tempCamera.gameObject.activeSelf)
        {
            // Using the InputManager's Player.Interact action instead of Input.GetMouseButtonDown(0)
            if (InputManager.Instance.Input.Player.Interact.triggered)
            {
                if (NetworkSpawnHandler.Instance.playersConnected.Count == 0)
                    return;

                // Cycle to the next player
                currentCameraIndex = (currentCameraIndex + 1) % NetworkSpawnHandler.Instance.playersConnected.Count;
                SetCameraToPlayer(currentCameraIndex);
            }
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
        // Apply settings to the player's look sensitivity and other options as needed.
    }
}
