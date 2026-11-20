using System;
using System.Collections.Generic;
using Fusion;
using Unity.Collections.LowLevel.Unsafe;

public abstract class StatContainerBase : NetworkBehaviour
{
    protected const int MaxStats = 16;

    [Networked, Capacity(MaxStats)] protected NetworkArray<float> Bonus => default;
    [Networked, Capacity(MaxStats)] protected NetworkArray<float> MultiplierBonus => default; // 더해지는 비율. 0 = x1, 0.5 = x1.5, -0.2 = x0.8
    // 이번 라운드에만 적용되는 보너스. 라운드가 시작될 때 ResetRoundBonuses로 초기화
    [Networked, Capacity(MaxStats)] protected NetworkArray<float> RoundBonus => default;
    [Networked, Capacity(MaxStats)] protected NetworkArray<float> RoundMultiplierBonus => default;

    public void ResetBonuses()
    {
        for (int i = 0; i < MaxStats; i++)
        {
            Bonus.Set(i, 0f);
            MultiplierBonus.Set(i, 0f);
        }
        ResetRoundBonuses();
    }

    public void ResetRoundBonuses()
    {
        for (int i = 0; i < MaxStats; i++)
        {
            RoundBonus.Set(i, 0f);
            RoundMultiplierBonus.Set(i, 0f);
        }
    }
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
    public void AddMultiplierBonus(TStat type, float value) => MultiplierBonus.Set(Idx(type), MultiplierBonus[Idx(type)] + value);
    public void AddRoundBonusStat(TStat type, float value) => RoundBonus.Set(Idx(type), RoundBonus[Idx(type)] + value);
    public void AddRoundMultiplierBonus(TStat type, float value) => RoundMultiplierBonus.Set(Idx(type), RoundMultiplierBonus[Idx(type)] + value);

    public float GetStat(TStat type)
    {
        int i = Idx(type);
        float mul = UnityEngine.Mathf.Max(0f, 1f + MultiplierBonus[i] + RoundMultiplierBonus[i]);
        return (GetDefaultStat(type) + Bonus[i] + RoundBonus[i]) * mul;
    }
}