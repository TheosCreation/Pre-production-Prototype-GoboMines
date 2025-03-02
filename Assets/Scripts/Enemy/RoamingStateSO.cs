using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
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
    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        SetNewRoamingPosition(enemy);
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        Debug.Log(enemy.GetAgent().path.status);
        if (IsTargetDetected(enemy))
        {
            enemy.ChangeStateByType(nextState.GetType());
            return;
        }
        if (HasReachedDestination(enemy))
        {
            SetNewRoamingPosition(enemy);
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


