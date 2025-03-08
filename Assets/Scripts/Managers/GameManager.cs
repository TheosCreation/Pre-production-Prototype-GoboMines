using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct GlobalPrefabs
{
    public ParticleSystem hitWallPrefab;
}

public class GameManager : Singleton<GameManager>
{
    public GlobalPrefabs prefabs;
    public UnityEvent onHostEvent;
    public int day = 1;
    public float timeOfDay = 0;
    public bool isDayProgressing = false;
    public bool dummy = true;

    private Generator generator;
    [SerializeField] private DayDisplayText dayDisplay;
    private EnemySpawner enemySpawner;
    private ElevatorManager elevator;
    [SerializeField] private GameObject ambientSource;
    private GameObject ambientTarget;
    public int initialQuota = 180;
    public int dailyQuota = 180;

    protected override void Awake()
    {
        base.Awake();

        generator = FindFirstObjectByType<Generator>();
        elevator = FindFirstObjectByType<ElevatorManager>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();

        ambientTarget = Instantiate(ambientSource);
    }

    public void Update()
    {
        timeOfDay += Time.deltaTime;
    }


    public void StartDay()
    {
        timeOfDay = 0;
        isDayProgressing = true;
        if(dummy)
        {
            generator.OnGenerationComplete += enemySpawner.BakeNavMeshAndSpawnsClientRpc;
            generator.OnGenerationComplete += enemySpawner.SpawnEnemies;
            dummy = false;
        }
        generator.ResetDungeon();
        ambientTarget.SetActive(true);
    }

    public void EndDay()
    {
        isDayProgressing = false;
        day++;
        dayDisplay.UpdateText(day);
        enemySpawner.ResetEnemies();
        ambientTarget.SetActive(true);
        NetworkSpawnHandler.Instance.RespawnConnectedPlayersServerRpc();
    }

    private void CalculateQuota()
    {
        dailyQuota = initialQuota * (int)(0.1f * Mathf.Pow((float)day, 1.3f) + 1f);
    }

    public void ResetLevel()
    {
        elevator.Reset();
        EndDay();
        day = 0;
    }
}
