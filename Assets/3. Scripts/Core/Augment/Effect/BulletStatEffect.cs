using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BulletModifier
{
    public BulletStatType stat;
    public ModifierType type;
    public float value; // Flat: 더할 값 / Percent: 50 = +50% (음수면 감소)
}

// 트리거가 정한 총알(context.Bullet) 하나에만 스탯 적용. 다른 총알이나 플레이어 스탯은 바뀌지 않음
// OnFireTrigger와 함께 쓰면 쏘는 순간 적용되고, 이미 날아가는 중인 총알이면 지난 시간을 빼고 다시 계산
[Serializable]
public class BulletStatEffect : IAugmentEffect
{
    [SerializeField] private List<BulletModifier> modifiers = new();
    [SerializeField] private float damagePercent; // 이 총알의 데미지 변화 (50 = +50%, 0 = 그대로)

    public void Apply(AugmentContext context)
    {
        Bullet bullet = context.Bullet;
        if (bullet == null || bullet.IsDead) return;

        foreach (BulletModifier modifier in modifiers)
            bullet.AddStat(modifier.stat, modifier.value, modifier.type);

        if (!Mathf.Approximately(damagePercent, 0f))
            bullet.AddDamagePercent(damagePercent);

        bullet.RefreshStats();
    }
}
