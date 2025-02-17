using Unity.VisualScripting;
using UnityEngine;

public class OreNode : MonoBehaviour
{
    public float TotalOre = 100f; // Total ore in the node
    public float MiningSpeed = 10f; // Defines how quickly the node is mined
    public float DiminishingFactor = 0.9f; // Defines how quickly the node runs out of ore
    public ParticleSystem SparkleEffect; // The sparkles when encountering the ore
    public ParticleSystem DustEffect; // The dust when breaking the ore

    private float PercentageMined = 0f;
    private bool isBeingMined = false;

    // Diff types of ores - Do sepereately later to include sell prices etc.
    private enum OreType
    {
        Copper,
        Silver,
        Gold,
        Platinum
    };

    // Player Reference for multiplayer idk how to implement
    public Null PlayerRef;


    private void Update()
    {
        if (isBeingMined)
        {
            MineOre();
        }
    }

    public void StartMining()
    {
        isBeingMined = true;
        if (DustEffect != null)
        {
            DustEffect.Play();
        }
    }

    public void StopMining()
    {
        isBeingMined = false;
        if (DustEffect != null)
        {
            DustEffect.Stop();
        }
    }

    private void MineOre()
    {
        if (TotalOre <= 0)
        {
            // Destroy Node
            return;
        }

        // Calculate Diminishing Returns For Ore
        float effectiveSpeed = MiningSpeed * Mathf.Pow(DiminishingFactor, PercentageMined);

        // Mine Ore Based on Speed etc.
        float minedThisFrame = effectiveSpeed * Time.deltaTime;
        PercentageMined += minedThisFrame / 100f;

        // Remove ore from node
        TotalOre -= minedThisFrame;

        // Clamp values so it doesnt over mine the node
        TotalOre = Mathf.Max(TotalOre, 0f);
        PercentageMined = Mathf.Min(PercentageMined, 100f);

        // Mining Progress Check
        Debug.Log($"Ore mined: {minedThisFrame}, Total Ore Left: {TotalOre}");

        // Give the mined ore to the player
        // PlayerRef.AddToInventory(OreType)

    }
}