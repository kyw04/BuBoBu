using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class StatContainer<TStatEnum> : MonoBehaviour where TStatEnum : Enum
{
    protected abstract Dictionary<TStatEnum, float> DefaultStats { get; }
    private readonly  Dictionary<TStatEnum, float> bonusStats = new();
    private readonly  Dictionary<TStatEnum, float> multiplierStats = new();

    public void SetDefaultStat(TStatEnum type, float value) => DefaultStats[type] = value;
    public void SetBonusStat(TStatEnum type, float value) => bonusStats[type] = value;
    public void SetMultiplierStat(TStatEnum type, float value) => multiplierStats[type] = value;
    public float GetDefaultStat(TStatEnum type) => DefaultStats.GetValueOrDefault(type, 0f);
    public float GetBonusStat(TStatEnum type) => bonusStats.GetValueOrDefault(type, 0f);

    public void AddBonusStat(TStatEnum type, float value)
    {
        bonusStats.TryGetValue(type, out var current);
        bonusStats[type] = current + value;
    }

    public float GetStat(TStatEnum type)
    {
        DefaultStats.TryGetValue(type, out var baseValue);
        bonusStats.TryGetValue(type, out var bonus);
        multiplierStats.TryGetValue(type, out var multiplier);
        return (baseValue + bonus) * multiplier;
    }
}
