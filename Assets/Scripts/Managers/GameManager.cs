using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct GlobalPrefabs
{
    public ParticleSystem hitWallPrefab;
}

public class GameManager : SingletonPersistent<GameManager>
{
    public GlobalPrefabs prefabs;
    public UnityEvent onHostEvent;
    public int day = 1;
    public float timeOfDay = 0;
    public bool isDayProgressing = false;

    private Generator generator;
    private EnemySpawner enemySpawner;

    protected override void Awake()
    {
        base.Awake();

        generator = FindFirstObjectByType<Generator>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    public void Update()
    {
        timeOfDay += Time.deltaTime;
    }


    public void StartDay()
    {
        timeOfDay = 0;
        isDayProgressing = true;
        enemySpawner.HandleGenerationComplete();
    }

    public void EndDay()
    {
        isDayProgressing = false;
        day++;
        generator.ResetDungeon();
        enemySpawner.ResetEnemies();
    }
}
