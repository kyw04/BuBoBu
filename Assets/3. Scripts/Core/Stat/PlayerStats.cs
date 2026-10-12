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

public interface ICombatStat
{
    float Health { get; }
    float MaxHealth { get; }
    float Damage { get; }
    float Defense { get; }
}

public interface IMoveStat
{
    float MoveSpeed { get; }
    float JumpPower { get; }
    float JumpCount { get; }
}

[System.Serializable]
public class PlayerStats : StatContainer<PlayerStatType>, ICombatStat, IMoveStat
{
    [SerializeField] private Dictionary<PlayerStatType, float> defaultStats = new();
    protected override IReadOnlyDictionary<PlayerStatType, float> DefaultStats => defaultStats;

    public float Health => GetStat(PlayerStatType.Health);
    public float MaxHealth => GetStat(PlayerStatType.MaxHealth);
    public float Damage => GetStat(PlayerStatType.Damage);
    public float Defense => GetStat(PlayerStatType.Defense);
    public float MoveSpeed => GetStat(PlayerStatType.Speed);
    public float JumpPower => GetStat(PlayerStatType.JumpPower);
    public float JumpCount => GetStat(PlayerStatType.JumpCount);
}