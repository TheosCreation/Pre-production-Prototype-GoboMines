using UnityEngine;
using UnityEngine.AI;

public abstract class BaseState : ScriptableObject, IEnemyState
{
    [Header("Movement Settings")]
  
    public float moveSpeed = 5f;
    public float rotationSpeed = 500f;

    [Header("Combat Settings")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    [Header("Destination")]
    public Vector3 currentDestination;
    [SerializeField]
    [NavMeshAreaMask]

    protected int allowedAreas = 0;
    public virtual void OnEnter(EnemyAI enemy)
    {
        enemy.GetAgent().areaMask = allowedAreas;
        ApplySettings(enemy);
    }

    public virtual void OnUpdate(EnemyAI enemy)
    {
        // Set a timer to find a new target if needed.
        if (!enemy.timer.IsRunning)
        {
            enemy.timer.SetTimer(30f, FindTarget, enemy);
        }
    }

    public virtual void OnExit(EnemyAI enemy) { }

    public virtual void ApplySettings(EnemyAI enemy)
    {
    
        enemy.SetMoveSpeed(moveSpeed);
        enemy.SetRotationSpeed(rotationSpeed);
        enemy.SetDetectionRange(detectionRange);
        enemy.SetAttackRange(attackRange);
        enemy.SetAttackCooldown(attackCooldown);
    }

    private void FindTarget(EnemyAI enemy)
    {
        PlayerController closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (PlayerController player in NetworkSpawnHandler.Instance.playersConnected)
        {
            float dist = Vector3.Distance(enemy.transform.position, player.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPlayer = player;
            }
        }
        if (closestPlayer != null)
        {
            enemy.SetTarget(closestPlayer.transform);
        }
    }
}
