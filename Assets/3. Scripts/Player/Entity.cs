using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class Entity : NetworkBehaviour, IDamageable
{
    public PlayerStats Stats { get; private set; }
    // 매번 현재 스탯으로 계산하므로 라운드 중에 공격력/총 배율이 바뀌어도 바로 반영 (총알은 발사 순간 값 사용)
    public float Damage => Stats.Damage * (EquippedGun != null ? EquippedGun.Stats.DamageMultiplier : 1f);
    [Networked] private Gun EquippedGun { get; set; }
    [Networked] public int Team { get; set; }
    [Networked] private int HitCount { get; set; } // 피격 연출 동기화용
    [Networked] public NetworkString<_16> PlayerName { get; set; } // 서버가 연결 토큰에서 읽어 설정
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private int LastAttackTick { get; set; } // 마지막으로 상대를 맞춘 틱 (0이면 없음)
    [Networked] private int LastHitTick { get; set; }    // 마지막으로 맞은 틱 (0이면 없음)

    [SerializeField] private EffectData hitEffect; // 플레이어 피격

    private Rigidbody2D rb;
    private NetworkRigidbody2D networkRb;
    private Renderer[] renderers;
    private Canvas[] canvases;
    private ChangeDetector changes;
    private GameManager game;
    private readonly List<(TickTimer timer, Action revert)> timedReverts = new(); // 서버 전용: 시간제 스탯 효과 되돌리기

    
    private GameManager Game => game != null ? game : game = GameManager.Get(Runner);
    public bool CanAct => !IsDead && (Game == null || Game.CanAct);
    public bool InRound => !IsDead && (Game == null || Game.Phase == GamePhase.Playing); // 살아서 전투 중인 라운드
    public int Round => Game != null ? Game.Round : 0;

    public event Action Ticked; // 서버 전용: 매 시뮬레이션 틱마다 호출 (시간 트리거용)
    public event Action<Entity, float> HitEnemy; // 서버 전용: 내 공격이 상대에게 맞았을 때 (맞은 상대, 들어간 데미지)
    public event Action ReloadStarted;   // 서버 전용: 장전을 시작했을 때
    public event Action ReloadCompleted; // 서버 전용: 장전이 끝나 탄약이 찼을 때
    public event Action<Bullet> BulletFired;   // 서버 전용: 총알을 쏠 때 (총알마다, 스폰 직전)
    public event Action<Bullet> BubbleStepped; // 서버 전용: 자신이나 같은 팀의 거품(총알)을 밟았을 때

    // Gun이 장전 상태가 바뀔 때 호출 (증강 트리거가 총 교체와 상관없이 플레이어에 구독할 수 있도록 중계)
    public void NotifyReloadStarted() => ReloadStarted?.Invoke();
    public void NotifyReloadCompleted() => ReloadCompleted?.Invoke();
    public void NotifyBulletFired(Bullet bullet) => BulletFired?.Invoke(bullet);
    public void NotifyBubbleStepped(Bullet bullet) => BubbleStepped?.Invoke(bullet);
    public Gun Gun => EquippedGun;
    public GunStats GunStats => EquippedGun.Stats;
    public BulletStats BulletStats => EquippedGun.BulletStats; 

    public void Awake()
    {
        Stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        networkRb = GetComponent<NetworkRigidbody2D>();
        renderers = GetComponentsInChildren<Renderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
    }

    public override void Spawned()
    {
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            ChangeGun(GetComponentInChildren<Gun>());
            if (Game != null)
                Game.OnPlayerSpawned(this);
        }

        ApplyDeadState();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Ticked?.Invoke();
        UpdateTimedReverts();
    }

    // 서버 전용: seconds초 뒤에 revert 실행 (시간제 스탯 효과용). 라운드가 바뀌면 실행하지 않고 버림
    public void AddTimedRevert(float seconds, Action revert)
    {
        if (!HasStateAuthority || revert == null) return;
        timedReverts.Add((TickTimer.CreateFromSeconds(Runner, seconds), revert));
    }

    private void UpdateTimedReverts()
    {
        for (int i = timedReverts.Count - 1; i >= 0; i--)
        {
            if (!timedReverts[i].timer.Expired(Runner)) continue;

            Action revert = timedReverts[i].revert;
            timedReverts.RemoveAt(i);
            revert();
        }
    }

    public override void Render()
    {
        foreach (var change in changes.DetectChanges(this))
        {
            if (change == nameof(IsDead))
                ApplyDeadState();
            else if (change == nameof(HitCount))
                hitEffect.Play(transform.position);
        }
    }

    public void ChangeGun(Gun newGun)
    {
        EquippedGun = newGun;
    }

    public void TakeDamage(IAttacker attacker)
    {
        if (IsDead) return;
        if (Game != null && Game.Phase != GamePhase.Playing) return; // 로비 등 라운드 밖에서는 무적

        float damage = Mathf.Max(attacker.Damage - Stats.Defense, 0f);
        Stats.AddBonusStat(PlayerStatType.Health, -damage);
        HitCount++;
        MarkCombat(CombatType.Hit);
        if (attacker.Owner != null)
        {
            attacker.Owner.MarkCombat(CombatType.Attack);
            attacker.Owner.HitEnemy?.Invoke(this, damage);
        }
        
        if (Stats.Health <= 0f)
            Die();
    }

    // 서버 전용: 공격하거나 맞은 순간을 기록 (비전투 판정용). Both면 둘 다 기록
    public void MarkCombat(CombatType type)
    {
        if (!HasStateAuthority) return;

        if (type != CombatType.Hit) LastAttackTick = Runner.Tick;
        if (type != CombatType.Attack) LastHitTick = Runner.Tick;
    }

    // type에 해당하는 전투 이후 seconds초 이상 지났는지 (Both면 공격/피격 모두 그만큼 없어야 함)
    public bool IsOutOfCombat(float seconds, CombatType type = CombatType.Both)
    {
        bool attackOk = type == CombatType.Hit || Elapsed(LastAttackTick) >= seconds;
        bool hitOk = type == CombatType.Attack || Elapsed(LastHitTick) >= seconds;
        return attackOk && hitOk;
    }

    private float Elapsed(int tick)
        => tick == 0 ? float.MaxValue : (Runner.Tick - tick) * Runner.DeltaTime;

    // 라운드 사이에 증강(스탯 보너스)을 유지해야 하므로 Despawn하지 않고 사망 상태로만 전환
    public void Die()
    {
        if (!HasStateAuthority || IsDead) return;

        IsDead = true;
        ApplyDeadState();
    }

    // 서버 전용: 체력을 최대치로 채우고 지정 위치에서 부활
    public void Revive(Vector3 position)
    {
        if (!HasStateAuthority) return;

        Stats.SetBonusStat(PlayerStatType.Health, Stats.MaxHealth - Stats.GetDefaultStat(PlayerStatType.Health));
        EquippedGun!.RefillAmmo();
        
        rb.linearVelocity = Vector2.zero;
        if (networkRb != null)
            networkRb.Teleport(position);
        else
            transform.position = position;

        IsDead = false;
        LastAttackTick = 0; // 새 라운드는 비전투 상태로 시작
        LastHitTick = 0;
        ApplyDeadState();
    }

    // 서버 전용: 최대 체력이 늘어난 만큼 현재 체력도 채움 (죽어 있으면 무시)
    public void HealByMaxHealthGain(float gain)
    {
        if (!HasStateAuthority || IsDead || gain <= 0f) return;
        Stats.AddBonusStat(PlayerStatType.Health, gain);
    }

    // 서버 전용: 최대 체력이 줄어 현재 체력이 더 많아졌으면 최대 체력에 맞춤
    public void ClampHealth()
    {
        if (!HasStateAuthority || IsDead) return;

        float over = Stats.Health - Stats.MaxHealth;
        if (over > 0f)
            Stats.AddBonusStat(PlayerStatType.Health, -over);

        if (Stats.Health <= 0f)
            Die();
    }

    // 서버 전용: 이번 라운드에만 적용된 보너스 스탯 초기화 (라운드 시작 시, 부활 전에 호출)
    public void ResetRoundStats()
    {
        if (!HasStateAuthority) return;

        timedReverts.Clear(); // 라운드 보너스를 통째로 지우므로 남은 되돌리기도 필요 없음
        Stats.ResetRoundBonuses();
        if (EquippedGun != null)
        {
            EquippedGun.Stats.ResetRoundBonuses();
            EquippedGun.ClearRoundBullet();
            if (EquippedGun.BulletStats != null) // 총알 스탯 컴포넌트가 없는 총도 있음
                EquippedGun.BulletStats.ResetRoundBonuses();
        }
    }

    // 서버 전용: 증강으로 얻은 보너스 스탯 초기화 (새 게임 시작 전)
    public void ResetStats()
    {
        if (!HasStateAuthority) return;

        timedReverts.Clear();
        Stats.ResetBonuses();
        if (EquippedGun != null)
        {
            EquippedGun.Stats.ResetBonuses();
            EquippedGun.ResetBullet();
        }
    }

    private void ApplyDeadState()
    {
        bool visible = !IsDead;
        rb.simulated = visible;

        foreach (Renderer r in renderers)
            r.enabled = visible;
        foreach (Canvas c in canvases)
            c.enabled = visible;
    }
}
