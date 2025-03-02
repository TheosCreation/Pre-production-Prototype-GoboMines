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
        // Determine how much ore can actually be mined
        int oreToMine = Mathf.Min(totalOre, damage);

        // Reduce ore and give it to the player
        if (oreToMine > 0)
        {
            totalOre -= oreToMine;
            fromPlayer.inventory.AddItemToInventory(ore, oreToMine);
        }

        // Reduce health by the amount of ore actually mined
        Health -= oreToMine;

        // Play dust effect if available
        if (dustEffect != null)
        {
            dustEffect.Play();
        }

        // If no ore or health remains, destroy the node
        if (totalOre <= 0 || Health <= 0)
        {
            Health = 0; // Ensure health doesn't go negative
            totalOre = 0; // Ensure ore doesn't go negative
            DestroyOreNode();
        }
    }


}
