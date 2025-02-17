using UnityEngine;

// Maybe modulate this so ores are items
// Diff types of ores - Do sepereately later to include sell prices etc.
public enum OreType
{
    Copper,
    Silver,
    Gold,
    Platinum
};

public class OreNode : MonoBehaviour
{
    public float totalOre = 100f; // Total ore in the node
    public float miningSpeed = 10f; // Defines how quickly the node is mined
    public float diminishingFactor = 0.9f; // Defines how quickly the node runs out of ore
    public ParticleSystem sparkleEffect; // The sparkles when encountering the ore
    public ParticleSystem dustEffect; // The dust when breaking the ore
    public OreType oreType;
    private float percentageMined = 0f;
    private bool isBeingMined = false;


    // Player Reference
    private PlayerController PlayerRef;


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
        PlayerRef = player;
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

    // Re do later to do int values to the inventory and make it not constantly adjust a value in the inventory
    // just call this function once and awhile instead of in Update thus making it easy to trigger so feedback to the player with Ui
    private void MineOre()
    {
        if (totalOre <= 0)
        {
            // Destroy Node
            return;
        }

        // Calculate Diminishing Returns For Ore
        float effectiveSpeed = miningSpeed * Mathf.Pow(diminishingFactor, percentageMined);

        // Mine Ore Based on Speed etc.
        float minedThisFrame = effectiveSpeed * Time.deltaTime;
        percentageMined += minedThisFrame / 100f;

        // Remove ore from node
        totalOre -= minedThisFrame;

        // Clamp values so it doesnt over mine the node
        totalOre = Mathf.Max(totalOre, 0f);
        percentageMined = Mathf.Min(percentageMined, 100f);

        // Mining Progress Check
        Debug.Log($"Ore mined: {minedThisFrame}, Total Ore Left: {totalOre}");

        // Give the mined ore to the player
        PlayerRef.AddToInventory(oreType, minedThisFrame);
    }
}