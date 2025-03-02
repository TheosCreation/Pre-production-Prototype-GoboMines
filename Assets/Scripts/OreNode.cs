using UnityEngine;

public class OreNode : MonoBehaviour, IDamageable
{
    public int totalOre = 100; // Total ore in the node
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
        if (isDead) return; // Prevent further mining

        float powerFactor = 1.5f; // Adjust for more or less diminishing
        int previousOre = totalOre;

        // Calculate new ore amount using diminishing function
        int newOre = Mathf.RoundToInt(totalOre * Mathf.Pow((float)Health / maxHealth, powerFactor));

        // Ore mined is the difference
        int oreToMine = Mathf.Clamp(previousOre - newOre, 0, totalOre);

        if (oreToMine > 0)
        {
            totalOre -= oreToMine;
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
        }

        // Apply damage to health
        Health -= damage;

        if (dustEffect != null)
            dustEffect.Play();

        // Check if the node is dead
        if (Health <= 0)
        {
            IsDead = true;
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
