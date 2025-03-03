using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemySpawner : MonoBehaviour
{
    [Header("NavMesh Surfaces")]
    // References to the two separate NavMeshSurface game objects.
    public NavMeshSurface insideSurface;
    public NavMeshSurface outsideSurface;
    [SerializeField]
    [NavMeshAreaMask]
    protected int insideArea = 0;
    [SerializeField]
    [NavMeshAreaMask]
    protected int outsideArea = 0;


    [Header("Enemy Prefab Options")]
    // The enemy prefabs for each type.
    public GameObject[] insideEnemyPrefabs;
    public GameObject[] outsideEnemyPrefabs;

    [Header("Spawn Settings")]
    public int insideEnemyCount = 5;
    public int outsideEnemyCount = 5;
    // Maximum attempts to find a valid spawn position.
    public int maxSpawnAttempts = 10;
    // Maximum allowed distance when sampling the NavMesh.
    public float spawnSampleRadius = 5f;

    // Fallback spawn area if bounds calculation fails
    [Header("Fallback Spawn Areas")]
    public Vector3 fallbackInsideCenter = Vector3.zero;
    public Vector3 fallbackInsideSize = new Vector3(10, 0, 10);
    public Vector3 fallbackOutsideCenter = Vector3.zero;
    public Vector3 fallbackOutsideSize = new Vector3(20, 0, 20);

    // Procedurally computed spawn area bounds.
    private Vector3 insideAreaCenter;
    private Vector3 insideAreaSize;
    private Vector3 outsideAreaCenter;
    private Vector3 outsideAreaSize;

    // Reference to the generator that triggers the spawn event.
    private Generator generator;

    private void Awake()
    {
        generator = FindFirstObjectByType<Generator>();

        if (generator != null)
        {
            // Subscribe to the OnGenerationComplete event.
            generator.OnGenerationComplete += HandleGenerationComplete;
        }
        else
        {
            Debug.LogError("EnemySpawner: No Generator instance found in the scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks.
        if (generator != null)
        {
            generator.OnGenerationComplete -= HandleGenerationComplete;
        }
    }

    /// <summary>
    /// Called when generation is complete.
    /// Rebuilds the nav meshes, calculates spawn areas, then spawns enemies.
    /// </summary>
    private void HandleGenerationComplete()
    {
        // Rebuild nav meshes if needed.
        if (insideSurface != null)
            insideSurface.BuildNavMesh();
        if (outsideSurface != null)
            outsideSurface.BuildNavMesh();

        // Procedurally determine spawn areas.
        CalculateSpawnAreas();

        // Spawn the enemies.
        SpawnEnemies();
    }

    /// <summary>
    /// Calculates the spawn area center and size for both inside and outside surfaces.
    /// Uses a simpler approach that samples the NavMesh to find valid areas.
    /// </summary>
    private void CalculateSpawnAreas()
    {
     
        if (insideSurface != null)
        {
            Bounds bounds = CalculateNavMeshBounds(insideSurface);
            insideAreaCenter = bounds.center;
            insideAreaSize = bounds.size;

            if (IsZeroSize(insideAreaSize))
            {
                Debug.LogWarning("EnemySpawner: Computed inside bounds have zero size. Using fallback values.");
                insideAreaCenter = fallbackInsideCenter;
                insideAreaSize = fallbackInsideSize;
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner: insideSurface not assigned. Using fallback values.");
            insideAreaCenter = fallbackInsideCenter;
            insideAreaSize = fallbackInsideSize;
        }

        if (outsideSurface != null)
        {
            Bounds bounds = CalculateNavMeshBounds(outsideSurface);
            outsideAreaCenter = bounds.center;
            outsideAreaSize = bounds.size;

            if (IsZeroSize(outsideAreaSize))
            {
                Debug.LogWarning("EnemySpawner: Computed outside bounds have zero size. Using fallback values.");
                outsideAreaCenter = fallbackOutsideCenter;
                outsideAreaSize = fallbackOutsideSize;
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner: outsideSurface not assigned. Using fallback values.");
            outsideAreaCenter = fallbackOutsideCenter;
            outsideAreaSize = fallbackOutsideSize;
        }
    }

    /// <summary>
    /// Determines if a Vector3 size is (or is nearly) zero.
    /// </summary>
    private bool IsZeroSize(Vector3 size)
    {
        return Mathf.Approximately(size.x, 0f) &&
               Mathf.Approximately(size.y, 0f) &&
               Mathf.Approximately(size.z, 0f);
    }

    /// <summary>
    /// Calculates the bounds of a NavMeshSurface by sampling points on the NavMesh.
    /// </summary>
    private Bounds CalculateNavMeshBounds(NavMeshSurface surface)
    {
        // First, try to get bounds from renderers or colliders in the surface's hierarchy
        Bounds bounds = GetBoundsFromComponents(surface.gameObject);

        if (!IsZeroSize(bounds.size))
        {
            return bounds;
        }

        // If that fails, try to sample the NavMesh directly
        return SampleNavMeshBounds(surface.transform.position);
    }

    /// <summary>
    /// Gets bounds from renderers or colliders in the given GameObject's hierarchy.
    /// </summary>
    private Bounds GetBoundsFromComponents(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        // Try to get bounds from renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (!boundsInitialized)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        // If no renderers, try colliders
        if (!boundsInitialized)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (!boundsInitialized)
                {
                    bounds = collider.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
        }

        return bounds;
    }

    /// <summary>
    /// Samples the NavMesh in a grid pattern around a center point to determine bounds.
    /// </summary>
    private Bounds SampleNavMeshBounds(Vector3 center)
    {
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        // Sample in a grid pattern
        float sampleRadius = 100f; // How far to search from center
        int sampleCount = 10; // Number of samples in each direction
        float step = sampleRadius * 2 / sampleCount;

        for (int x = 0; x < sampleCount; x++)
        {
            for (int z = 0; z < sampleCount; z++)
            {
                Vector3 samplePoint = new Vector3(
                    center.x - sampleRadius + x * step,
                    center.y,
                    center.z - sampleRadius + z * step
                );

                if (NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    if (!boundsInitialized)
                    {
                        bounds = new Bounds(hit.position, Vector3.zero);
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(hit.position);
                    }
                }
            }
        }

        // Add some padding to the bounds
        if (boundsInitialized)
        {
            bounds.Expand(1.0f);
        }
        else
        {
            // If no valid points found, return a default bounds
            bounds = new Bounds(center, Vector3.zero);
        }

        return bounds;
    }

    /// <summary>
    /// Spawns enemies at random valid positions in the procedural spawn areas.
    /// </summary>
    private void SpawnEnemies()
    {
        // Spawn inside enemies.
        for (int i = 0; i < insideEnemyCount; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(insideAreaCenter, insideAreaSize, insideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, insideEnemyPrefabs.Length);
                GameObject enemy = Instantiate(insideEnemyPrefabs[randIdx], spawnPos, Quaternion.identity);
                enemy.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an inside enemy.");
            }
        }

        // Spawn outside enemies.
        for (int i = 0; i < outsideEnemyCount; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(outsideAreaCenter, outsideAreaSize,outsideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, outsideEnemyPrefabs.Length);
                Instantiate(outsideEnemyPrefabs[randIdx], spawnPos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an outside enemy.");
            }
        }
    }

    /// <summary>
    /// Attempts to find a valid world position on the NavMesh within the given bounds.
    /// </summary>
    private Vector3 GetValidSpawnPosition(Vector3 areaCenter, Vector3 areaSize, int area)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(areaCenter.x - areaSize.x * 0.5f, areaCenter.x + areaSize.x * 0.5f),
                areaCenter.y, // Adjust this if your level's height varies.
                Random.Range(areaCenter.z - areaSize.z * 0.5f, areaCenter.z + areaSize.z * 0.5f)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnSampleRadius, area))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }
}