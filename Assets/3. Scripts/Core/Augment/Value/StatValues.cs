using System;
using UnityEngine;

// 플레이어 스탯 (현재 체력, 최대 체력, 공격력 등)
[Serializable]
public class PlayerStatValue : IAugmentValue
{
    [SerializeField] private PlayerStatType stat;

    public float GetValue(Entity entity) => entity.Stats.GetStat(stat);
}

// 장착한 총 스탯 (최대 탄약, 재장전 시간 등)
[Serializable]
public class GunStatValue : IAugmentValue
{
    [SerializeField] private GunStatType stat;

    public float GetValue(Entity entity) => entity.Gun != null ? entity.Gun.Stats.GetStat(stat) : 0f;
}

// 장착한 총의 총알 스탯 (속도, 사거리 등)
[Serializable]
public class BulletStatValue : IAugmentValue
{
    [SerializeField] private BulletStatType stat;

    public float GetValue(Entity entity)
        => entity.Gun != null && entity.Gun.BulletStats != null ? entity.Gun.BulletStats.GetStat(stat) : 0f;
}

// 탄창에 남은 총알 수
[Serializable]
public class CurrentAmmoValue : IAugmentValue
{
    public float GetValue(Entity entity) => entity.Gun != null ? entity.Gun.CurrentAmmo : 0f;
}
