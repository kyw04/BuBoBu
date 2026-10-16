using System.Collections.Generic;
using UnityEngine;

public enum GunStatType
{
    Ammo,
    Range,
    ReloadTime,
    FireSpeed,
    BulletCount
}

[System.Serializable]
public class GunStats : StatContainer<GunStatType>
{
    [SerializeField] private Dictionary<GunStatType, float> defaultStats = new();
    protected override IReadOnlyDictionary<GunStatType, float> DefaultStats => defaultStats;
}

