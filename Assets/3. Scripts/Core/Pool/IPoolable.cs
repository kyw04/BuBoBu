// NetworkObjectPool이 재사용할 네트워크 오브젝트에 붙이는 인터페이스
// 네트워크 상태는 Spawned/onBeforeSpawned에서 다시 설정되므로, 여기선 로컬(비네트워크) 상태만 정리
public interface IPoolable
{
    void OnReturnToPool();
}
