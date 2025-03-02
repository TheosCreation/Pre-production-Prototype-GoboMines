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
        // First, calculate how much ore should be mined based on the amount of damage taken
        int oreToMine = Mathf.Min(totalOre, damage); // Mine up to the total ore, but not more

        // Mine the ore and update the player's inventory
        if (oreToMine > 0)
        {
            totalOre -= oreToMine;
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
        }

        // Apply damage to health
        Health -= damage;

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
