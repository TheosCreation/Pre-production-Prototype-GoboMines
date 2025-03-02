using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/SpiralChasingState", fileName = "SpiralChasingState")]
public class SpiralChasingStateSO : BaseState
{
    [Header("Spiral Settings")]
    [SerializeField] private float initialRadius = 10f;
    [SerializeField] private float radiusDecreaseRate = 0.5f;
    [SerializeField] private float minRadius = 3f;
    [SerializeField] private float spiralSpeed = 2f;
    [SerializeField] private float heightOffset = 0f;

    [Header("NavMesh Settings")]
    [SerializeField] private int navMeshAreaMask = -1; 

    private float currentRadius;
    private float currentAngle;
    private bool isSpiraling = false;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);

        // Configure NavMeshAgent to traverse all areas
        enemy.GetAgent().areaMask = navMeshAreaMask;

        // Initialize spiral parameters
        currentRadius = initialRadius;
        currentAngle = 0f;
        isSpiraling = false;

        // Set initial destination to move toward target
        if (enemy.GetTarget() != null)
        {
            enemy.GetAgent().SetDestination(enemy.GetTarget().position);
        }
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        base.OnUpdate(enemy);

        NavMeshAgent agent = enemy.GetAgent();
        Transform target = enemy.GetTarget();

        if (target == null)
        {
            // If no target, return to roaming
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        float distanceToTarget = Vector3.Distance(enemy.transform.position, target.position);

        // Start spiraling when close enough to target
        if (!isSpiraling && distanceToTarget <= initialRadius)
        {
            isSpiraling = true;
        }

        if (isSpiraling)
        {
            // Calculate next position in spiral
            UpdateSpiralMovement(enemy, target);
        }
        else
        {
            // Move directly toward target until close enough to start spiraling
            agent.SetDestination(target.position);
        }

        // If target moves too far away, reset spiraling
        if (isSpiraling && distanceToTarget > initialRadius * 1.5f)
        {
            isSpiraling = false;
            agent.SetDestination(target.position);
        }
    }

    private void UpdateSpiralMovement(EnemyAI enemy, Transform target)
    {
        NavMeshAgent agent = enemy.GetAgent();

        // Update angle based on speed
        currentAngle += spiralSpeed * Time.deltaTime;

        // Gradually decrease radius
        currentRadius = Mathf.Max(minRadius, currentRadius - (radiusDecreaseRate * Time.deltaTime));

        // Calculate position on spiral
        Vector3 offset = new Vector3(
            Mathf.Cos(currentAngle) * currentRadius,
            heightOffset,
            Mathf.Sin(currentAngle) * currentRadius
        );

        Vector3 targetPosition = target.position + offset;

        // Check if the position is on a valid NavMesh
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2.0f, agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If no valid position found, try a larger radius
            currentRadius += radiusDecreaseRate * 2f;

            // Recalculate with new radius
            offset = new Vector3(
                Mathf.Cos(currentAngle) * currentRadius,
                heightOffset,
                Mathf.Sin(currentAngle) * currentRadius
            );

            targetPosition = target.position + offset;

            if (NavMesh.SamplePosition(targetPosition, out hit, 2.0f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
            }
        }

        // If we've reached the minimum radius, reset to create continuous spiraling
        if (currentRadius <= minRadius + 0.1f)
        {
            currentRadius = initialRadius;
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        base.OnExit(enemy);
        isSpiraling = false;
    }
}