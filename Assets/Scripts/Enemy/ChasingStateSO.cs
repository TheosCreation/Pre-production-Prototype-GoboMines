using UnityEngine;
using UnityEngine.AI;

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
