using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class Entity : NetworkBehaviour, IDamageable
{
    public PlayerStats stats { get; private set; }

    public void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void TakeDamage(IAttacker attacker)
    {
        stats.AddBonusStat(PlayerStatType.Health, -Mathf.Max(attacker.Damage - stats.Defense, 0f));
        if (stats.Health <= 0f)
            Die();
    }

    public void Die()
    {
        Runner.Despawn(Object);
    }
}