using System;
using UnityEngine;

public enum BulletPick
{
    EveryNth,       // n발마다 (n, 2n, 3n... 번째 발사)
    NthInMagazine,  // 탄창의 n번째 총알 (n = 1이면 장전 후 첫 발)
    LastInMagazine  // 탄창의 마지막 총알
}

// 트리거가 정한 총알(context.Bullet)이 원하는 총알인지 확인. 총알 관련 트리거(OnFireTrigger, OnStepBubbleTrigger)와 함께 사용
// 산탄은 한 번 쏠 때 나간 총알이 모두 같은 번호라서 함께 조건을 만족함
[Serializable]
public class BulletCondition : IAugmentCondition
{
    [SerializeField] private BulletPick pick = BulletPick.EveryNth;
    [SerializeField, Min(1)] private int n = 3; // LastInMagazine 일 때는 사용 안 함

    public bool Condition(AugmentContext context)
    {
        Bullet bullet = context.Bullet;
        if (bullet == null) return false;

        return pick switch
        {
            BulletPick.EveryNth => bullet.ShotNumber > 0 && bullet.ShotNumber % n == 0,
            BulletPick.NthInMagazine => bullet.MagazineIndex == n,
            BulletPick.LastInMagazine => bullet.IsLastInMagazine,
            _ => false
        };
    }
}
