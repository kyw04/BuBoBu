using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class Entity : NetworkBehaviour, IDamageable
{
    public PlayerStats stats { get; private set; }
    public float Damage { get; private set; }
    [Networked] private Gun EquippedGun { get; set; }
    [Networked] public int Team { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; } // 서버가 연결 토큰에서 읽어 설정
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private int HitCount { get; set; } // 피격 연출 동기화용

    [SerializeField] private EffectData hitEffect; // 플레이어 피격

    private Rigidbody2D rb;
    private NetworkRigidbody2D networkRb;
    private Renderer[] renderers;
    private Canvas[] canvases;
    private ChangeDetector changes;
    private GameManager game;

    private GameManager Game => game != null ? game : game = GameManager.Get(Runner);
    public bool CanAct => !IsDead && (Game == null || Game.CanAct);

    public void Awake()
    {
        stats = GetComponent<PlayerStats>();
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
        Damage = stats.Damage;

        if (EquippedGun == null)
            return;

        Damage *= EquippedGun.stats.DamageMultiplier;
    }

    public void TakeDamage(IAttacker attacker)
    {
        if (IsDead) return;
        if (Game != null && Game.Phase != GamePhase.Playing) return; // 로비 등 라운드 밖에서는 무적

        stats.AddBonusStat(PlayerStatType.Health, -Mathf.Max(attacker.Damage - stats.Defense, 0f));
        HitCount++;
        if (stats.Health <= 0f)
            Die();
    }

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

        stats.SetBonusStat(PlayerStatType.Health, stats.MaxHealth - stats.GetDefaultStat(PlayerStatType.Health));
        Damage = stats.Damage * (EquippedGun != null ? EquippedGun.stats.DamageMultiplier : 1f); // 증강 반영

        rb.linearVelocity = Vector2.zero;
        if (networkRb != null)
            networkRb.Teleport(position);
        else
            transform.position = position;

        IsDead = false;
        ApplyDeadState();
    }

    // 서버 전용: 증강으로 얻은 보너스 스탯 초기화 (새 게임 시작 전)
    public void ResetStats()
    {
        if (!HasStateAuthority) return;

        stats.ResetBonuses();
        if (EquippedGun != null)
            EquippedGun.stats.ResetBonuses();
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
