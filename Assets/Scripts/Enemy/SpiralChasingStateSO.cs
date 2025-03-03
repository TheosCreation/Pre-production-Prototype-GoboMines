using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/SpiralChasingState", fileName = "SpiralChasingState")]
public class SpiralChasingStateSO : BaseState
{
    [Header("Chase Settings")]
    [SerializeField] private float updatePathInterval = 0.2f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float maxRandomDistance = 10f; // Maximum distance for random position

    [Header("Obstacle Detection")]
    [SerializeField] private float raycastDistance = 2f;
    [SerializeField] private float slowdownFactor = 0.5f;
    [SerializeField] private float speedRecoveryRate = 2f;

    [Header("Area Selection")]
    [SerializeField]
    [NavMeshAreaMask]
    private int Outside = 0;
    [SerializeField]
    [NavMeshAreaMask]
    private int both = 0;


    private float originalMoveSpeed;
    private bool isSlowedDown = false;
    private float currentSlowdownTime = 0f;
    private float pathUpdateTimer = 0f;


    private bool destinationReached = false;
    private float destinationCheckTimer = 0.5f;

    public override void OnEnter(EnemyAI enemy)
    {
        allowedAreas = Outside;
        base.OnEnter(enemy);
        originalMoveSpeed = moveSpeed;
        isSlowedDown = false;
        currentSlowdownTime = 0f;
        pathUpdateTimer = 0f;
        destinationReached = false;
        enemy.GetAgent().areaMask = allowedAreas;
        enemy.GetAgent().stoppingDistance = stoppingDistance;

        // Set initial path to target
        FindNewDestination(enemy);
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        base.OnUpdate(enemy);

        Transform target = enemy.GetTarget();
        if (target == null)
        {
            // No target, return to roaming
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        // Check if we've reached the destination
        CheckIfDestinationReached(enemy);

        // Update path to target periodically only if we're actively chasing
        if (!destinationReached)
        {
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0)
            {
                pathUpdateTimer = updatePathInterval;

                // Only update path if we haven't reached our destination
                if (!destinationReached && !enemy.GetAgent().pathPending)
                {
                    // Check if the path is invalid or if we're stuck
                    if (!enemy.GetAgent().hasPath || enemy.GetAgent().pathStatus == NavMeshPathStatus.PathInvalid)
                    {
                        FindNewDestination(enemy);
                    }
                }
            }
        }

        // Check for obstacles with raycast
        CheckForObstacles(enemy);

        // Handle speed recovery if slowed down
        if (isSlowedDown)
        {
            currentSlowdownTime += Time.deltaTime;
            if (currentSlowdownTime >= 1f)
            {
                // Gradually recover speed
                float newSpeed = Mathf.MoveTowards(enemy.GetAgent().speed, originalMoveSpeed,
                    speedRecoveryRate * Time.deltaTime);
                enemy.SetMoveSpeed(newSpeed);

                // If fully recovered, reset slowdown state
                if (Mathf.Approximately(newSpeed, originalMoveSpeed))
                {
                    isSlowedDown = false;
                }
            }
        }

        // If we're close enough to the target, perform attack
        float distanceToTarget = Vector3.Distance(enemy.transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            enemy.PerformAttack();
        }
    }

    private void CheckIfDestinationReached(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetAgent();

        // Check if we've reached our destination
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f)
                {
                    if (!destinationReached)
                    {
                        destinationReached = true;

                        // Switch to "both" areas when destination is reached
                        allowedAreas = both;
                        agent.areaMask = allowedAreas;

                        // Check ground layer after a short delay
                        enemy.StartCoroutine(CheckGroundLayerAfterDelay(enemy, 0.5f));
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator CheckGroundLayerAfterDelay(EnemyAI enemy, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Raycast downward to check ground layer
        if (Physics.Raycast(enemy.transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            int defaultLayer = 0; // Default layer in Unity is 0
            int outsideLayer = LayerMask.NameToLayer("Outside");

            // First check if we're on the default layer
            if (hit.collider.gameObject.layer == defaultLayer)
            {
                // We're on the default layer, keep the "both" mask
                allowedAreas = both;
                enemy.GetAgent().areaMask = allowedAreas;
            }
            // Only if we're not on default layer, check if we're on Outside layer
            else if (hit.collider.gameObject.layer == outsideLayer)
            {
                // We're on the Outside layer, switch back to Outside mask
                allowedAreas = Outside;
                enemy.GetAgent().areaMask = allowedAreas;
            }
        }

        // Find a new destination regardless of the layer result
        destinationReached = false;
        FindNewDestination(enemy);
    }

    private void FindNewDestination(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        NavMeshAgent agent = enemy.GetAgent();

        // Try to get a path to the player first
        if (target != null)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(target.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                currentDestination = target.position;
                agent.SetDestination(currentDestination);
                return;
            }
        }

        // If we can't reach the player, find a random valid position
        NavMeshHit hit;
        Vector3 randomDirection = Random.insideUnitSphere * maxRandomDistance;
        randomDirection += enemy.transform.position;

        if (NavMesh.SamplePosition(randomDirection, out hit, maxRandomDistance, agent.areaMask))
        {
            currentDestination = hit.position;
            agent.SetDestination(currentDestination);
        }
    }

    private void CheckForObstacles(EnemyAI enemy)
    {
        // Cast a ray forward to detect obstacles
        if (Physics.Raycast(enemy.transform.position, enemy.transform.forward, out RaycastHit hit, raycastDistance))
        {
            if (hit.collider.gameObject.CompareTag("Untagged"))
            {
                // Slow down the enemy
                if (!isSlowedDown)
                {
                    originalMoveSpeed = enemy.GetAgent().speed;
                    enemy.SetMoveSpeed(originalMoveSpeed * slowdownFactor);
                    isSlowedDown = true;
                    currentSlowdownTime = 0f;
                }
            }
        }
    }

    public override void OnExit(EnemyAI enemy)
    {
        base.OnExit(enemy);

        // Reset speed to original when exiting state
        enemy.SetMoveSpeed(originalMoveSpeed);
    }
}