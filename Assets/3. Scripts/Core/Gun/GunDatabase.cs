using System.Collections.Generic;
using UnityEngine;

// 서버와 클라이언트가 같은 인덱스로 총을 식별하므로 리스트 순서를 바꾸면 안 됨
[CreateAssetMenu(fileName = "GunDatabase", menuName = "BuBoBu/Gun/Gun Database")]
public class GunDatabase : ScriptableObject
{
    [SerializeField] private List<GunData> guns = new();

    private readonly List<int> pool = new();

    public int Count => guns.Count;

    public GunData Get(int index)
        => index >= 0 && index < guns.Count ? guns[index] : null;

    // 중복 없이 뽑음. 개수가 부족하면 남는 칸은 -1
    public void PickRandom(int[] result)
    {
        pool.Clear();
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] != null)
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
