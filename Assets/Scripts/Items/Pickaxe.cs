using Unity.Netcode;
using UnityEngine;

public class Pickaxe : Weapon
{
    [SerializeField] private float attackRange = 3.0f;
    public LayerMask hitMask;

    protected override void Attack()
    {
        base.Attack();

        AttackServerRpc();
    }

    [ServerRpc]
    protected void AttackServerRpc()
    {
        Vector3 firePosition = player.playerLook.playerCamera.transform.position;
        Vector3 direction = player.playerLook.playerCamera.transform.forward;

        RaycastHit hit;
        if(Physics.Raycast(firePosition, direction, out hit, attackRange, hitMask))
        {
            Debug.Log("Hit something");
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.collider.GetComponent<IDamageable>();
            }

            if (damageable != null)
            {

                damageable.TakeDamage(damage, player);
            }
        }
    }
}
