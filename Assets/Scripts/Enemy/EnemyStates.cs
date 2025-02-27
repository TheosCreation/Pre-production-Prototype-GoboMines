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
    // Roam around untill a player gets in its radius (you can use a trigger collider if you want) it sets that player as a target and begins stalking it
    // follow around not getting too close 
    // if player looks in its direction, it will roll a die then depending on result it will either run after the player or hide, make it more likely to hide and keep stalking
    // make hiding and stalking different states
    // then just make it the chase state when it rolls to run after the player
    // hiding should hide untill its not in player sight
    // you can shoot raycasts in like and if none of those hit a player then youre hidden, then to stalk just make sure at least one is hitting but its like not moving towards the player
    // make it move towards the player untill the raycast hits, then stop moving (can prolly find a better solution)
    


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
        if (isHiding)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                isHiding = false;
            }
            return; 
        }

        if (!HasLineOfSight(enemy))
        {
            MoveToCover(enemy);
        }
        else
        {
            enemy.transform.LookAt(enemy.GetTarget().position);

            if (IsPlayerLookingAtMe(enemy))
            {
                isHiding = true;
                hideTimer = hidingDuration;
            }
            else
            {
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


        if (Physics.Raycast(ray, out hitInfo, lineOfSightDistance))
        {           
            if (hitInfo.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPlayerLookingAtMe(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null)
        {
            return false;
        }
        Vector3 toEnemy = (enemy.transform.position - target.position).normalized;
        float dot = Vector3.Dot(target.forward, toEnemy);
        return dot > playerViewThreshold;
    }

    private void ChaseTarget(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        if (agent.pathPending)
        {
            return;
        }
        agent.SetDestination(enemy.GetTarget().position);
    }

    private void MoveToCover(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();
        Transform target = enemy.GetTarget();
        if (target == null)
        {
            return;
        }

        Vector3 awayDirection = (enemy.transform.position - target.position).normalized;
        float retreatDistance = 10f;
        Vector3 coverPosition = enemy.transform.position + awayDirection * retreatDistance;

        if (NavMesh.SamplePosition(coverPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
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

