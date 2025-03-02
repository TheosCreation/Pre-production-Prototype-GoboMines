    using UnityEngine.AI;
    using UnityEngine;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using static UnityEngine.GraphicsBuffer;

public interface IEnemyState
{
    void OnEnter(EnemyAI enemy);
    void OnUpdate(EnemyAI enemy);
    void OnExit(EnemyAI enemy);
    void ApplySettings(EnemyAI enemy);
}

public abstract class BaseState : ScriptableObject, IEnemyState
{
    [Header("Movement Settings")]
    public float roamRadius = 10f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 500f;

    [Header("Combat Settings")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    public virtual void OnEnter(EnemyAI enemy)
    {
        ApplySettings(enemy);
    }

    public virtual void OnUpdate(EnemyAI enemy)
    {
        if (!enemy.timer.IsRunning)
        {
            enemy.timer.SetTimer(30f, FindTarget, enemy);
        }
    }

    public virtual void OnExit(EnemyAI enemy) { }

    public virtual void ApplySettings(EnemyAI enemy)
    {
        enemy.SetRoamRadius(roamRadius);
        enemy.SetMoveSpeed(moveSpeed);
        enemy.SetRotationSpeed(rotationSpeed);
        enemy.SetDetectionRange(detectionRange);
        enemy.SetAttackRange(attackRange);
        enemy.SetAttackCooldown(attackCooldown);
    }

    private void FindTarget(EnemyAI enemy)
    {
        PlayerController closestPlayer = null;
        float closestDistance = 99999999f;
        foreach (PlayerController player in NetworkSpawnHandler.Instance.playersConnected)
        {
            if (Vector3.Distance(enemy.transform.position, player.transform.position) < closestDistance)
            {
                closestPlayer = player;
                closestDistance = Vector3.Distance(enemy.transform.position, player.transform.position);
            }
        }
        enemy.SetTarget(closestPlayer.transform);
    }
}

[CreateAssetMenu(menuName = "EnemyStates/RoamingState", fileName = "RoamingState")]
public class RoamingStateSO : BaseState
{
    private Vector3 currentDestination;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        SetNewRoamingPosition(enemy);
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (IsTargetDetected(enemy))
        {
            enemy.ChangeState<ChasingStateSO>();
            return;
        }

        if (HasReachedDestination(enemy))
        {
            SetNewRoamingPosition(enemy);
        }
    }

    private void SetNewRoamingPosition(EnemyAI enemy)
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        Vector3 newPosition = enemy.GetHomePosition() + randomDirection;

        if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            enemy.GetAgent().SetDestination(currentDestination);
        }
    }

    private bool HasReachedDestination(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        return !agent.pathPending && agent.remainingDistance < 0.1f;
    }

    private bool IsTargetDetected(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        return target != null && Vector3.Distance(enemy.transform.position, target.position) <= detectionRange;
    }


}

[CreateAssetMenu(menuName = "EnemyStates/HidingState", fileName = "HidingState")]
public class HidingStateSO : BaseState
{
    public float hideDistance = 7f;
    public float minSafeDistance = 10f;
    public LayerMask layerMask;
    public float maxSearchAttempts = 5;

    private Vector3 hidePosition;
    private bool isHiding = false;
    private EnemyAI enemyRef;

    public override void OnEnter(EnemyAI enemy)
    {
        Debug.Log("Entering Hiding State");
        enemyRef = enemy;

        if (IsInPlayerSight(enemy))
        {
            hidePosition = FindHidingSpot(enemy);

            if (hidePosition != Vector3.zero)
            {
                isHiding = true;
                enemy.GetAgent().SetDestination(hidePosition);
            }
            else
            {
                Debug.Log("No valid hiding spot found, roaming instead.");
                isHiding = false;
                enemy.ChangeState<RoamingStateSO>();
            }
        }
        else
        {
            isHiding = false;
            enemy.ChangeState<RoamingStateSO>();
        }
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (!isHiding) return;

        if (Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) >= minSafeDistance)
        {
            if (!IsInPlayerSight(enemy))
            {
                Debug.Log("Successfully hidden, switching to stalking.");
                enemy.ChangeState<RoamingStateSO>();
            }
            else
            {
                Debug.Log("Still in sight, finding a new hiding spot.");
                hidePosition = FindHidingSpot(enemy);
                if (hidePosition != Vector3.zero)
                {
                    enemy.GetAgent().SetDestination(hidePosition);
                }
            }
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        Debug.Log("Exiting Hiding State");
    }

