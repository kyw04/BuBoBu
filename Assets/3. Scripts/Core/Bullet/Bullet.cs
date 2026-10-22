using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour, IAttacker
{
    [Networked] private BulletStats stats { get; set; }

    public Entity Owner { get; private set; }
    public float Damage { get; private set; }
    
    [Networked] private Vector2 Direction { get; set; }
    [Networked] private TickTimer InvincibilityTimer { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer RangeTimer { get; set; } // 시간 = 거리 / 속도
    [Networked] private float Speed { get; set; }
    [Networked] private float Range { get; set; }
    
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private TickTimer DeathTimer { get; set; }
    
    [SerializeField] private Animator animator;
    [SerializeField] private float deathDuration = 0.3f; // 터지는 애니메이션 길이

    private ChangeDetector changes;

    public override void Spawned()
    {
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public void Initialize(Entity owner, NetworkRunner runner, Vector2 direction)
    {
        if (stats == null)
            stats = GetComponent<BulletStats>();

        Owner = owner;
        Damage = owner.Damage;
        Direction = direction;
        Speed = stats.GetStat(BulletStatType.Speed);
        Range = stats.GetStat(BulletStatType.Range);
        InvincibilityTimer = TickTimer.CreateFromSeconds(runner, 0.1f);
        LifeTimer = TickTimer.CreateFromSeconds(runner, stats.GetStat(BulletStatType.LifTime));
        RangeTimer = TickTimer.CreateFromSeconds(runner, Range / Speed);
    }
    
    public void Die()
    {
        if (!HasStateAuthority || IsDead) return;
        IsDead = true;
        DeathTimer = TickTimer.CreateFromSeconds(Runner, deathDuration);
        OnDeathStart();
    }

    protected virtual void OnDeathStart() { }

    protected virtual void OnDeathEnd()
    {
        Runner.Despawn(Object);
    }

    protected virtual void OnDeathVisual()
    {
        if (animator != null)
            animator.SetTrigger("Pop");
    }

    public override void Render()
    {
        foreach (var change in changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsDead) when IsDead:
                    OnDeathVisual();
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead)
        {
            if (HasStateAuthority && DeathTimer.Expired(Runner))
                OnDeathEnd();
            return;
        }

        if (LifeTimer.Expired(Runner))
        {
            Die();
            return;
        }

        Vector3 direction = Direction * Speed * Runner.DeltaTime;
        if (RangeTimer.Expired(Runner))
            direction = Vector3.up * Speed * Runner.DeltaTime;
        
        transform.position += direction;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Object.IsValid || !HasStateAuthority || IsDead) return;
        if (!InvincibilityTimer.Expired(Runner)) return;
        
        if (!other.TryGetComponent(out IDamageable target) || ReferenceEquals(target, Owner))
        {
            if (other.TryGetComponent(out Player.PlayerMovement movement) && transform.position.y < movement.Foot.position.y)
            {
                movement.Jump();
                Die();
            }
            return;
        }

        target.TakeDamage(this);
        Die();
    }
}