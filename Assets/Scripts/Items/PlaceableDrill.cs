using Unity.Netcode;
using UnityEngine;

public class PlacableDrill : Item
{
    [SerializeField] private float attackRange = 3.0f;
    [SerializeField] private int damage = 10;
    public LayerMask hitMask;
    [SerializeField] private GameObject drill;
    protected PlayerController player;

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<PlayerController>();
    }


    /// <summary>
    /// When Placed
    /// </summary>

    public void DrillAttack()
    {
        DrillAttackServerRpc();
    }

    [ServerRpc]
    protected void DrillAttackServerRpc()
    {
        Vector3 firePosition = drill.transform.position;
        Vector3 direction = drill.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(firePosition, direction, out hit, attackRange, hitMask))
        {
            Debug.Log("Hit something");
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.collider.GetComponent<IDamageable>();
            }

            if (damageable != null)
            {
                if (damageable.HitSounds.Length > 0)
                {
                    AudioClip hitSound = damageable.HitSounds[Random.Range(0, damageable.HitSounds.Length)];
                    NetworkSpawnHandler.Instance.SpawnSound(hitSound, hit.point);
                }

                damageable.TakeDamage(damage, drill);
            }
        }
    }




    /// <summary>
    /// When In Hand
    /// </summary>

    public override void StartAttacking()
    {
        base.StartAttacking();

        AttackServerRpc();
    }

    [ServerRpc]
    protected void AttackServerRpc()
    {
        Vector3 firePosition = player.playerLook.playerCamera.transform.position;
        Vector3 direction = player.playerLook.playerCamera.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(firePosition, direction, out hit, attackRange, hitMask))
        {
            Debug.Log("Hit something");
            GameObject hitObject = hit.collider.GetComponentInParent<GameObject>();
            if (hitObject == null)
            {
                hitObject = hit.collider.GetComponent<GameObject>();
            }

            if (hitObject != null)
            {

                // Place Drill On Object At Hit Location
                Vector3 hitPosition = hit.collider.transform.position;
            }
        }
    }
}
