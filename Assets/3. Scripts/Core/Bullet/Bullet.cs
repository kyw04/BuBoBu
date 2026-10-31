using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour, IAttacker, IPoolable
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
    [SerializeField] private EffectData spawnEffect;
    [SerializeField] private EffectData deathEffect; // 피격·수명 종료 등으로 터질 때

    private ChangeDetector changes;

    public override void Spawned()
    {
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        spawnEffect.Play(transform.position);
    }

    // 풀로 돌아갈 때 로컬 상태 정리 (네트워크 상태는 다음 Initialize에서 다시 설정됨)
    public void OnReturnToPool()
    {
        Owner = null;
        Damage = 0f;
        if (animator != null)
            animator.Rebind();
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
        deathEffect.Play(transform.position);
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
        
        // 자신과 같은 팀은 피격 대상이 아님 (총알 밟고 점프만 가능)
        if (!other.TryGetComponent(out IDamageable target) || IsAlly(target))
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

    private bool IsAlly(IDamageable target)
    {
        if (ReferenceEquals(target, Owner)) return true;
        return target is Entity entity && Owner != null && entity.Team == Owner.Team;
    }
}