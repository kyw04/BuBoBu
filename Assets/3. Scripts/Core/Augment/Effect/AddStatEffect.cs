using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierType
{
    Flat,
    Percent
}

// 스탯 변화가 유지되는 기간
public enum StatDuration
{
    Permanent, // 게임이 끝날 때까지
    Round,     // 이번 라운드만 (다음 라운드 시작 때 되돌림)
    Timed      // seconds초 동안만 (라운드가 먼저 끝나도 되돌림)
}

[System.Serializable]
public struct AugmentModifier
{
    public AugmentTarget target;
    public PlayerStatType playerStat; // target == Player 일 때 사용
    public GunStatType gunStat;       // target == Gun 일 때 사용
    public BulletStatType bulletStat; // target == Bullet 일 때 사용
    public ModifierType type;
    public float value;            // Bonus에 더해지는 값 (음수면 감소)
}

[System.Serializable]
public class AddStatEffect : IAugmentEffect
{
    [SerializeField] private AugmentSubject subject = AugmentSubject.Caster; // 스탯을 바꿀 대상
    [SerializeField] private StatDuration duration = StatDuration.Permanent;
    [SerializeField, Min(0.1f)] private float seconds = 3f; // duration == Timed 일 때 유지 시간
    [SerializeField] private List<AugmentModifier> modifiers = new();

    public void Apply(AugmentContext context)
    {
        Entity entity = context.Get(subject);
        if (entity == null) return;

        ApplyModifiers(entity, 1f);
        entity.ClampHealth(); // 최대 체력이 줄었으면 현재 체력도 맞춤

        // 시간이 끝나면 같은 양을 반대로 적용해서 되돌림
        if (duration == StatDuration.Timed)
        {
            entity.AddTimedRevert(seconds, () =>
            {
                ApplyModifiers(entity, -1f);
                entity.ClampHealth();
            });
        }
    }

    // 최대 체력이 늘어난 만큼 현재 체력도 채움 (줄어든 경우는 ClampHealth가 맞춤)
    private void ApplyModifiers(Entity entity, float sign)
    {
        float maxHealthBefore = entity.Stats.MaxHealth;
        AddModifiers(entity, sign);
        entity.HealByMaxHealthGain(entity.Stats.MaxHealth - maxHealthBefore);
    }

    private void AddModifiers(Entity entity, float sign)
    {
        foreach (var modifier in modifiers)
        {
            float value = modifier.value * sign;
            switch (modifier.target)
            {
                case AugmentTarget.Player:
                    AddStat(entity.Stats, modifier.playerStat, value, modifier.type);
                    break;
                case AugmentTarget.Gun:
                    AddStat(entity.GunStats, modifier.gunStat, value, modifier.type);
                    break;
                case AugmentTarget.Bullet:
                    AddStat(entity.BulletStats, modifier.bulletStat, value, modifier.type);
                    break;
            }
        }
    }

    private void AddStat<T>(StatContainer<T> stats, T statType, float value, ModifierType modifierType) where T : unmanaged, Enum
    {
        bool flat = modifierType == ModifierType.Flat;
        float amount = flat ? value : value / 100f; // Percent: 50 = +50%

        // 라운드/시간 효과는 라운드 보너스에 넣어서 라운드가 바뀌면 함께 초기화되게 함
        if (duration == StatDuration.Permanent)
        {
            if (flat) stats.AddBonusStat(statType, amount);
            else stats.AddMultiplierBonus(statType, amount);
        }
        else
        {
            if (flat) stats.AddRoundBonusStat(statType, amount);
            else stats.AddRoundMultiplierBonus(statType, amount);
        }
    }
}
