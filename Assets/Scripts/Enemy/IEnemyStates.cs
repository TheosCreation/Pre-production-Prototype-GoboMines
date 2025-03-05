using System;
using UnityEngine;

[Serializable]
public class IEnemyState : ScriptableObject
{
    public virtual void OnEnter(EnemyAI enemy) { }
    public virtual void OnUpdate(EnemyAI enemy) { }
    public virtual void OnExit(EnemyAI enemy) { }
    public virtual void ApplySettings(EnemyAI enemy) { }
}
