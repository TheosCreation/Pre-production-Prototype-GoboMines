using Unity.Netcode;
using UnityEngine;

interface IDamageable
{
    ParticleSystem HitParticlePrefab { get; set; }
    AudioClip[] HitSounds { get; set; }

    bool IsDead { get; set; }

    int Health { get; set; }


    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(int amount, ulong clientId);

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(int amount, NetworkObjectReference fromObject);
    [ClientRpc]
    void TakeDamageClientRpc(int amount, ulong clientId = 100000);
}
