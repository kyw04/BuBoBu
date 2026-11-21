using System;

// 총알을 쏠 때마다 발동 (산탄이면 총알마다). Bullet = 쏜 총알
// 스폰 직전에 발동하므로 BulletStatEffect로 바꾼 스탯이 처음부터 적용됨
[Serializable]
public class OnFireTrigger : IAugmentTrigger
{
    private Entity owner;
    private Action<AugmentContext> fire;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.BulletFired += OnBulletFired;
    }

    public void Unbind(Entity owner)
    {
        owner.BulletFired -= OnBulletFired;
        this.owner = null;
        fire = null;
    }

    private void OnBulletFired(Bullet bullet)
    {
        fire?.Invoke(new AugmentContext { Caster = owner, Bullet = bullet });
    }
}
