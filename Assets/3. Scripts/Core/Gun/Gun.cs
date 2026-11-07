using Fusion;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Gun : NetworkBehaviour
{
    public GunStats stats { get; private set; }
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private EffectData fireEffect; // 플레이어 공격
    [SerializeField] private GunDatabase database;  // 총 증강으로 바꿀 수 있는 총 목록
    [SerializeField] private Image ammoImage;

    [Networked] private int Ammo { get; set; }
    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private Vector2 AimDirection { get; set; }
    [Networked] private int FireCount { get; set; } // 발사는 서버에서만 일어나므로 클라이언트 연출용으로 동기화
    [Networked] private int GunId { get; set; }     // 0 = 기본 총, n = database의 n-1번 총

    private ChangeDetector changes;
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;

    public GunDatabase Database => database;
    public GunData CurrentData => database != null ? database.Get(GunId - 1) : null;

    public int MaxAmmo => (int)stats.GetStat(GunStatType.Ammo);
    public bool IsReloading => ReloadTimer.IsRunning;

    public float AmmoFill
    {
        get
        {
            if (IsReloading)
            {
                float total = stats.GetStat(GunStatType.ReloadTime);
                float remaining = ReloadTimer.RemainingTime(Runner) ?? 0f;
                return total > 0f ? 1f - remaining / total : 1f;
            }
            return MaxAmmo > 0 ? (float)Ammo / MaxAmmo : 0f;
        }
    }

    private Entity owner;
    private float orbitRadius;

    private void Awake()
    {
        stats = GetComponent<GunStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            defaultSprite = spriteRenderer.sprite;
    }

    public override void Spawned()
    {
        owner = GetComponentInParent<Entity>();
        orbitRadius = transform.localPosition.magnitude;
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        ApplyAim();
        ApplyGunData();

        if (HasStateAuthority)
            Ammo = MaxAmmo;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (ReloadTimer.Expired(Runner))
        {
            ReloadTimer = TickTimer.None;
            Ammo = MaxAmmo;
        }
    }

    public override void Render()
    {
        ApplyAim();
        ammoImage.fillAmount = AmmoFill;
        
        
        foreach (var change in changes.DetectChanges(this))
        {
            if (change == nameof(FireCount))
                fireEffect.Play(firePoint.position, firePoint.rotation);
            else if (change == nameof(GunId))
                ApplyGunData();
        }
    }

    // 서버 전용: database의 index번 총으로 교체. -1이면 기본 총
    public void SetGun(int index)
    {
        if (!HasStateAuthority) return;

        GunId = index + 1;
        ApplyGunData(); // 데디케이티드 서버는 Render가 안 돌 수 있으므로 바로 적용

        ReloadTimer = TickTimer.None;
        Ammo = MaxAmmo;
    }

    public void ResetGun() => SetGun(-1);

    public void Aim(Vector2 aimPoint)
    {
        Vector2 direction = aimPoint - (Vector2)transform.parent.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        AimDirection = direction.normalized;
        ApplyAim();
    }

    public void Fire()
    {
        if (IsReloading || Ammo <= 0 || !CooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        CooldownTimer = TickTimer.CreateFromSeconds(Runner, stats.GetStat(GunStatType.FireSpeed));

        float halfAngle = stats.GetStat(GunStatType.Angle) / 2f;
        int count = (int)stats.GetStat(GunStatType.BulletCount);
        Vector2 forward = (firePoint.position - transform.position).normalized;
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, Random.Range(-halfAngle, halfAngle)) * forward;
            Runner.Spawn(bulletPrefab, firePoint.position, onBeforeSpawned: (runner, obj) =>
                obj.GetComponent<Bullet>().Initialize(owner, runner, direction));
        }

        FireCount++;
        if (--Ammo <= 0)
            Reload();
    }

    public void Reload()
    {
        if (IsReloading || Ammo >= MaxAmmo) return;

        ReloadTimer = TickTimer.CreateFromSeconds(Runner, stats.GetStat(GunStatType.ReloadTime));
    }

    private void ApplyGunData()
    {
        GunData data = CurrentData;
        stats.SetData(data);

        if (spriteRenderer != null)
            spriteRenderer.sprite = data != null && data.Sprite != null ? data.Sprite : defaultSprite;
    }

    private void ApplyAim()
    {
        Vector2 direction = AimDirection == Vector2.zero ? Vector2.right : AimDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.localPosition = direction * orbitRadius;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 scale = transform.localScale;
        scale.y = Mathf.Abs(scale.y) * (direction.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;
        float range = stats != null ? stats.GetStat(GunStatType.Angle) : 30f;
        Vector2 direction = firePoint.position - transform.position;
        Vector2 dir2 = Quaternion.Euler(0, 0, range / 2) * direction;
        Vector2 dir3 = Quaternion.Euler(0, 0, -range / 2) * direction;
        Gizmos.DrawRay(firePoint.position, dir2.normalized * 5);
        Gizmos.DrawRay(firePoint.position, dir3.normalized * 5);
    }
}
