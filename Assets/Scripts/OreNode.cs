using UnityEngine;

public class OreNode : MonoBehaviour, IDamageable
{
    public int totalOre = 100; // Total ore in the node
    public ParticleSystem sparkleEffect; // Sparkle effect when in vacinity
    public ParticleSystem dustEffect; // Dust effect when mining
    
    public ParticleSystem HitParticlePrefab { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public AudioClip HitSound { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public bool IsDead { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    
    public OreSO ore;

    public int maxHealth = 100;
    private int health = 100; // Health of the node

    void Start()
    {
        Health = maxHealth;
    }

    public int Health { get => health; 
        set
        {
            health = value;
            if (health > 0) { return; }
            DestroyOreNode();
        }
    }

    public void TakeDamage(int amount, PlayerController fromPlayer)
    {
        MineOre(fromPlayer, amount);

        Health -= amount;
        Debug.Log($"Ore node took {amount} damage. Remaining health: {health}");

        if (dustEffect != null)
        {
            dustEffect.Play();
        }
    }

    private void MineOre(PlayerController fromPlayer, int amount)
    {
        if (totalOre <= 0) return;

        // Calculate diminishing returns based on remaining health
        int oreMined = (int)((maxHealth * totalOre * amount) / (maxHealth * (maxHealth + amount)));

        // Give mined ore to the player's inventory
        if (oreMined > 0)
        {
            fromPlayer.AddToInventory(ore, oreMined);
        }

        Debug.Log($"Mined {oreMined} ore. Remaining total ore: {totalOre}");
    }

    private void DestroyOreNode()
    {
        Destroy(gameObject);
    }

    public void TakeDamage(float amount, ulong attackerId)
    {
        throw new System.NotImplementedException();
    }
}
