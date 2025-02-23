using Unity.Netcode;
using UnityEngine;

public class Pickaxe : Weapon
{
    [SerializeField] private float attackRange = 3.0f;

    [ServerRpc]
    protected override void AttackServerRpc()
    {
        base.AttackServerRpc();

        Vector3 firePosition = player.playerLook.playerCamera.transform.position;
        Vector3 direction = player.playerLook.playerCamera.transform.forward;

        RaycastHit hit;
        if(Physics.Raycast(firePosition, direction, out hit, attackRange))
        {
            // Check if we hit ourselves
            NetworkObject hitObject = hit.collider.GetComponentInParent<NetworkObject>();
            if (hitObject != null && hitObject.OwnerClientId == player.OwnerClientId)
            {
                // Ignore collision if we hit ourselves
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                ulong attackerId = player.OwnerClientId;

                damageable = hit.collider.GetComponent<IDamageable>();
                damageable.TakeDamage(damage, attackerId);
            }
        }
    }
}
