using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/StalkingState", fileName = "StalkingState")]
public class StalkingStateSO : BaseState
{
    public float lineOfSightDistance = 15f;
    public float playerViewThreshold = 0.8f;
    public bool enableDebugging = true;
    public LayerMask raycastLayerMask;
    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        enemy.SetTarget(FindNearestPlayer(enemy));
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) < attackRange)
        {
            enemy.ChangeState<AttackingStateSO>(); 
            return;
        }

        if (IsPlayerLookingAtMe(enemy))
        {
            enemy.ChangeState<HidingStateSO>();
        }
        else 
        {
            enemy.GetAgent().SetDestination(enemy.GetTarget().position);
        }
        if (enableDebugging)
        {
            Vector3 direction = (enemy.GetTarget().position - enemy.transform.position).normalized;
            Debug.DrawRay(enemy.transform.position, direction * lineOfSightDistance, HasDirectLineOfSight(enemy) ? Color.green : Color.red);
        }
    }



    private bool IsPlayerLookingAtMe(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        if (target == null) return false;

        Vector3 toEnemy = (enemy.transform.position - target.position).normalized;
        if (enableDebugging)
        {
            Debug.DrawRay(target.position, target.forward * 5f, Color.blue);
        }
        return (Vector3.Dot(target.forward, toEnemy) > playerViewThreshold) && HasDirectLineOfSight(enemy);
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

    private bool HasDirectLineOfSight(EnemyAI enemy)
    {
        Transform player = enemy.GetTarget();
        if (player == null)
            return false;

        Vector3 direction = (enemy.transform.position - player.position).normalized;

        if (Physics.Raycast(player.position, direction, out RaycastHit hit, lineOfSightDistance, ~0))
        {
            return hit.transform == enemy.transform;
        }

        return false;
    }
}
