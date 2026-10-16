using Fusion;
using UnityEngine;

public class Gun : NetworkBehaviour
{
    [SerializeField] private GunStats stats;
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private Transform firePoint;
    
    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private int BulletCount { get; set; }

    public void Fire()
    {
        if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

        CooldownTimer = TickTimer.CreateFromSeconds(Runner, stats.GetStat(GunStatType.FireSpeed));

        Vector2 direction = firePoint.position - transform.position;
        Runner.Spawn(bulletPrefab, firePoint.position, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<Bullet>().Initialize(runner, direction.normalized);
        });
    }
}
