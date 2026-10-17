using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour, IAttacker
{
    [Networked] private BulletStats stats { get; set; }

    public Entity Owner { get; private set; }
    public float Damage { get; private set; }
    
    [Networked] private Vector2 Direction { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer RangeTimer { get; set; } // 시간 = 거리 / 속도
    [Networked] private float Speed { get; set; }
    [Networked] private float Range { get; set; }
    
    public void Initialize(Entity owner, NetworkRunner runner, Vector2 direction)
    {
        if (stats == null)
            stats = GetComponent<BulletStats>();

        Owner = owner;
        Damage = owner.stats.Damage;
        Direction = direction;
        Speed = stats.GetStat(BulletStatType.Speed);
        Range = stats.GetStat(BulletStatType.Range);
        LifeTimer = TickTimer.CreateFromSeconds(runner, stats.GetStat(BulletStatType.LifTime));
        RangeTimer = TickTimer.CreateFromSeconds(runner, Range / Speed);
    }

    public override void FixedUpdateNetwork()
    {
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        Vector3 direction = Direction * Speed * Runner.DeltaTime;
        if (RangeTimer.Expired(Runner))
            direction = Vector3.up * Speed * Runner.DeltaTime;
        
        transform.position += direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Object.IsValid || !HasStateAuthority) return;
        if (!other.TryGetComponent(out IDamageable target) || ReferenceEquals(target, Owner)) return;

        target.TakeDamage(this);
        Runner.Despawn(Object);
    }
}