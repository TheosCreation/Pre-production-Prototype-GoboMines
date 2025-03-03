using UnityEngine;

interface IDamageable
{
    ParticleSystem HitParticlePrefab { get; set; }
    AudioClip[] HitSounds { get; set; }

    bool IsDead { get; set; }

    int Health { get; set; }

    void TakeDamage(int amount, PlayerController fromPlayer);

    void TakeDamage(int amount, GameObject fromObject);
}
