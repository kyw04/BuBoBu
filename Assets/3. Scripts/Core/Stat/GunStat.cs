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
public class GunStat : StatContainer<GunStatType>
{
    [SerializeField] private Dictionary<GunStatType, float> defaultStats = new();
    protected override Dictionary<GunStatType, float> DefaultStats => defaultStats;
}