    private bool IsInPlayerSight(EnemyAI enemy)
    {
        Vector3 directionToPlayer = (enemy.GetTarget().position - enemy.transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.GetTarget().position);

        if (Physics.Raycast(enemy.transform.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, layerMask))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    private Vector3 FindHidingSpot(EnemyAI enemy)
    {
        Vector3 bestHidingSpot = Vector3.zero;
        float bestDistance = 0f;

        for (int i = 0; i < maxSearchAttempts; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * hideDistance;
            randomDirection.y = 0;
            Vector3 potentialHidingSpot = enemy.transform.position + randomDirection;

            if (NavMesh.SamplePosition(potentialHidingSpot, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (!IsInPlayerSight(enemy))
                {
                    float distToPlayer = Vector3.Distance(hit.position, enemy.GetTarget().position);
                    if (distToPlayer > bestDistance)
                    {
                        bestHidingSpot = hit.position;
                        bestDistance = distToPlayer;
                    }
                }
            }
        }

        return bestHidingSpot;
    }
}




[CreateAssetMenu(menuName = "EnemyStates/ChasingState", fileName = "ChasingState")]
public class ChasingStateSO : BaseState
{
    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        UpdateChasePath(enemy);
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (!IsTargetValid(enemy))
        {
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        if (IsWithinAttackRange(enemy))
        {
            enemy.ChangeState<AttackingStateSO>();
            return;
        }

        UpdateChasePath(enemy);
    }

    private void UpdateChasePath(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        Transform target = enemy.GetTarget();
        if (target != null && !agent.pathPending)
        {
            agent.SetDestination(target.position);
        }
    }

    private bool IsTargetValid(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        return target != null && target.gameObject.activeInHierarchy;
    }

    private bool IsWithinAttackRange(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        return target != null && Vector3.Distance(enemy.transform.position, target.position) <= attackRange;
    }
}

[CreateAssetMenu(menuName = "EnemyStates/StalkingState", fileName = "StalkingState")]
public class StalkingStateSO : BaseState
{
    public float lineOfSightDistance = 15f;
    public float playerViewThreshold = 0.8f;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        enemy.SetTarget(FindNearestPlayer(enemy));
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (!HasLineOfSight(enemy))
        {
            enemy.GetAgent().SetDestination(enemy.GetTarget().position);
        }
        else if (IsPlayerLookingAtMe(enemy))
        {
            enemy.ChangeState<HidingStateSO>();
        }
    }

    private bool HasLineOfSight(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null) return false;

        Vector3 direction = (target.position - enemy.transform.position).normalized;
        if (Physics.Raycast(enemy.transform.position, direction, out RaycastHit hit, lineOfSightDistance))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    private bool IsPlayerLookingAtMe(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null) return false;

        Vector3 toEnemy = (enemy.transform.position - target.position).normalized;
        return Vector3.Dot(target.forward, toEnemy) > playerViewThreshold;
    }

    private Transform FindNearestPlayer(EnemyAI enemy)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(enemy.transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
        }
        return nearest;
    }
}


[CreateAssetMenu(menuName = "EnemyStates/AttackingState", fileName = "AttackingState")]
public class AttackingStateSO : BaseState
{
    private float nextAttackTime;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        nextAttackTime = Time.time + attackCooldown;
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (!IsTargetValid(enemy) || !IsWithinAttackRange(enemy))
        {
            if (IsTargetValid(enemy))
            {
                enemy.ChangeState<ChasingStateSO>();
            }
            else
            {
                enemy.ChangeState<RoamingStateSO>();
            }

            return;
        }

        if (Time.time >= nextAttackTime)
        {
            enemy.PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private bool IsTargetValid(EnemyAI enemy) => enemy.GetTarget() != null && enemy.GetTarget().gameObject.activeInHierarchy;
    private bool IsWithinAttackRange(EnemyAI enemy) => enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) <= attackRange;
}
