using UnityEngine;

public interface IEnemyState
{
    void OnEnter(EnemyAI enemy);
    void OnUpdate(EnemyAI enemy);
    void OnExit(EnemyAI enemy);
    void ApplySettings(EnemyAI enemy);
}
