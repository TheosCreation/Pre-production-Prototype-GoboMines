using UnityEngine;

public class OreNode : MonoBehaviour, IDamageable
{
    public int totalOre = 100; // Total ore in the node
    private int initialOre;
    public ParticleSystem sparkleEffect; // Sparkle effect when in vicinity
    public ParticleSystem dustEffect; // Dust effect when mining
    
    public ParticleSystem HitParticlePrefab { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public AudioClip HitSound { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

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
        oreToMine = Mathf.Clamp(oreToMine, 0, totalOre);

        totalOre -= oreToMine;

        // Add the mined ore to the player's inventory
        if (oreToMine > 0)
        {
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
            Debug.Log("Ore Mined: " + oreToMine);
        }

        if (dustEffect != null)
        {
            dustEffect.Play();
        }

        if (Health <= 0)
        {
            DestroyOreNode();
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

        // Play dust effect if available
        if (dustEffect != null)
        {
            dustEffect.Play();
        }

        // Check if the node is dead and destroy it if necessary
        if (Health <= 0)
        {
            DestroyOreNode();
        }
    }
}
