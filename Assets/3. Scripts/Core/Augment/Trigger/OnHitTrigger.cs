using System;

// 내 공격이 상대에게 맞을 때마다 발동. Target = 맞은 상대, Value = 들어간 데미지
[Serializable]
public class OnHitTrigger : IAugmentTrigger
{
    private Entity owner;
    private Action<AugmentContext> fire;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.HitEnemy += OnHitEnemy;
    }

    public void Unbind(Entity owner)
    {
        owner.HitEnemy -= OnHitEnemy;
        this.owner = null;
        fire = null;
    }

    private void OnHitEnemy(Entity target, float damage)
    {
        fire?.Invoke(new AugmentContext { Caster = owner, Target = target, Value = damage });
    }
}
