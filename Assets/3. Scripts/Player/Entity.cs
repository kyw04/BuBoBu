using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class Entity : NetworkBehaviour, IDamageable
{
    public PlayerStats stats { get; private set; }
    public float Damage { get; private set; }
    [Networked] private Gun EquippedGun { get; set; }
    
    public void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
            ChangeGun(GetComponentInChildren<Gun>());
    }

    public void ChangeGun(Gun newGun)
    {
        EquippedGun = newGun;
        Damage = stats.Damage;
        
        if (EquippedGun == null)
            return;
        
        Damage *= EquippedGun.stats.DamageMultiplier;
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