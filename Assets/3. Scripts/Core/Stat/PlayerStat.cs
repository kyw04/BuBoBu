using System.Collections.Generic;
using UnityEngine;

public enum PlayerStatType
{
    Health,
    MaxHealth,
    Damage,
    Defense,
    Speed,
    JumpPower,
    JumpCount,
}

[System.Serializable]
public class PlayerStat : StatContainer<PlayerStatType>
{
    [SerializeField] private Dictionary<PlayerStatType, float> defaultStats = new();
    protected override Dictionary<PlayerStatType, float> DefaultStats => defaultStats;
}