using UnityEngine;
using UnityEngine.AI;

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
