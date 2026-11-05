using System.Collections.Generic;
using UnityEngine;

public enum GunStatType
{
    Ammo,
    Range,
    ReloadTime,
    FireSpeed,
    BulletCount,
    DamageMultiplier
}

public interface IDamageMultiplier
{
    float DamageMultiplier { get; }
}

[System.Serializable]
public class GunStats : StatContainer<GunStatType>, IDamageMultiplier
{
    [SerializeField] private Dictionary<GunStatType, float> defaultStats = new();
    private readonly Dictionary<GunStatType, float> gunStats = new(); // 기본 스탯 + 선택한 총 스탯
    private GunData data;

    protected override IReadOnlyDictionary<GunStatType, float> DefaultStats => data != null ? gunStats : defaultStats;
    public float DamageMultiplier => GetStat(GunStatType.DamageMultiplier);

    // 총을 바꾸면 기본 스탯을 그 총 값으로 덮어씀. null이면 원래 기본 총
    public void SetData(GunData gunData)
    {
        data = gunData;
        if (data == null) return;

        gunStats.Clear();
        foreach (KeyValuePair<GunStatType, float> pair in defaultStats)
            gunStats[pair.Key] = pair.Value;
        foreach (KeyValuePair<GunStatType, float> pair in data.Stats)
            gunStats[pair.Key] = pair.Value;
    }
}

