using System.Collections.Generic;
using UnityEngine;

public enum BulletStatType
{
    Speed,
    Range,
    LifTime,
}

[System.Serializable]
public class BulletStat : StatContainer<BulletStatType>
{
    [SerializeField] private Dictionary<BulletStatType, float> defaultStats = new();
    protected override Dictionary<BulletStatType, float> DefaultStats => defaultStats;
}