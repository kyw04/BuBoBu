using System;
using UnityEngine;

public enum SizeTarget
{
    Player,       // 플레이어 크기
    Bullets,      // 앞으로 쏘는 총알 전부의 크기
    TriggerBullet // 트리거가 정한 총알 하나의 크기 (OnFireTrigger 등과 함께 사용)
}

// 플레이어나 총알의 크기를 바꿈. 크기는 배율이라 기본 1
// Percent: 50 = 1.5배, -50 = 0.5배 / Flat: 0.5 = 기본 크기에 +0.5배
[Serializable]
public class SizeEffect : IAugmentEffect
{
    [SerializeField] private AugmentSubject subject = AugmentSubject.Caster; // 크기를 바꿀 플레이어 (TriggerBullet이면 사용 안 함)
    [SerializeField] private SizeTarget target = SizeTarget.Player;
    [SerializeField] private ModifierType type = ModifierType.Percent;
    [SerializeField] private float value = 50f;
    [SerializeField] private StatDuration duration = StatDuration.Permanent; // TriggerBullet이면 총알이 사라질 때까지
    [SerializeField, Min(0.1f)] private float seconds = 3f; // duration == Timed 일 때 유지 시간

    public void Apply(AugmentContext context)
    {
        if (target == SizeTarget.TriggerBullet)
        {
            Bullet bullet = context.Bullet;
            if (bullet == null || bullet.IsDead) return;

            bullet.AddStat(BulletStatType.Size, value, type);
            bullet.RefreshStats();
            return;
        }

        Entity entity = context.Get(subject);
        if (entity == null) return;

        PlayerStatType stat = target == SizeTarget.Player ? PlayerStatType.Size : PlayerStatType.BulletSize;
        AddSize(entity.Stats, stat, value);

        // 시간이 끝나면 같은 양을 반대로 적용해서 되돌림
        if (duration == StatDuration.Timed)
            entity.AddTimedRevert(seconds, () => AddSize(entity.Stats, stat, -value));
    }

    private void AddSize(PlayerStats stats, PlayerStatType stat, float amount)
    {
        bool flat = type == ModifierType.Flat;
        if (!flat) amount /= 100f;

        // 라운드/시간 효과는 라운드 보너스에 넣어서 라운드가 바뀌면 함께 초기화되게 함
        if (duration == StatDuration.Permanent)
        {
            if (flat) stats.AddBonusStat(stat, amount);
            else stats.AddMultiplierBonus(stat, amount);
        }
        else
        {
            if (flat) stats.AddRoundBonusStat(stat, amount);
            else stats.AddRoundMultiplierBonus(stat, amount);
        }
    }
}
