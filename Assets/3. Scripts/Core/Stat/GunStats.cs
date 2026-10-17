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
    protected override IReadOnlyDictionary<GunStatType, float> DefaultStats => defaultStats;
    public float DamageMultiplier => GetStat(GunStatType.DamageMultiplier);
}

