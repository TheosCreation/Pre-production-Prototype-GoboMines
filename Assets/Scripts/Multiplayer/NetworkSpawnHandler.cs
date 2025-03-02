using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSpawnHandler : NetworkBehaviour
{
    public static NetworkSpawnHandler Instance;

    public List<PlayerController> playersConnected;

    public PlayerController playerPrefab;
    public float playerHeight = 2f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client disconnected: {clientId}");
            PlayerController playerToRemove = playersConnected.Find(player => player.OwnerClientId == clientId);
            if (playerToRemove != null)
            {
                playersConnected.Remove(playerToRemove);
                Debug.Log($"Removed player with Client ID: {clientId}");
            }
        }
    }


    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client connected: {clientId}");
            RequestPlayerSpawnServerRpc(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayerSpawnServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            Debug.LogError("Only the server can handle player respawning.");
            return;
        }

        Debug.Log($"Respawning player for Client ID: {clientId}");

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Spawn");
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points found!");
            return;
        }

        GameObject spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        Vector3 spawnPosition = spawnPoint.transform.position;
        spawnPosition.y += playerHeight / 2;

        // Instantiate the player prefab on the server
        PlayerController newPlayer = Instantiate(playerPrefab, spawnPosition, spawnPoint.transform.rotation);
        NetworkObject networkObject = newPlayer.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);

        // Add the player to the list
        playersConnected.Add(newPlayer);

        // Debug ownership
        Debug.Log($"Player spawned with OwnerClientId: {networkObject.OwnerClientId}, Expected: {clientId}");

        //access the client that connected level manager
        NotifyClientPlayerSpawnedClientRpc(clientId);
    }

    [ClientRpc]
    private void NotifyClientPlayerSpawnedClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        // Check if this is the client that connected
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log($"Client {clientId} notified of player spawn.");
            LocalClientHandler.Instance.HandlePlayerSpawned(clientId);
        }
    }
}