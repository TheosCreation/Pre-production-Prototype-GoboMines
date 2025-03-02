using Unity.AI.Navigation;
using UnityEngine;

public class BakeOnComplete: MonoBehaviour
{
    private Generator generator { get; set; }

    private void Awake()
    {
        generator = FindFirstObjectByType<Generator>();

        if (generator != null)
        {
            generator.OnGenerationComplete += HandleGenerationComplete;
        }
        else
        {
            Debug.LogError("Could not find Generator instance in the scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when this object is destroyed
        if (generator != null)
        {
            generator.OnGenerationComplete -= HandleGenerationComplete;
        }
    }

    private void HandleGenerationComplete()
    {
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }
}
