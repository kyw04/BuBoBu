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
    Size,       // 플레이어 크기 배율 (기본 1)
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
    protected override float FallbackDefault(PlayerStatType type)
        => type == PlayerStatType.Size ? 1f : 0f;

    public float Health => GetStat(PlayerStatType.Health);
    public float MaxHealth => GetStat(PlayerStatType.MaxHealth);
    public float Damage => GetStat(PlayerStatType.Damage);
    public float Defense => GetStat(PlayerStatType.Defense);
    public float MoveSpeed => GetStat(PlayerStatType.Speed);
    public float JumpPower => GetStat(PlayerStatType.JumpPower);
    public float JumpCount => GetStat(PlayerStatType.JumpCount);
    public float Size => GetStat(PlayerStatType.Size);
}