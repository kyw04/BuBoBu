using System;

// 증강을 선택한 순간 한 번 발동 (영구 스탯 증가 등)
[Serializable]
public class OnSelectTrigger : IAugmentTrigger
{
    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        fire?.Invoke(new AugmentContext { Caster = owner, Target = owner });
    }

    public void Unbind(Entity owner) { }
}
