using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 러너 프리팹에 붙여 StartGameArgs.ObjectProvider로 넘김. 러너마다 따로 풀을 가지므로 방(러너)별로 분리됨
// IPoolable을 가진 프리팹(총알 등)만 재사용하고 나머지는 기본 동작(Instantiate/Destroy)
public class NetworkObjectPool : NetworkObjectProviderDefault
{
    [SerializeField] private int maxPooledPerPrefab = 64;

    private readonly Dictionary<NetworkPrefabId, Stack<NetworkObject>> pools = new();

    public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject instance)
    {
        if (TryPop(context.PrefabId, out instance))
        {
            instance.gameObject.SetActive(true);
            if (context.DontDestroyOnLoad)
                runner.MakeDontDestroyOnLoad(instance.gameObject);
            else
                runner.MoveToRunnerScene(instance.gameObject);

            runner.Prefabs.AddInstance(context.PrefabId); // 기본 구현과 동일하게 인스턴스 수 집계
            return NetworkObjectAcquireResult.Success;
        }

        return base.AcquirePrefabInstance(runner, context, out instance);
    }

    protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        if (!instance.TryGetComponent(out IPoolable poolable))
        {
            base.DestroyPrefabInstance(runner, prefabId, instance);
            return;
        }

        if (!pools.TryGetValue(prefabId, out var pool))
            pools[prefabId] = pool = new Stack<NetworkObject>();

        if (pool.Count >= maxPooledPerPrefab)
        {
            base.DestroyPrefabInstance(runner, prefabId, instance);
            return;
        }

        poolable.OnReturnToPool();
        instance.gameObject.SetActive(false);
        pool.Push(instance);
    }

    private bool TryPop(NetworkPrefabId prefabId, out NetworkObject instance)
    {
        instance = null;
        if (!pools.TryGetValue(prefabId, out var pool)) return false;

        while (pool.Count > 0)
        {
            instance = pool.Pop();
            if (instance != null) return true; // 씬 전환 등으로 파괴된 항목은 건너뜀
        }
        return false;
    }
}
