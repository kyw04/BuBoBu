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

// 자신의 거품(총알)을 밟았을 때 발동. Bullet = 밟은 거품, Target = 거품을 쏜 플레이어
// includeAllyBubbles를 켜면 같은 팀이 쏜 거품을 밟아도 발동
[Serializable]
public class OnStepBubbleTrigger : IAugmentTrigger
{
    [UnityEngine.SerializeField] private bool includeAllyBubbles;

    private Entity owner;
    private Action<AugmentContext> fire;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.BubbleStepped += OnBubbleStepped;
    }

    public void Unbind(Entity owner)
    {
        owner.BubbleStepped -= OnBubbleStepped;
        this.owner = null;
        fire = null;
    }

    private void OnBubbleStepped(Bullet bullet)
    {
        if (!includeAllyBubbles && bullet.Owner != owner) return;
        fire?.Invoke(new AugmentContext { Caster = owner, Target = bullet.Owner, Bullet = bullet });
    }
}
