using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Networked] public BulletStats stats { get; private set; }

    [Networked] private Vector2 Direction { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer RangeTimer { get; set; } // 시간 = 거리 / 속도
    [Networked] private float Speed { get; set; }
    [Networked] private float Range { get; set; }
    
    
    public void Initialize(NetworkRunner runner, Vector2 direction)
    {
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
}