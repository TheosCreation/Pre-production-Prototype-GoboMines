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
    [SerializeField] private GameObject ambientSource;
    private GameObject ambientTarget;
    public int initialQuota = 180;
    public int dailyQuota = 180;

    protected override void Awake()
    {
        base.Awake();

        generator = FindFirstObjectByType<Generator>();
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
        enemySpawner.HandleGenerationComplete();
        ambientTarget.SetActive(true);
    }

    public void EndDay()
    {
        isDayProgressing = false;
        day++;
        generator.ResetDungeon();
        enemySpawner.ResetEnemies();
        ambientTarget.SetActive(true);
    }

    private void CalculateQuota()
    {
        dailyQuota = initialQuota * (int)(0.1f * Mathf.Pow((float)day, 1.3f) + 1f);
    }

}
