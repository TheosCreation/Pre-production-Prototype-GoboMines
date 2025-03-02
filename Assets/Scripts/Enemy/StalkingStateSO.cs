using UnityEngine;
using UnityEngine.AI;

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
