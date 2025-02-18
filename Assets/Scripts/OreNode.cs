using UnityEngine;

public class OreNode : MonoBehaviour, IDamageable
{
    public int totalOre = 100; // Total ore in the node
    public float miningSpeed = 10f; // Defines how quickly the node is mined
    public float diminishingFactor = 0.9f; // Diminishing returns based on health
    public ParticleSystem sparkleEffect; // Sparkle effect when in vacinity
    public ParticleSystem dustEffect; // Dust effect when mining
    public OreType oreType; // Type of ore this node contains

    private float health = 100f; // Health of the node
    private bool isBeingMined = false;
    private PlayerController playerRef; // Reference to the player mining this node
    private float miningCarryover = 0f; // Fractional ore mined to be carried over (so you get full amount even with fractional damage)

    private void Update()
    {
        if (isBeingMined)
        {
            MineOre();
        }
    }

    public void StartMining(PlayerController player)
    {
        isBeingMined = true;
        playerRef = player;

        if (dustEffect != null)
        {
            dustEffect.Play();
        }

    }

    public void StopMining()
    {
        isBeingMined = false;

        if (dustEffect != null)
        {
            dustEffect.Stop();
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Ore node took {amount} damage. Remaining health: {health}");

        if (health <= 0)
        {
            DestroyOreNode();
        }
    }

    private void MineOre()
    {
        if (totalOre <= 0) return;

        // Calculate diminishing returns based on remaining health
        float effectiveSpeed = miningSpeed * Mathf.Pow(diminishingFactor, 1f - (health / 100f));

        // Mine ore based on effective speed and delta time
        float minedThisFrame = effectiveSpeed * Time.deltaTime;

        // Add carryover to compensate for fractional mining damage
        minedThisFrame += miningCarryover;

        // Convert mined ore to an integer
        int oreMined = (int)minedThisFrame;

        // Carry over any remaining fractional ore for the next frame
        miningCarryover = minedThisFrame - oreMined;

        // Reduce total ore and clamp to avoid negative values
        totalOre -= oreMined;
        totalOre = Mathf.Max(totalOre, 0);

        // Give mined ore to the player's inventory
        if (oreMined > 0)
        {
            // playerRef.AddToInventory(new Ore(oreType, oreMined));    -- Uncomment after adding an amount value to give to the player.
        }

        Debug.Log($"Mined {oreMined} ore. Remaining total ore: {totalOre}");

        // Have the node take damage
        TakeDamage(oreMined);
    }

    private void DestroyOreNode()
    {
        Debug.Log($"Ore node {oreType} destroyed!");
        // Optionally spawn an effect or play a sound here
        Destroy(gameObject);
    }
}
