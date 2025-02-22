interface IDamageable
{
    int Health { get; set; }

    void TakeDamage(int amount, PlayerController fromPlayer);
}
