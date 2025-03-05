using Unity.Netcode;
using UnityEngine;

public class OreNode : NetworkBehaviour, IDamageable
{
    public int totalOre = 100; // Total ore in the node
    private int initialOre;
    public ParticleSystem dustEffectPrefab; // Dust effect when mining
    public ParticleSystem HitParticlePrefab { get => dustEffectPrefab; set => dustEffectPrefab = value; }
    [SerializeField] private AudioClip[] hitSounds;
    public AudioClip[] HitSounds { get => hitSounds; set => hitSounds = value; }

    private bool isDead = false;
    public bool IsDead { get => isDead; set => isDead = value; }
    
    public OreSO ore;

    public int maxHealth = 100;
    private int health = 100; // Health of the node


    void Start()
    {
        // Initialize health and totalOre with NetworkVariables
        health = maxHealth;
        initialOre = totalOre;
    }

    public int Health
    {
        get => health;
        set
        {
            health = value;
            if (health <= 0)
            {
                IsDead = true;
                DestroyOreNode();
            }
        }
    }

    private void DestroyOreNode()
    {
        NetworkObject.Despawn(true);
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
        oreToMine = Mathf.Clamp(oreToMine, 0, totalOre);

        totalOre -= oreToMine;

        // Add the mined ore to the player's inventory
        if (oreToMine > 0)
        {
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
            Debug.Log("Ore Mined: " + oreToMine);
        }
    }


    public void TakeDamage(int amount, GameObject fromObject)
    {
        int oreToMine = Mathf.Min(totalOre, amount); 

        if (oreToMine > 0)
        {
            totalOre -= oreToMine;

            // Drop Ore At Object Location Or Give To Object Inventory
        }

        Health -= amount;
    }
}
