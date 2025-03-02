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

    [Header("Gap Jumping")]
    [SerializeField] private float maxGapDistance = 5f; // Maximum distance the worm can jump across gaps
    [SerializeField] private float jumpHeight = 2f; // Height of the jump arc
    [SerializeField] private float jumpDuration = 1f; // How long the jump takes
    [SerializeField] private LayerMask groundLayers; // Layers considered as valid landing spots

    private float currentRadius;
    private float currentAngle;
    private bool isSpiraling = false;
    private bool isJumping = false;
    private Vector3 jumpTarget;
    private float jumpStartTime;
    private Vector3 jumpStartPosition;
    private Vector3 lastSpiralPosition;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);

        // Configure NavMeshAgent to traverse all areas
        enemy.GetAgent().areaMask = navMeshAreaMask;

        // Initialize spiral parameters
        currentRadius = initialRadius;
        currentAngle = 0f;
        isSpiraling = false;
        isJumping = false;

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

        // Handle jumping state
        if (isJumping)
        {
            UpdateJump(enemy);
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

            // Check if the agent is stuck or can't find a path
            if (agent.pathStatus == NavMeshPathStatus.PathPartial ||
                agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                (agent.pathPending == false && agent.hasPath && agent.remainingDistance > 0.5f && agent.velocity.magnitude < 0.1f))
            {
                TryJumpAcrossGap(enemy, lastSpiralPosition);
            }
        }
        else
        {
            // Move directly toward target until close enough to start spiraling
            agent.SetDestination(target.position);

            // Check if direct path to target is blocked
            if (agent.pathStatus == NavMeshPathStatus.PathPartial ||
                agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                (agent.pathPending == false && agent.hasPath && agent.remainingDistance > 0.5f && agent.velocity.magnitude < 0.1f))
            {
                TryJumpAcrossGap(enemy, target.position);
            }
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
        lastSpiralPosition = targetPosition; // Store for potential gap jumping

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
            lastSpiralPosition = targetPosition; // Update stored position

            if (NavMesh.SamplePosition(targetPosition, out hit, 2.0f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // If still no valid position, try jumping
                TryJumpAcrossGap(enemy, targetPosition);
            }
        }

        // If we've reached the minimum radius, reset to create continuous spiraling
        if (currentRadius <= minRadius + 0.1f)
        {
            currentRadius = initialRadius;
        }
    }

    private void TryJumpAcrossGap(EnemyAI enemy, Vector3 desiredPosition)
    {
        NavMeshAgent agent = enemy.GetAgent();
        Vector3 currentPosition = enemy.transform.position;
        Vector3 desiredDirection = (desiredPosition - currentPosition).normalized;

        // Cast rays in the desired direction to find valid landing spots
        RaycastHit hit;
        if (Physics.SphereCast(currentPosition + Vector3.up * 0.5f, 0.5f, desiredDirection, out hit, maxGapDistance, groundLayers))
        {
            // Check if the hit point is on the NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 1.0f, agent.areaMask))
            {
                // Start the jump
                jumpTarget = navHit.position;
                jumpStartPosition = currentPosition;
                jumpStartTime = Time.time;
                isJumping = true;

                // Disable NavMeshAgent during the jump
                agent.enabled = false;

                Debug.Log("Worm is jumping across a gap to " + jumpTarget);
            }
        }
    }

    private void UpdateJump(EnemyAI enemy)
    {
        float jumpProgress = (Time.time - jumpStartTime) / jumpDuration;

        if (jumpProgress >= 1.0f)
        {
            // Jump completed
            enemy.transform.position = jumpTarget;
            isJumping = false;

            // Re-enable NavMeshAgent
            NavMeshAgent agent = enemy.GetAgent();
            agent.enabled = true;
            agent.Warp(jumpTarget); // Ensure agent is at the correct position

            // Continue spiral movement
            if (enemy.GetTarget() != null)
            {
                // Calculate a new spiral position based on current position relative to target
                Vector3 directionToTarget = enemy.GetTarget().position - enemy.transform.position;
                float distanceToTarget = directionToTarget.magnitude;

                // If we're close enough to spiral
                if (distanceToTarget <= initialRadius)
                {
                    isSpiraling = true;

                    // Calculate current angle based on position
                    Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
                    currentAngle = Mathf.Atan2(flatDirection.z, flatDirection.x);

                    // Set radius based on distance
                    currentRadius = Mathf.Clamp(distanceToTarget, minRadius, initialRadius);
                }
                else
                {
                    // If too far, move directly to target
                    isSpiraling = false;
                    agent.SetDestination(enemy.GetTarget().position);
                }
            }

            return;
        }

        // Calculate jump arc
        Vector3 jumpVector = jumpTarget - jumpStartPosition;
        float horizontalDistance = new Vector3(jumpVector.x, 0, jumpVector.z).magnitude;

        // Current horizontal progress
        float horizontalProgress = jumpProgress;
        Vector3 horizontalPosition = jumpStartPosition + new Vector3(jumpVector.x, 0, jumpVector.z) * horizontalProgress;

        // Calculate height using a parabolic arc
        float height = Mathf.Sin(jumpProgress * Mathf.PI) * jumpHeight;

        // Set the position
        enemy.transform.position = new Vector3(horizontalPosition.x, jumpStartPosition.y + height, horizontalPosition.z);

        // Rotate towards the jump direction
        if (jumpVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(jumpVector.x, 0, jumpVector.z));
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        base.OnExit(enemy);
        isSpiraling = false;

        // Ensure NavMeshAgent is enabled when exiting
        if (isJumping)
        {
            isJumping = false;
            enemy.GetAgent().enabled = true;
        }
    }
}