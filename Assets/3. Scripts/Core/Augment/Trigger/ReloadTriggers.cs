using System;

// 장전을 시작할 때 발동 (탄약을 다 써서 자동 장전되는 경우 포함)
[Serializable]
public class OnReloadStartTrigger : IAugmentTrigger
{
    private Entity owner;
    private Action<AugmentContext> fire;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.ReloadStarted += OnReloadStarted;
    }

    public void Unbind(Entity owner)
    {
        owner.ReloadStarted -= OnReloadStarted;
        this.owner = null;
        fire = null;
    }

    private void OnReloadStarted()
        => fire?.Invoke(new AugmentContext { Caster = owner, Target = owner });
}

// 장전이 끝나 탄약이 다시 찼을 때 발동
[Serializable]
public class OnReloadCompleteTrigger : IAugmentTrigger
{
    private Entity owner;
    private Action<AugmentContext> fire;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.ReloadCompleted += OnReloadCompleted;
    }

    public void Unbind(Entity owner)
    {
        owner.ReloadCompleted -= OnReloadCompleted;
        this.owner = null;
        fire = null;
    }

    private void OnReloadCompleted()
        => fire?.Invoke(new AugmentContext { Caster = owner, Target = owner });
}
