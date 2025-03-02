using UnityEngine;

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

    private bool IsTargetValid(EnemyAI enemy) =>
        enemy.GetTarget() != null && enemy.GetTarget().gameObject.activeInHierarchy;

    private bool IsWithinAttackRange(EnemyAI enemy) =>
        enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) <= attackRange;
}
