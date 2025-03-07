using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSpawnHandler : NetworkBehaviour
{
    public static NetworkSpawnHandler Instance;

    public List<ulong> clientsConnected = new List<ulong>();
    public Dictionary<ulong, PlayerController> playersAlive = new Dictionary<ulong, PlayerController>();
    public List<ulong> clientsToRespawn = new List<ulong>();

    public PlayerController playerPrefab;
    public float playerHeight = 2f;
    public AudioSource audioSourceExamplePrefab;

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
            clientsConnected.Remove(clientId);

            RemovePlayer(clientId);
            Debug.Log($"Removed player with Client ID: {clientId}");
        }
    }


    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client connected: {clientId}");
            clientsConnected.Add(clientId);
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
        playersAlive.Add(clientId, newPlayer);

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
    [ServerRpc(RequireOwnership = false)]
    public void MarkPlayerToRespawnServerRpc(ulong clientId, ServerRpcParams serverRpcParams)
    {
        RemovePlayer(clientId);
        clientsToRespawn.Add(clientId);
    }

    public void SpawnParticles(ParticleSystem prefab, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // Only the server should handle sound creation
        if (!IsServer) return;

        // Spawn hit particles
        ParticleSystem hitParticles = Instantiate(prefab, spawnPosition, spawnRotation);
        NetworkObject netObj = hitParticles.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        // Despawn the sound maker after the clip finishes
        float duration = hitParticles.main.duration + hitParticles.main.startLifetime.constant;
        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(netObj, duration);
    }

    public void SpawnSound(AudioClip audioClip, Vector3 spawnPosition, float volume = 1.0f)
    {
        AudioSource soundSource = Instantiate(audioSourceExamplePrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = soundSource.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        soundSource.PlayOneShot(audioClip, volume);

        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(netObj, audioClip.length + 0.1f);
    }

    private void RemovePlayer(ulong clientId)
    {
        if (playersAlive.ContainsKey(clientId))
        {
            playersAlive.Remove(clientId);
        }
        if (clientsToRespawn.Contains(clientId))
        {
            clientsToRespawn.Remove(clientId);
        }
    }


    [ServerRpc]
    public void RespawnConnectedPlayersServerRpc()
    {
        foreach (ulong clientToRespawn in clientsToRespawn)
        {
            RequestPlayerSpawnServerRpc(clientToRespawn);
        }
        clientsToRespawn.Clear();
    }
}