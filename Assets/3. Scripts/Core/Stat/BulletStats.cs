using System.Collections.Generic;
using UnityEngine;

public enum BulletStatType
{
    Speed,
    Range,
    UpRange,
    LifTime,
    Size
}

[System.Serializable]
public class BulletStats : StatContainer<BulletStatType>
{
    [SerializeField] private Dictionary<BulletStatType, float> defaultStats = new();
    protected override IReadOnlyDictionary<BulletStatType, float> DefaultStats => defaultStats;
}