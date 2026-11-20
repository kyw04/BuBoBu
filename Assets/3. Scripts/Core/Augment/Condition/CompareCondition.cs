using System;
using UnityEngine;

public enum CompareType
{
    LessOrEqual,    // <=
    Less,           // <
    GreaterOrEqual, // >=
    Greater,        // >
    Equal           // ==
}

// value를 threshold와 비교. divideBy를 지정하면 value / divideBy를 퍼센트(50 = 50%)로 비교
// 예) 체력 50% 이하: value = 플레이어 스탯(Health), divideBy = 플레이어 스탯(MaxHealth), LessOrEqual, 50
// 예) 남은 총알 2발 이하: value = 현재 탄약, divideBy = 없음, LessOrEqual, 2
[Serializable]
public class CompareCondition : IAugmentCondition
{
    [SerializeField] private AugmentSubject subject = AugmentSubject.Caster;
    [SerializeReference, SubclassSelector] private IAugmentValue value;
    [SerializeReference, SubclassSelector] private IAugmentValue divideBy; // 없으면 값 그대로 비교
    [SerializeField] private CompareType compare = CompareType.LessOrEqual;
    [SerializeField] private float threshold = 50f;

    public bool Condition(AugmentContext context)
    {
        Entity entity = context.Get(subject);
        if (entity == null || value == null) return false;

        float current = value.GetValue(entity);
        if (divideBy != null)
        {
            float max = divideBy.GetValue(entity);
            if (max <= 0f) return false;
            current = current / max * 100f;
        }

        return compare switch
        {
            CompareType.LessOrEqual => current <= threshold,
            CompareType.Less => current < threshold,
            CompareType.GreaterOrEqual => current >= threshold,
            CompareType.Greater => current > threshold,
            CompareType.Equal => Mathf.Approximately(current, threshold),
            _ => false
        };
    }
}
