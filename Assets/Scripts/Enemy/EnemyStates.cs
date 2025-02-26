    using UnityEngine.AI;
    using UnityEngine;
using System.Diagnostics.Contracts;


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
    public float rotationSpeed = 5f;

    [Header("Combat Settings")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    public virtual void OnEnter(EnemyAI enemy)
    {
        ApplySettings(enemy);
    }

    public virtual void OnUpdate(EnemyAI enemy) { }

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
}

[CreateAssetMenu(menuName = "EnemyStates/RoamingState", fileName = "RoamingState")]
public class RoamingStateSO : BaseState
{
    // This field is not serialized between play sessions.
    private Vector3 currentDestination;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        CalculateNewRoamingPosition(enemy);
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
            CalculateNewRoamingPosition(enemy);
        }
    }

    private void CalculateNewRoamingPosition(EnemyAI enemy)
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
        if (target != null)
        {
            // Simple distance check for detection.
            return Vector3.Distance(enemy.transform.position, target.position) <= detectionRange;
        }
        return false;
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
            // Target lost; switch back to roaming.
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        if (IsWithinAttackRange(enemy))
        {
            // Target within attack range; switch to attacking.
            enemy.ChangeState<AttackingStateSO>();
            return;
        }

        UpdateChasePath(enemy);
    }

    private void UpdateChasePath(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        Transform target = enemy.GetTarget();
        if (target == null || agent.pathPending)
            return;
        agent.SetDestination(target.position);
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
public class StalkingState : BaseState
{
    public float lineOfSightDistance = 15f;
    public float hidingDuration = 2f;
    public float playerViewThreshold = 0.8f;

    private bool isHiding = false;
    private float hideTimer = 0f;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);

        enemy.SetTarget(FindNearestPlayer(enemy));
    }  
        

    public override void OnUpdate(EnemyAI enemy)
    {
        // Only run cover logic if we aren’t aleady in hiding behavior.
        if (isHiding)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                // After waiting, decide to chase or resume stalking.
                isHiding = false;
            }
            return; // Stay in cover (or not update movement)
        }

        // Check the line of sight with a raycast.
        if (!HasLineOfSight(enemy))
        {
            // If the direct ray is blocked then move to cover or back away.
            MoveToCover(enemy);
        }
        else
        {
            // If we have LoS, look at the player.
            enemy.transform.LookAt(enemy.GetTarget().position);

            // Optionally, check if the target “sees” the enemy.
            if (IsPlayerLookingAtMe(enemy))
            {
                // Once spotted and the player is looking, hide for a bit before proceeding.
                isHiding = true;
                hideTimer = hidingDuration;
                // Optionally, set a cover point manually if you have cover presets.
            }
            else
            {
                // If no cover is needed, follow the player.
                ChaseTarget(enemy);
            }
        }
    }
    private bool HasLineOfSight(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null) return false;

        Vector3 direction = (target.position - enemy.transform.position).normalized;
        Ray ray = new Ray(enemy.transform.position, direction);
        RaycastHit hitInfo;

        // Consider all layers and use your detection range.
        if (Physics.Raycast(ray, out hitInfo, lineOfSightDistance))
        {
            // If the hit object is the player, then LoS exists.
            // Otherwise, something (like a wall) is blocking the view.
            if (hitInfo.collider.CompareTag("Player"))
                return true;
        }
        return false;
    }

    private bool IsPlayerLookingAtMe(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null) return false;

        // For example, if the player has a main camera as a child, or if the player forward is the view.
        Vector3 toEnemy = (enemy.transform.position - target.position).normalized;
        // Here we assume the player's forward is the direction they’re looking.
        // A dot product near 1 means the enemy is almost directly in front.
        float dot = Vector3.Dot(target.forward, toEnemy);
        return dot > playerViewThreshold;
    }

    private void ChaseTarget(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        if (agent.pathPending)
            return;
        // Follow the player's current position.
        agent.SetDestination(enemy.GetTarget().position);
    }

    private void MoveToCover(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        Transform target = enemy.GetTarget();
        if (target == null)
            return;

        // Basic logic: move in the opposite direction of the target.
        Vector3 awayDirection = (enemy.transform.position - target.position).normalized;
        // Multiply by some distance – you might choose a max retreat distance.
        float retreatDistance = 10f;
        Vector3 coverPosition = enemy.transform.position + awayDirection * retreatDistance;

        // Use NavMesh.SamplePosition to find a valid point on the NavMesh.
        if (NavMesh.SamplePosition(coverPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Finds the nearest player from the enemy's position.
    /// This can be optimized by caching references to players.
    /// </summary>
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
            if (!IsTargetValid(enemy))
            {
                enemy.ChangeState<RoamingStateSO>();
            }
            else
            {
                enemy.ChangeState<ChasingStateSO>();
            }
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            enemy.PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
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

