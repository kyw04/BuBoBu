using System;
using UnityEngine;

// 비전투 상태인지 확인. combatType에 해당하는 전투가 duration초 이상 없으면 비전투
// Attack: 상대를 맞춘 적이 없을 때 / Hit: 맞은 적이 없을 때 / Both: 둘 다 없을 때
[Serializable]
public class OutOfCombatCondition : IAugmentCondition
{
    [SerializeField] private CombatType combatType = CombatType.Both;
    [SerializeField, Min(0f)] private float duration = 3f;

    public bool Condition(AugmentContext context)
        => context.Caster != null && context.Caster.IsOutOfCombat(duration, combatType);
}
