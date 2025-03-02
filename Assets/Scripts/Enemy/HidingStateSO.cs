using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/HidingState", fileName = "HidingState")]
public class HidingStateSO : BaseState
{
    public float hideDistance = 7f;
    public float minSafeDistance = 10f;
    public LayerMask layerMask;
    public int maxSearchAttempts = 5; // using an int for number of attempts

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
        if (enemy.GetTarget() == null)
            return false;

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
