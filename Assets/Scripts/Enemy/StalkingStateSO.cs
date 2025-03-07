using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "EnemyStates/StalkingState", fileName = "StalkingState")]
public class StalkingStateSO : BaseState
{
    public float lineOfSightDistance = 15f;
    public float playerViewThreshold = 0.8f;
    public bool enableDebugging = true;
    public LayerMask raycastLayerMask;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepTimer;
    public AudioClip[] scream;
    private int lastFootstep = -1;

    public override void OnEnter(EnemyAI enemy)
    {
        base.OnEnter(enemy);
        enemy.SetTargetClientRpc(FindNearestPlayer(enemy));
    }

    public override void OnUpdate(EnemyAI enemy)
    {
        if (enemy.GetTarget() != null && Vector3.Distance(enemy.transform.position, enemy.GetTarget().position) < attackRange)
        {
            PlayRandomScream(enemy);
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

        float velocityMagnitude = enemy.GetAgent().velocity.magnitude;
        footstepTimer = Mathf.MoveTowards(footstepTimer, 0f,
            (Mathf.Min(velocityMagnitude, 15f) / 15f) * Time.deltaTime * moveSpeed * 1.5f);

        if (footstepTimer <= 0f)
        {
            Footstep(enemy);
        }
    }

    public void Footstep(EnemyAI enemy, float volume = 0.5f, bool force = false, float delay = 0f)
    {
        footstepTimer = 1f;
        PlayRandomFootstepClip(enemy, footstepSounds, delay);
    }

    private void PlayRandomFootstepClip(EnemyAI enemy, AudioClip[] clips, float delay = 0f)
    {
        if (clips != null && clips.Length != 0)
        {
            int num = Random.Range(0, clips.Length);
            if (clips.Length > 1 && num == lastFootstep)
            {
                num = (num + 1) % clips.Length;
            }
            lastFootstep = num;
            PlayFootstepClip(enemy, clips[num], delay);
        }
    }

    private void PlayFootstepClip(EnemyAI enemy, AudioClip clip, float delay = 0f)
    {
        if (clip != null)
        {
            // Assume enemy has a method to get its movement AudioSource.
            AudioSource movementAudioSource = enemy.GetComponent<AudioSource>();
            if (movementAudioSource != null)
            {
                movementAudioSource.clip = clip;
                movementAudioSource.pitch = Random.Range(0.9f, 1.1f);
                if (delay == 0f)
                {
                    movementAudioSource.Play();
                }
                else
                {
                    movementAudioSource.PlayDelayed(delay);
                }
            }
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

    private GameObject FindNearestPlayer(EnemyAI enemy)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(enemy.transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player;
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
    private void PlayRandomScream(EnemyAI enemy, float delay = 0f)
    {
        if (scream != null && scream.Length > 0)
        {
            int index = Random.Range(0, scream.Length);
            AudioClip chosenClip = scream[index];
            AudioSource audioSource = enemy.GetComponent<AudioSource>();
            if (audioSource != null && chosenClip != null)
            {
                audioSource.clip = chosenClip;
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                if (delay == 0f)
                {
                    audioSource.Play();
                }
                else
                {
                    audioSource.PlayDelayed(delay);
                }
            }
        }
    }
}
