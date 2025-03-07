using Unity.Netcode;
using UnityEngine;

public class OreNode : NetworkBehaviour, IDamageable
{
    public NetworkVariable<int> totalOre = new NetworkVariable<int>(100); // Total ore in the node
    private int initialOre;
    public ParticleSystem dustEffectPrefab; // Dust effect when mining
    public ParticleSystem HitParticlePrefab { get => dustEffectPrefab; set => dustEffectPrefab = value; }
    [SerializeField] private AudioClip[] hitSounds;
    public AudioClip[] HitSounds { get => hitSounds; set => hitSounds = value; }

    private bool isDead = false;
    public bool IsDead { get => isDead; set => isDead = value; }
    
    public OreSO ore;

    public int maxHealth = 100;
    private NetworkVariable<int> health = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn()
    {
        health.Value = maxHealth;
        initialOre = totalOre.Value;
    }

    public int Health
    {
        get => health.Value;
        set
        {
            health.Value = value;
            if (health.Value <= 0)
            {
                IsDead = true;
                DestroyOreNodeServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyOreNodeServerRpc()
    {
        NetworkObject.Despawn();
        Destroy(gameObject);
    }

    public void TakeDamage(int damage, PlayerController fromPlayer)
    {
        if (isDead) return;

        // Deminishing Scale
        float powerFactor = 1.5f;

        // Calculate ore left based on current health BEFORE damage
        int oreBeforeHit = Mathf.RoundToInt(initialOre * Mathf.Pow((float)Health / maxHealth, powerFactor));
    
        Health -= damage;

        // And then AFTER damage
        int oreAfterHit = Mathf.RoundToInt(initialOre * Mathf.Pow((float)Health / maxHealth, powerFactor));

        int oreToMine = oreBeforeHit - oreAfterHit;
        oreToMine = Mathf.Clamp(oreToMine, 0, totalOre.Value);

        totalOre.Value -= oreToMine;

        // Add the mined ore to the player's inventory
        if (oreToMine > 0)
        {
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
            Debug.Log("Ore Mined: " + oreToMine);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, ulong fromPlayer)
    {
        TakeDamageClientRpc(amount, fromPlayer);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount, NetworkObjectReference fromObject)
    {
        TakeDamageClientRpc(amount);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(int amount, ulong fromPlayer = 100000)
    {
        if (!IsOwner) return;
        if (isDead) return;

        // Deminishing Scale
        float powerFactor = 1.5f;

        // Calculate ore left based on current health BEFORE damage
        int oreBeforeHit = Mathf.RoundToInt(initialOre * Mathf.Pow((float)Health / maxHealth, powerFactor));

        Health -= amount;

        // And then AFTER damage
        int oreAfterHit = Mathf.RoundToInt(initialOre * Mathf.Pow((float)Health / maxHealth, powerFactor));

        int oreToMine = oreBeforeHit - oreAfterHit;
        oreToMine = Mathf.Clamp(oreToMine, 0, totalOre.Value);

        totalOre.Value -= oreToMine;

        // Add the mined ore to the player's inventory
        if (oreToMine > 0)
        {
            if(fromPlayer != 100000)
            {
                NetworkSpawnHandler.Instance.playersAlive[fromPlayer].inventory.AddItemToInventory(ore, oreToMine);
            }
            else
            {
                //Drop on ground
            }
            Debug.Log("Ore Mined: " + oreToMine);
        }
    }
}
