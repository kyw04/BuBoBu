using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierType
{
    Flat,
    Percent
}

[System.Serializable]
public struct AugmentModifier
{
    public AugmentTarget target;
    public PlayerStatType playerStat; // target == Player 일 때 사용
    public GunStatType gunStat;       // target == Gun 일 때 사용
    public BulletStatType bulletStat; // target == Bullet 일 때 사용
    public ModifierType type;
    public float value;            // Bonus에 더해지는 값
}

[System.Serializable]
public class AddStatEffect : IAugmentEffect
{
    [SerializeField] private List<AugmentModifier> modifiers = new();
    
    public void Apply(AugmentContext context)
    {
        foreach (var modifier in modifiers)
        {
            switch (modifier.target)
            {
                case AugmentTarget.Player:
                    AddStat(context.Caster.Stats, modifier.playerStat, modifier.value, modifier.type);
                    break;
                case AugmentTarget.Gun:
                    AddStat(context.Caster.GunStats, modifier.gunStat, modifier.value, modifier.type);
                    break;
                case AugmentTarget.Bullet:
                    AddStat(context.Caster.BulletStats, modifier.bulletStat, modifier.value, modifier.type);
                    break;
            }
        }
    }
    
    private void AddStat<T>(StatContainer<T> stats, T statType, float value, ModifierType modifierType) where T : unmanaged, Enum
    {
        if (modifierType ==  ModifierType.Flat)
            stats.AddBonusStat(statType, value);
        else
            stats.AddMultiplierBonus(statType, value / 100f); // Percent: 50 = +50%
    }
}