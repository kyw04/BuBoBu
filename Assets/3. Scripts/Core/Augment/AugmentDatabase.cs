using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AugmentRankSetting
{
    public AugmentRank rank;
    public string displayName;
    public Color color;
    public Sprite frame; // 증강 카드 배경으로 쓸 랭크 프레임
    [Min(0)] public float weight; // 게임 시작 시 랭크 순서를 뽑을 때의 가중치
}

// 서버와 클라이언트가 같은 인덱스로 증강을 식별하므로 리스트 순서를 바꾸면 안 됨
[CreateAssetMenu(fileName = "AugmentDatabase", menuName = "BuBoBu/Augment/Augment Database")]
public class AugmentDatabase : ScriptableObject
{
    [SerializeField] private List<AugmentData> augments = new();
    [SerializeField] private List<AugmentRankSetting> rankSettings = new()
    {
        new AugmentRankSetting { rank = AugmentRank.Silver, displayName = "실버", color = new Color(0.75f, 0.78f, 0.82f), weight = 50f },
        new AugmentRankSetting { rank = AugmentRank.Gold, displayName = "골드", color = new Color(1f, 0.78f, 0.25f), weight = 35f },
        new AugmentRankSetting { rank = AugmentRank.Platinum, displayName = "플래티넘", color = new Color(0.55f, 0.9f, 0.85f), weight = 15f },
    };

    private readonly List<int> pool = new();

    public int Count => augments.Count;

    public AugmentData Get(int index)
        => index >= 0 && index < augments.Count ? augments[index] : null;

    public bool HasRank(AugmentRank rank)
    {
        foreach (AugmentData augment in augments)
        {
            if (augment != null && augment.Rank == rank)
                return true;
        }
        return false;
    }

    public bool TryGetRankSetting(AugmentRank rank, out AugmentRankSetting setting)
    {
        foreach (AugmentRankSetting s in rankSettings)
        {
            if (s.rank != rank) continue;
            setting = s;
            return true;
        }

        setting = default;
        return false;
    }

    // 서버 전용: 가중치에 따라 랭크 하나를 뽑음. 해당 랭크 증강이 하나도 없으면 후보에서 제외
    public AugmentRank RollRank()
    {
        float total = 0f;
        foreach (AugmentRankSetting s in rankSettings)
        {
            if (s.weight > 0f && HasRank(s.rank))
                total += s.weight;
        }

        if (total <= 0f)
        {
            // 가중치 설정이 비어 있으면 증강이 있는 아무 랭크나 사용
            foreach (AugmentData augment in augments)
            {
                if (augment != null)
                    return augment.Rank;
            }
            return default;
        }

        float roll = Random.Range(0f, total);
        AugmentRank last = default;
        foreach (AugmentRankSetting s in rankSettings)
        {
            if (s.weight <= 0f || !HasRank(s.rank)) continue;

            last = s.rank;
            roll -= s.weight;
            if (roll < 0f)
                return s.rank;
        }
        return last;
    }

    // 해당 랭크 증강 중에서 중복 없이 뽑음. 개수가 부족하면 남는 칸은 -1
    public void PickRandom(AugmentRank rank, int[] result)
    {
        pool.Clear();
        for (int i = 0; i < augments.Count; i++)
        {
            if (augments[i] != null && augments[i].Rank == rank)
                pool.Add(i);
        }

        for (int i = 0; i < result.Length; i++)
        {
            if (pool.Count == 0)
            {
                result[i] = -1;
                continue;
            }

            int pick = Random.Range(0, pool.Count);
            result[i] = pool[pick];
            pool.RemoveAt(pick);
        }
    }
}
