using System;

// 증강 발동 시점. AugmentManager가 증강을 얻을 때마다 트리거를 복제해서 Bind하므로
// 구독한 이벤트 핸들러 같은 상태를 필드에 저장해도 다른 플레이어/중복 증강과 섞이지 않음
public interface IAugmentTrigger
{
    // 발동 조건이 되는 이벤트를 구독하고, 발동할 때마다 fire(context)를 호출
    void Bind(Entity owner, Action<AugmentContext> fire);
    // Bind에서 구독한 것을 해제
    void Unbind(Entity owner);
}
