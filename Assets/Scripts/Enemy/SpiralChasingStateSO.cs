using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/SpiralChasingState", fileName = "SpiralChasingState")]
public class SpiralChasingStateSO : BaseState
{
    [Header("Chase Settings")]
    [SerializeField] private float updatePathInterval = 0.2f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float maxChaseRadius = 10f;
    [SerializeField] private float spiralValue = 10f; 
    [Header("NavMesh Area Costs")]
    [SerializeField, NavMeshAreaMask]
    private int outsideArea = 0; 
    [SerializeField, NavMeshAreaMask]
    private int insideArea = 1;  
    [SerializeField] private float outsideAreaCost = 1f;  
    [SerializeField] private float insideAreaCost = 10f;  

    [Header("Transition Effects")]
    [SerializeField] private float transitionSlowdownFactor = 0.5f;
    [SerializeField] private float transitionSlowdownDuration = 1f; 
    [SerializeField] private ParticleSystem transitionParticleEffect; 

    private float pathUpdateTimer = 0f;
    private float spiralAngle = 0f;
    private bool transitionTriggered = false;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        NavMeshAgent agent = enemy.GetAgent();
        agent.stoppingDistance = stoppingDistance;

        agent.SetAreaCost(outsideArea, outsideAreaCost);
        agent.SetAreaCost(insideArea, insideAreaCost);

        spiralAngle = 0f;
        pathUpdateTimer = 0f;
        transitionTriggered = false;

        FindNewDestination(enemy);
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        base.OnUpdate(enemy);

        Transform target = enemy.GetTarget();
        if (target == null)
        {
            enemy.ChangeState<RoamingStateSO>();
            return;
        }

        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = updatePathInterval;

            if (!enemy.GetAgent().pathPending &&
                enemy.GetAgent().remainingDistance <= enemy.GetAgent().stoppingDistance)
            {
                FindNewDestination(enemy);
            }
        }

        float distanceToTarget = Vector3.Distance(enemy.transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            enemy.PerformAttack();
        }

        // Safeguard: if the agent appears stuck, reset its path.
        if (enemy.GetAgent().velocity.sqrMagnitude < 0.01f && enemy.GetAgent().hasPath)
        {
            enemy.GetAgent().ResetPath();
            FindNewDestination(enemy);
        }
    }

    private void FindNewDestination(EnemyAI enemy)
    {
        Transform target = enemy.GetTarget();
        NavMeshAgent agent = enemy.GetAgent();

        if (target != null)
        {
            // Calculate a spiral offset around the target.
            float radius = Mathf.Clamp(Vector3.Distance(enemy.transform.position, target.position),
                                         stoppingDistance, maxChaseRadius);
            Vector3 offset = new Vector3(Mathf.Cos(spiralAngle), 0, Mathf.Sin(spiralAngle)) * radius;
            currentDestination = target.position + offset;

            // Increase the angle for the spiral effect.
            spiralAngle += Mathf.PI / spiralValue;

            int outsideLayer = LayerMask.NameToLayer("Outside");
            RaycastHit destHit;
            if (Physics.Raycast(currentDestination + Vector3.up * 5f, Vector3.down, out destHit, 10f))
            {
                if (destHit.collider.gameObject.layer == outsideLayer)
                {
                    RaycastHit currentHit;
                    if (Physics.Raycast(enemy.transform.position + Vector3.up * 5f, Vector3.down, out currentHit, 10f))
                    {
                        if (currentHit.collider.gameObject.layer != outsideLayer && !transitionTriggered)
                        {
                            transitionTriggered = true;
                            float originalSpeed = agent.speed;
                            enemy.SetMoveSpeed(originalSpeed * transitionSlowdownFactor);
                            if (transitionParticleEffect != null)
                            {
                                transitionParticleEffect.Play();
                            }
                            enemy.StartCoroutine(ResetTransition(enemy, originalSpeed));
                        }
                    }
                }
            }

            agent.SetDestination(currentDestination);
        }
        else
        {
            NavMeshHit hit;
            Vector3 randomDirection = Random.insideUnitSphere * maxChaseRadius + enemy.transform.position;
            if (NavMesh.SamplePosition(randomDirection, out hit, maxChaseRadius, agent.areaMask))
            {
                currentDestination = hit.position;
                agent.SetDestination(currentDestination);
            }
        }
    }

    private IEnumerator ResetTransition(EnemyAI enemy, float originalSpeed)
    {
        yield return new WaitForSeconds(transitionSlowdownDuration);
        enemy.SetMoveSpeed(originalSpeed);
        transitionTriggered = false;
    }

    public override void OnExit(EnemyAI enemy)
    {
        base.OnExit(enemy);
        NavMeshAgent agent = enemy.GetAgent();
        agent.SetAreaCost(outsideArea, outsideAreaCost);
        agent.SetAreaCost(insideArea, insideAreaCost);
    }
}
