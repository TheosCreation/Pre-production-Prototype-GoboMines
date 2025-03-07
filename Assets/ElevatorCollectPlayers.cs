using System.Collections.Generic;
using UnityEngine;

public class ElevatorCollectPlayers : MonoBehaviour
{
    private List<PlayerController> playerInRange = new List<PlayerController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            playerInRange.Add(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController player) && playerInRange.Contains(player))
        {
            playerInRange.Remove(player);
        }
    }

    public bool CheckIfPlayerAreIn()
    {
        NetworkSpawnHandler.Instance.UpdatePlayersConnectedServerRpc();
        if (playerInRange.Count == NetworkSpawnHandler.Instance.playersConnected.Count)
        {
            return true;
        }
        return false;
    }
}
