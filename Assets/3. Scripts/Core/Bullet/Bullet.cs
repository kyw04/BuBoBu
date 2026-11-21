using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour, IAttacker, IPoolable
{
    [Networked] private BulletStats Stats { get; set; }

    public Entity Owner { get; private set; }
    public float Damage { get; private set; }

    // 서버 전용: 어떤 총알인지 (증강 조건용)
    public int ShotNumber { get; private set; }     // 총을 쏜 횟수 중 몇 번째 발사인지 (1부터)
    public int MagazineIndex { get; private set; }  // 탄창에서 몇 번째 총알인지 (1부터)
    public bool IsLastInMagazine { get; private set; }
    
    [Networked] private Vector2 Direction { get; set; }
    [Networked] private TickTimer InvincibilityTimer { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer RangeTimer { get; set; } // 시간 = 거리 / 속도
    [Networked] private TickTimer UpTimer { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private float Range { get; set; }
    [Networked] private float UpRange { get; set; }
    [Networked] private int SpawnTick { get; set; } // 스탯이 바뀌었을 때 남은 수명/사거리를 다시 계산하는 기준
    
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private TickTimer DeathTimer { get; set; }
    
    [SerializeField] private Animator animator;
    [SerializeField] private float deathDuration = 0.3f; // 터지는 애니메이션 길이
    [SerializeField] private EffectData spawnEffect;
    [SerializeField] private EffectData deathEffect; // 피격·수명 종료 등으로 터질 때

    private ChangeDetector changes;
    private NetworkRunner runner; // onBeforeSpawned 안에서는 Runner가 아직 없을 수 있어서 따로 보관

    private float upTime;
    
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
        ShotNumber = 0;
        MagazineIndex = 0;
        IsLastInMagazine = false;
        if (animator != null)
            animator.Rebind();
    }

    public void Initialize(Entity owner, NetworkRunner runner, Vector2 direction,
        int shotNumber = 0, int magazineIndex = 0, bool isLastInMagazine = false)
    {
        if (Stats == null)
            Stats = GetComponent<BulletStats>();
        Stats.ResetBonuses(); // 풀에서 재사용될 때 이전 총알에 적용된 증강 스탯 제거

        this.runner = runner;
        Owner = owner;
        Damage = owner.Damage;
        Direction = direction;
        ShotNumber = shotNumber;
        MagazineIndex = magazineIndex;
        IsLastInMagazine = isLastInMagazine;
        SpawnTick = runner.Tick;
        InvincibilityTimer = TickTimer.CreateFromSeconds(runner, 0.1f);
        RefreshStats();
    }

    // 서버 전용: 이 총알 하나에만 스탯 보너스 적용 (증강 효과용). 적용 후 RefreshStats 호출 필요
    public void AddStat(BulletStatType type, float value, ModifierType modifierType)
    {
        if (modifierType == ModifierType.Flat) Stats.AddBonusStat(type, value);
        else Stats.AddMultiplierBonus(type, value / 100f); // Percent: 50 = +50%
    }

    // 서버 전용: 이 총알의 데미지를 percent%만큼 변경 (50 = +50%, -20 = -20%)
    public void AddDamagePercent(float percent)
    {
        Damage = Mathf.Max(0f, Damage * (1f + percent / 100f));
    }

    // 현재 스탯으로 속도/사거리/수명을 다시 계산. 이미 지난 시간만큼은 빼고 남은 시간으로 타이머를 다시 맞춤
    public void RefreshStats()
    {
        Speed = Stats.GetStat(BulletStatType.Speed);
        Range = Stats.GetStat(BulletStatType.Range);
        UpRange = Stats.GetStat(BulletStatType.UpRange);

        float elapsed = (runner.Tick - SpawnTick) * runner.DeltaTime;
        LifeTimer = CreateTimer(Stats.GetStat(BulletStatType.LifTime) - elapsed);
        RangeTimer = CreateTimer((Speed > 0f ? Range / Speed : 0f) - elapsed);
        upTime = (Speed > 0f ? UpRange / Speed : 0f) - elapsed;
    }

    private TickTimer CreateTimer(float seconds)
        => TickTimer.CreateFromSeconds(runner, Mathf.Max(seconds, runner.DeltaTime));
    
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
        {
            if (!UpTimer.IsRunning)
                UpTimer = CreateTimer(upTime);
            
            direction = Vector3.up * Speed * Runner.DeltaTime;
        }
        if (UpTimer.Expired(Runner))
            direction = Vector3.zero;
        
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