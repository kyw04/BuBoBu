using System.Collections.Generic;
using UnityEngine;

// 서버와 클라이언트가 같은 인덱스로 증강을 식별하므로 리스트 순서를 바꾸면 안 됨
[CreateAssetMenu(fileName = "AugmentDatabase", menuName = "BuBoBu/Augment/Augment Database")]
public class AugmentDatabase : ScriptableObject
{
    [SerializeField] private List<AugmentData> augments = new();

    public int Count => augments.Count;

    public AugmentData Get(int index)
        => index >= 0 && index < augments.Count ? augments[index] : null;

    // 중복 없이 count개 인덱스를 뽑음. 개수가 부족하면 남는 칸은 -1
    public void PickRandom(int[] result)
    {
        List<int> pool = new(augments.Count);
        for (int i = 0; i < augments.Count; i++)
            pool.Add(i);

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
