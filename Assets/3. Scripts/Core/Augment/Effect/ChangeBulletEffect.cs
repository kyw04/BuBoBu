using System;
using Fusion;
using UnityEngine;

// 대상이 쏘는 총알을 다른 총알 프리팹으로 교체
// 프리팹에는 Bullet과 BulletStats 컴포넌트가 있어야 함. 같은 대상에 여러 번 적용되면 마지막에 적용된 총알을 사용
// Round/Timed는 Permanent보다 우선하고, 끝나면 Permanent로 바꾼 총알(없으면 기본 총알)로 돌아감
[Serializable]
public class ChangeBulletEffect : IAugmentEffect
{
    [SerializeField] private AugmentSubject subject = AugmentSubject.Caster; // 총알을 바꿀 대상
    [SerializeField] private NetworkPrefabRef bulletPrefab;
    [SerializeField] private StatDuration duration = StatDuration.Permanent;
    [SerializeField, Min(0.1f)] private float seconds = 3f; // duration == Timed 일 때 유지 시간

    public void Apply(AugmentContext context)
    {
        Entity entity = context.Get(subject);
        Gun gun = entity != null ? entity.Gun : null;
        if (gun == null || !bulletPrefab.IsValid) return;

        gun.SetBullet(bulletPrefab, duration != StatDuration.Permanent);

        if (duration == StatDuration.Timed)
        {
            NetworkPrefabRef prefab = bulletPrefab;
            entity.AddTimedRevert(seconds, () => gun.ClearRoundBullet(prefab));
        }
    }
}
