using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/HidingState", fileName = "HidingState")]
public class HidingStateSO : BaseState
{
    public float farHideDistance = 20f;
    public float minSafeDistance = 10f;   
    public LayerMask layerMask;
    public int maxSearchAttempts = 3;     
    public float sampleRadius = 5f;      

    private Vector3 hidePosition;
    private bool isHiding = false;
    private EnemyAI enemyRef;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        enemyRef = enemy;

        hidePosition = FindOppositeHidingSpot(enemy);
        if (hidePosition != Vector3.zero)
        {
            enemy.GetAgent().SetDestination(hidePosition);
            isHiding = true;
        }
        else
        {
            Debug.Log("No valid far hiding spot found, switching to roaming.");
            enemy.ChangeState<RoamingStateSO>();
        }
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (Vector3.Distance(enemy.transform.position, hidePosition) < enemy.GetAgent().stoppingDistance + 0.5f)
        {
            enemy.ChangeState<RoamingStateSO>();
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        isHiding = false;
    }

    private Vector3 FindOppositeHidingSpot(EnemyAI enemy)
    {
        if (enemy.GetTarget() == null)
            return Vector3.zero;

        Vector3 oppositeDirection = (enemy.transform.position - enemy.GetTarget().position).normalized;
        Vector3 idealSpot = enemy.transform.position + oppositeDirection * farHideDistance;

        for (int i = 0; i < maxSearchAttempts; i++)
        {
            Vector3 variation = Random.insideUnitSphere * 0.5f;
            variation.y = 0; 
            Vector3 candidate = idealSpot + variation;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, allowedAreas))
            {
                return hit.position;
            }
        }

        if (NavMesh.SamplePosition(idealSpot, out NavMeshHit fallbackHit, sampleRadius, allowedAreas))
        {
            return fallbackHit.position;
        }

        return Vector3.zero;
    }
}
