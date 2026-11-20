using System;
using UnityEngine;

// 효과가 정해진 횟수만큼만 적용되도록 제한. 다른 조건까지 모두 맞아 실제로 적용된 경우만 셈
// AugmentManager가 플레이어/획득마다 복제하므로 횟수는 증강마다 따로 셈
[Serializable]
public class CountCondition : IAugmentCondition, IAugmentAppliedListener
{
    [SerializeField, Min(1)] private int maxCount = 1;
    [SerializeField] private bool resetEachRound; // 켜면 라운드마다 횟수 초기화

    private int count;
    private int countedRound = -1;

    public bool Condition(AugmentContext context)
    {
        RefreshRound(context);
        return count < maxCount;
    }

    public void OnApplied(AugmentContext context)
    {
        RefreshRound(context);
        count++;
    }

    private void RefreshRound(AugmentContext context)
    {
        if (!resetEachRound || context.Caster == null) return;

        int round = context.Caster.Round;
        if (round == countedRound) return;

        countedRound = round;
        count = 0;
    }
}
