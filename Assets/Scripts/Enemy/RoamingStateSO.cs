using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/RoamingState", fileName = "RoamingState")]
public class RoamingStateSO : BaseState
{
    [Header("Roam Settings")]
    [SerializeField]
    public float roamRadius = 10f;
    public BaseState nextState;

    [Header("Area Selection")]
    [SerializeField]
    [NavMeshAreaMask]
    private int allowedAreas = ~0;

    [Header("Path Validation")]
    [SerializeField] private float maxPathLength = 1000;
    [SerializeField] private int maxCornerCount = 10;

    [Header("Target Detection")]
    [SerializeField] private float detectionCheckInterval = 0.5f; // How often to check for targets

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        SetNewRoamingPosition(enemy);

        // Start periodic detection check
        enemy.timer.SetTimer(detectionCheckInterval, () =>
        {
            CheckForTargets(enemy);
            // Restart the timer for continuous checking
         
            enemy.timer.SetTimer(detectionCheckInterval, () => CheckForTargets(enemy));
        });
    }

    public override void OnUpdate(EnemyAI enemy)

    {
        CheckForTargets(enemy);

        if (IsTargetDetected(enemy))
        {
            Debug.Log("changed");
            enemy.ChangeStateByType(nextState.GetType());
            return;
        }
        if (HasReachedDestination(enemy))
        {
            SetNewRoamingPosition(enemy);
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        base.OnExit(enemy);
        enemy.timer.StopTimer();
    }

    private void CheckForTargets(EnemyAI enemy)
    {
        Collider[] colliders = Physics.OverlapSphere(enemy.transform.position, detectionRange);

        Transform closestTarget = null;
        float closestDistance = float.MaxValue;
        // Debug.Log("checking for targets");
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                Debug.Log("targetFound");
                float distance = Vector3.Distance(enemy.transform.position, collider.transform.position);

                if (distance < closestDistance)
                {
                    
                    closestDistance = distance;
                    closestTarget = collider.transform;
                    Debug.Log("target set");
                    
                }
            }
        }

        if (closestTarget != null)
        {
            enemy.SetTarget(closestTarget);
        }
    }

    private void SetNewRoamingPosition(EnemyAI enemy)
    {
        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
            Vector3 newPosition = enemy.GetHomePosition() + randomDirection;

            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1f, allowedAreas))
            {
                currentDestination = hit.position;
                enemy.GetAgent().SetDestination(currentDestination);

                if (enemy.GetAgent().path.status == NavMeshPathStatus.PathComplete)
                {
                    return;
                }
            }

            attempts++;
        }

        Debug.LogWarning("Failed to find valid roaming position after " + maxAttempts + " attempts");
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