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

    public void Update()
    {
        timeOfDay += Time.deltaTime;
    }

    public void StartDay()
    {
        timeOfDay = 0;
        isDayProgressing = true;
        // Start Spawning Of Enemies and Timer

    }

    public void EndDay()
    {
        isDayProgressing = false;
        day++;
        // Generator.CreateNewMap
    }
}
