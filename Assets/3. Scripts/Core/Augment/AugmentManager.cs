using System.Collections.Generic;
using UnityEngine;

// 플레이어 한 명이 가진 증강 목록과 트리거 바인딩을 관리 (서버 전용)
public class AugmentManager
{
    private class ActiveAugment
    {
        public AugmentData Data;
        public readonly List<IAugmentTrigger> Triggers = new();
    }

    private readonly Entity owner;
    private readonly List<ActiveAugment> actives = new();

    public AugmentManager(Entity owner)
    {
        this.owner = owner;
    }

    public int Count => actives.Count;
    public AugmentData Get(int index) => actives[index].Data;

    public void Add(AugmentData data)
    {
        if (data == null || owner == null) return;

        var active = new ActiveAugment { Data = data };
        actives.Add(active);

        // 트리거는 ScriptableObject에 공유된 인스턴스라서 플레이어/획득마다 복제해서 사용
        foreach (IAugmentTrigger trigger in data.Triggers)
        {
            if (trigger == null) continue;

            IAugmentTrigger instance = Clone(trigger);
            active.Triggers.Add(instance);
            instance.Bind(owner, data.Apply);
        }
    }

    // 모든 증강의 트리거를 해제하고 목록을 비움 (스탯 보너스 초기화는 Entity.ResetStats에서 따로 함)
    public void Clear()
    {
        foreach (ActiveAugment active in actives)
        {
            foreach (IAugmentTrigger trigger in active.Triggers)
                trigger.Unbind(owner);
        }
        actives.Clear();
    }

    private static IAugmentTrigger Clone(IAugmentTrigger source)
        => (IAugmentTrigger)JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType());
}
