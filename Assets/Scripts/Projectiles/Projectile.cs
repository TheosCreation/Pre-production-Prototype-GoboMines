using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] protected float speed = 20.0f; // Speed of the projectile
    [SerializeField] protected float particleSpawnOffset = 0.1f; // Z offset to spawn the hit particles
    [SerializeField] protected float alignmentDistance = 3.0f; // Distance that it takes to align with the correct projectile path
    [SerializeField] protected float headShotMultiplier = 1.5f; // Speed of the projectile
    [SerializeField] protected bool destroyOnHit = true; // Does obj destroy on hit
    [SerializeField] protected TrailRenderer trail;
    [SerializeField] protected float trailLifeTime = 0.5f;
    public UnityEvent onCollision;
    bool arrived = false;
    private Vector3 pointOnPath;
    private Rigidbody rb;

    private Vector3 m_startPosition;
    private Vector3 m_direction;
    private PlayerController m_weaponUser;
    private int m_damage = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 startPosition, Vector3 direction, PlayerController weaponUser, int damage)
    {
        m_weaponUser = weaponUser;
        m_startPosition = startPosition;
        m_direction = direction;
        m_damage = damage;

        // Validate rigidbody
        if (rb == null)
        {
            Debug.LogError("Rigidbody is not assigned to the projectile.");
            return;
        }

        // Validate direction
        if (direction.magnitude == 0)
        {
            Debug.LogError("Direction vector is zero, cannot initialize projectile.");
            return;
        }


        pointOnPath = startPosition + direction * alignmentDistance;

        // Align the projectile's orientation
        transform.forward = direction.normalized;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (!arrived)
        {
            // Move towards alignment point
            float step = Time.fixedDeltaTime * speed;
            transform.position = Vector3.MoveTowards(transform.position, pointOnPath, step);

            // Check if alignment is done
            if (Vector3.Distance(transform.position, pointOnPath) <= 0.05f)
            {
                arrived = true;

                // Apply force to move the projectile
                rb.AddForce(m_direction.normalized * speed, ForceMode.VelocityChange);
            }
        }
    }


    //private void OnTriggerEnter(Collider other)
    //{
    //    // Check if we hit an object on the collisionMask
    //    if (other.gameObject.layer == ownerLayer) {  return; };
    //    if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;
    //
    //    ProcessCollision(other);
    //}

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        ProcessCollision(collision.GetContact(0));
    }

    protected void ProcessCollision(ContactPoint contactPoint)
    {
        // Calculate the hit point and normal
        Vector3 hitPoint = contactPoint.point;
        Vector3 hitNormal = contactPoint.normal;

        if (hitNormal == Vector3.zero)
        {
            hitNormal = Vector3.up; // Fallback normal
        }

        // Check if we hit ourselves
        NetworkObject hitObject = contactPoint.otherCollider.GetComponentInParent<NetworkObject>();
        if (hitObject != null && hitObject.OwnerClientId == m_weaponUser.OwnerClientId)
        {
            // Ignore collision if we hit ourselves
            return;
        }

        // Deal damage if the object is damageable
        IDamageable damageable = contactPoint.otherCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            damageable = contactPoint.otherCollider.GetComponent<IDamageable>();
        }
        if (damageable != null && !damageable.IsDead)
        {
            if (contactPoint.otherCollider.gameObject.CompareTag("Head"))
            {
                //m_weaponUser.OnHit(true); //for a hitmarker indicator
                damageable.TakeDamage((int)(m_damage * headShotMultiplier), m_weaponUser);
                HitDamageable(hitPoint, hitNormal, damageable.HitParticlePrefab, damageable.HitSound);
            }
            else
            {
                // m_weaponUser.OnHit(false); // for a hitmarker indicator
                damageable.TakeDamage(m_damage, m_weaponUser);
                HitDamageable(hitPoint, hitNormal, damageable.HitParticlePrefab, damageable.HitSound);
            }
        }
        else
        {
            // hit a wall
            HitOther(hitPoint, hitNormal, contactPoint.otherCollider.tag);
        }

        onCollision?.Invoke();
        // Destroy the projectile on collision
        if (destroyOnHit)
        {
            NetworkObject.Despawn();
        }
    }


    protected void HitDamageable(Vector3 hitPosition, Vector3 normal, ParticleSystem particleToSpawn, AudioClip audioToPlay)
    {
        if (!IsServer) return; // Only the server spawns particles and sounds

        // Spawn hit particles
        ParticleSystem hitParticles = Instantiate(particleToSpawn, hitPosition, Quaternion.LookRotation(-normal)).GetComponent<ParticleSystem>();
        NetworkObject netObj = hitParticles.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        //trail.transform.parent = null;
        //Destroy(trail, trailLifeTime);

        // Despawn the sound maker after the clip finishes
        float duration = hitParticles.main.duration + hitParticles.main.startLifetime.constantMax;
        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(netObj, duration);

        // Play hit sound
        //CreateHitSound(audioToPlay, hitPosition);
    }

    protected void HitOther(Vector3 hitPosition, Vector3 wallNormal, string tag)
    {
        if (!IsServer) return; // Only the server spawns particles and sounds

        // Spawn hit particles with offset
        Vector3 particlePositionOffset = wallNormal * particleSpawnOffset;
        ParticleSystem hitParticles = Instantiate(GameManager.Instance.prefabs.hitWallPrefab, hitPosition + particlePositionOffset, Quaternion.LookRotation(-wallNormal)).GetComponent<ParticleSystem>();
        NetworkObject netObj = hitParticles.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        //trail.transform.parent = null;
        //Destroy(trail, trailLifeTime);

        float duration = hitParticles.main.duration + hitParticles.main.startLifetime.constantMax;
        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(netObj, duration);

        // Play hit sound based on tag
        //AudioClip clipToPlay = null;
        //switch (tag)
        //{
        //    case "Stone":
        //        clipToPlay = GameManager.Instance.prefabs.stoneImpactSound;
        //        break;
        //    case "Wood":
        //        clipToPlay = GameManager.Instance.prefabs.woodImpactSound;
        //        break;
        //    default:
        //        Debug.Log($"Hit sounds for tag: {tag} not implemented");
        //        break;
        //}

        //if (clipToPlay != null)
        //{
        //    CreateHitSound(clipToPlay, hitPosition);
        //}
    }
}
