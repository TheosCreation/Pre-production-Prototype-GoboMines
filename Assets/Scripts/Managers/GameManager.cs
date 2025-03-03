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
}
