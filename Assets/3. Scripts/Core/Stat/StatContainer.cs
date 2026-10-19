using System;
using System.Collections.Generic;
using Fusion;
using Unity.Collections.LowLevel.Unsafe;

public abstract class StatContainerBase : NetworkBehaviour
{
    protected const int MaxStats = 16;

    [Networked, Capacity(MaxStats)] protected NetworkArray<float> Bonus => default;
    [Networked, Capacity(MaxStats)] protected NetworkArray<float> MultiplierBonus => default; // 0 = x1. 0.2 = x0.2
}

public abstract class StatContainer<TStat> : StatContainerBase where TStat : unmanaged, Enum
{
    protected abstract IReadOnlyDictionary<TStat, float> DefaultStats { get; }

    private static int Idx(TStat type) => UnsafeUtility.EnumToInt(type);

    public float GetDefaultStat(TStat type) => DefaultStats.GetValueOrDefault(type, 0f);
    public float GetBonusStat(TStat type) => Bonus[Idx(type)];

    public void SetBonusStat(TStat type, float value) => Bonus.Set(Idx(type), value);
    public void AddBonusStat(TStat type, float value) => Bonus.Set(Idx(type), Bonus[Idx(type)] + value);
    public void SetMultiplierBonus(TStat type, float value) => MultiplierBonus.Set(Idx(type), value);

    public float GetStat(TStat type)
    {
        int i = Idx(type);
        float mul = MultiplierBonus[i] == 0 ? 1f : MultiplierBonus[i];
        return (GetDefaultStat(type) + Bonus[i]) * mul;
    }
}