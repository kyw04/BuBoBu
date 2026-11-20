using System;
using UnityEngine;

// 라운드 중 살아 있는 동안 interval초마다 발동 (죽어 있거나 라운드 밖이면 시간이 흐르지 않음)
[Serializable]
public class IntervalTrigger : IAugmentTrigger
{
    [SerializeField, Min(0.1f)] private float interval = 1f;
    [SerializeField] private bool resetOnRoundStart = true; // 라운드가 바뀌면 쌓인 시간을 처음부터

    private Entity owner;
    private Action<AugmentContext> fire;
    private float elapsed;
    private int lastRound = -1;

    public void Bind(Entity owner, Action<AugmentContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        owner.Ticked += OnTick;
    }

    public void Unbind(Entity owner)
    {
        owner.Ticked -= OnTick;
        this.owner = null;
        fire = null;
    }

    private void OnTick()
    {
        if (owner == null || !owner.InRound) return;

        if (resetOnRoundStart && owner.Round != lastRound)
        {
            lastRound = owner.Round;
            elapsed = 0f;
        }

        elapsed += owner.Runner.DeltaTime;
        while (elapsed >= interval)
        {
            elapsed -= interval;
            fire?.Invoke(new AugmentContext { Caster = owner, Target = owner });
        }
    }
}
