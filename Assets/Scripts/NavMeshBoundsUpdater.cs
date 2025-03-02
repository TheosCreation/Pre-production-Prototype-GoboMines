/*using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBoundsUpdater : MonoBehaviour
{
    [Header("NavMesh Surfaces")]
    [Tooltip("The NavMeshSurface for the outside area.")]
    public NavMeshSurface outsideSurface;  // Assign via Inspector
    [Tooltip("The NavMeshSurface for the inside area (left unchanged).")]
    public NavMeshSurface insideSurface;   // Assign via Inspector

    [Header("Level Settings")]
    [Tooltip("Parent of all procedural level objects.")]
    public Transform levelRoot;
    [Tooltip("Extra margin outside the level bounds.")]
    public float extraPadding = 5f;

    void Start()
    {
      
    }
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
        outsideSurface.BuildNavMesh();
        insideSurface.BuildNavMesh();
        
        UpdateOutsideNavMeshBounds();
    }
    public void UpdateOutsideNavMeshBounds()
    {
        // Check that levelRoot exists.
        if (levelRoot == null)
        {
            Debug.LogWarning("Level root not assigned!");
            return;
        }

        // Calculate the bounds from all renderers under levelRoot.
        Renderer[] renderers = levelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found to calculate level bounds.");
            return;
        }

        Bounds levelBounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            levelBounds.Encapsulate(rend.bounds);
        }

        // Expand the bounds by extra padding.
        levelBounds.Expand(extraPadding);

        // Create (or use an existing) helper GameObject to define our volume.
        // You could also adjust the transform and size of your outsideSurface's GameObject.
        GameObject volume = new GameObject("NavMeshVolume_Outside");
        volume.transform.position = levelBounds.center;

        // Optional: add a BoxCollider to visualize or represent the bounds.
        BoxCollider box = volume.AddComponent<BoxCollider>();
        box.size = levelBounds.size;
        box.isTrigger = true;

        // Depending on how your Outside NavMeshSurface is set up, you may need to parent
        // the volume (or use its bounds) so that your NavMeshSurface collects geometry within that volume.
        // For example, if using Collect Objects: Volume, set the GameObject with the volume as a child:
        volume.transform.parent = outsideSurface.transform;

        // Finally, build the outside navmesh.
        outsideSurface.BuildNavMesh();
    }
}*/