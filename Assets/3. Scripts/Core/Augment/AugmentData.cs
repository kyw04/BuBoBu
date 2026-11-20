using System.Collections.Generic;
using UnityEngine;

// 값 순서가 네트워크로 전송되는 int 값이므로 중간에 끼워 넣지 말고 뒤에 추가할 것
public enum AugmentRank
{
    Silver,
    Gold,
    Platinum
}

public enum AugmentTarget
{
    Player,
    Gun,
    Bullet,
    Other
}

[CreateAssetMenu(fileName = "Augment", menuName = "BuBoBu/Augment/Augment Data")]
public class AugmentData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private AugmentRank rank;

    [SerializeReference, SubclassSelector] private List<IAugmentTrigger> triggers = new();
    [SerializeReference, SubclassSelector] private List<IAugmentCondition> conditions = new();
    [SerializeReference, SubclassSelector] private List<IAugmentEffect> effects = new();
    
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public AugmentRank Rank => rank;
    public IReadOnlyList<IAugmentTrigger> Triggers => triggers;
    public IReadOnlyList<IAugmentCondition> Conditions => conditions;

    // 트리거가 발동했을 때 호출. 조건이 모두 맞으면(조건이 없으면 항상) 효과 적용
    // 조건은 횟수 같은 상태를 가질 수 있어서 AugmentManager가 플레이어별로 복제한 목록을 넘김
    public void Apply(AugmentContext context, IReadOnlyList<IAugmentCondition> activeConditions)
    {
        if (!CheckConditions(context, activeConditions)) return;

        foreach (IAugmentEffect effect in effects)
            effect?.Apply(context);

        foreach (IAugmentCondition condition in activeConditions)
        {
            if (condition is IAugmentAppliedListener listener)
                listener.OnApplied(context);
        }
    }

    private static bool CheckConditions(AugmentContext context, IReadOnlyList<IAugmentCondition> activeConditions)
    {
        foreach (IAugmentCondition condition in activeConditions)
        {
            if (condition != null && !condition.Condition(context))
                return false;
        }
        return true;
    }
}
