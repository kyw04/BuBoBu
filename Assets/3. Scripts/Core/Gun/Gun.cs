using Fusion;
using UnityEngine;

public class Gun : NetworkBehaviour
{
    public GunStats stats { get; private set; }
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private int BulletCount { get; set; }
    [Networked] private Vector2 AimDirection { get; set; }

    private Entity owner;
    private float orbitRadius;

    private void Awake()
    {
        stats = GetComponent<GunStats>();
    }

    public override void Spawned()
    {
        owner = GetComponentInParent<Entity>();
        orbitRadius = transform.localPosition.magnitude;
        ApplyAim();
    }

    public void Aim(Vector2 aimPoint)
    {
        Vector2 direction = aimPoint - (Vector2)transform.parent.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        AimDirection = direction.normalized;
        ApplyAim();
    }

    public override void Render()
    {
        ApplyAim();
    }

    public void Fire()
    {
        if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

        CooldownTimer = TickTimer.CreateFromSeconds(Runner, stats.GetStat(GunStatType.FireSpeed));

        Vector2 direction = firePoint.position - transform.position;
        Runner.Spawn(bulletPrefab, firePoint.position, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<Bullet>().Initialize(owner, runner, direction.normalized);
        });
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
}
