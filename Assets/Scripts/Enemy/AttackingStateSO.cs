using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/AttackingState", fileName = "AttackingState")]
public class AttackingStateSO : BaseState
{
    private float nextAttackTime;
    [SerializeField] private float slowDownDuration = 1f;
    [SerializeField] private float slowDownSpeed = 2f;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        nextAttackTime = Time.time + attackCooldown;
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (!IsTargetValid(enemy))
        {
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        NavMeshAgent agent = enemy.GetAgent();
        agent.SetDestination(enemy.GetTarget().position);

        if (IsWithinAttackRange(enemy))
        {
            agent.isStopped = true;
            if (Time.time >= nextAttackTime)
            {
                enemy.PerformAttack();
                nextAttackTime = Time.time + attackCooldown;

                StartSlowDown(enemy);
            }
        }
        else
        {
            agent.isStopped = false;
            enemy.SetMoveSpeed(moveSpeed);
        }

        if (!IsTargetInRange(enemy))
        {
            enemy.ChangeState<RoamingStateSO>();
        }
    }

    private bool IsTargetValid(EnemyAI enemy) =>
        enemy.GetTarget() != null && enemy.GetTarget().gameObject.activeInHierarchy;

    private bool IsWithinAttackRange(EnemyAI enemy) =>
        enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) <= attackRange;

    private bool IsTargetInRange(EnemyAI enemy) =>
        enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) <= detectionRange;

    private void StartSlowDown(EnemyAI enemy)
    {
        enemy.SetMoveSpeed(slowDownSpeed);
        enemy.timer.SetTimer(slowDownDuration, () => enemy.SetMoveSpeed(moveSpeed));
    }
}