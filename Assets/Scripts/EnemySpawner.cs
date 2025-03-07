using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemySpawner : MonoBehaviour
{
    [Header("NavMesh Surfaces")]
    public NavMeshSurface insideSurface;
    public NavMeshSurface outsideSurface;
    [SerializeField]
    [NavMeshAreaMask]
    protected int insideArea = 0;
    [SerializeField]
    [NavMeshAreaMask]
    protected int outsideArea = 0;

    [Header("Enemy Prefab Options")]
    public GameObject[] insideEnemyPrefabs;
    public GameObject[] outsideEnemyPrefabs;

    [Header("Spawn Settings")]
    public int insideEnemyCount = 5;
    public int outsideEnemyCount = 5;
    public int maxSpawnAttempts = 10;
    public float spawnSampleRadius = 5f;

    [Header("Fallback Spawn Areas")]
    public Vector3 fallbackInsideCenter = Vector3.zero;
    public Vector3 fallbackInsideSize = new Vector3(10, 0, 10);
    public Vector3 fallbackOutsideCenter = Vector3.zero;
    public Vector3 fallbackOutsideSize = new Vector3(20, 0, 20);

    [Header("Wave Spawn Settings")]
    public float spawnInterval = 10f;

    private Vector3 insideAreaCenter;
    private Vector3 insideAreaSize;
    private Vector3 outsideAreaCenter;
    private Vector3 outsideAreaSize;

    private Generator generator;
    private List<GameObject> enemyList = new List<GameObject>();
   
    public void ResetEnemies()
    {
        foreach (GameObject enemy in enemyList)
        {
            if (enemy == null) { continue; }
            enemy.GetComponent<EnemyAI>().Die();
        }
    }

    public void HandleGenerationComplete()
    {
        if (insideSurface != null)
        { 
            insideSurface.BuildNavMesh(); 
        }
        if (outsideSurface != null)
        { 
            outsideSurface.BuildNavMesh(); 
        }

        CalculateSpawnAreas();

        SpawnEnemies();

        StartCoroutine(SpawnEnemiesOverTime());
    }

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

    private bool IsZeroSize(Vector3 size)
    {
        return Mathf.Approximately(size.x, 0f) &&
               Mathf.Approximately(size.y, 0f) &&
               Mathf.Approximately(size.z, 0f);
    }

    private Bounds CalculateNavMeshBounds(NavMeshSurface surface)
    {
        Bounds bounds = GetBoundsFromComponents(surface.gameObject);

        if (!IsZeroSize(bounds.size))
        {
            return bounds;
        }

        return SampleNavMeshBounds(surface.transform.position);
    }

    private Bounds GetBoundsFromComponents(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

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

    private Bounds SampleNavMeshBounds(Vector3 center)
    {
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        float sampleRadius = 100f; 
        int sampleCount = 10; 
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

        if (boundsInitialized)
        {
            bounds.Expand(1.0f);
        }
        else
        {
            bounds = new Bounds(center, Vector3.zero);
        }

        return bounds;
    }

    private void SpawnEnemies()
    {
        // Spawn inside enemies.
        for (int i = 0; i < insideEnemyCount; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(insideAreaCenter, insideAreaSize, insideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, insideEnemyPrefabs.Length);
                SpawnEnemy(insideEnemyPrefabs[randIdx], spawnPos);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an inside enemy.");
            }
        }

        for (int i = 0; i < outsideEnemyCount; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(outsideAreaCenter, outsideAreaSize, outsideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, outsideEnemyPrefabs.Length);
                SpawnEnemy(outsideEnemyPrefabs[randIdx], spawnPos);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an outside enemy.");
            }
        }
    }


    private IEnumerator SpawnEnemiesOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnWave();
        }
    }


    private void SpawnWave()
    {
        
        int dayMultiplier = GameManager.Instance.day;  

     
        for (int i = 0; i < insideEnemyCount * dayMultiplier; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(insideAreaCenter, insideAreaSize, insideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, insideEnemyPrefabs.Length);
                SpawnEnemy(insideEnemyPrefabs[randIdx], spawnPos);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an inside enemy.");
            }
        }

        // Spawn outside enemies for this wave.
        for (int i = 0; i < outsideEnemyCount * dayMultiplier; i++)
        {
            Vector3 spawnPos = GetValidSpawnPosition(outsideAreaCenter, outsideAreaSize, outsideArea);
            if (spawnPos != Vector3.zero)
            {
                int randIdx = Random.Range(0, outsideEnemyPrefabs.Length);
                SpawnEnemy(outsideEnemyPrefabs[randIdx], spawnPos);
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find a valid spawn position for an outside enemy.");
            }
        }
    }

    public void SpawnEnemy(GameObject prefab, Vector3 spawnPos)
    {
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
        enemyList.Add(enemy);
    }

    private Vector3 GetValidSpawnPosition(Vector3 areaCenter, Vector3 areaSize, int area)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(areaCenter.x - areaSize.x * 0.5f, areaCenter.x + areaSize.x * 0.5f),
                areaCenter.y, 
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
