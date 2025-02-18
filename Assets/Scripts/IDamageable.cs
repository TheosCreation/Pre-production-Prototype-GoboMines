using UnityEngine;

interface IDamageable
{
    ParticleSystem HitParticlePrefab { get; set; }
    AudioClip HitSound { get; set; }

    bool IsDead { get; set; }

    void TakeDamage(float amount, ulong attackerId);
}
