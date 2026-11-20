// 조건이 모두 맞아 효과가 실제로 적용된 뒤 알림을 받고 싶은 조건이 구현 (횟수 세기 등)
public interface IAugmentAppliedListener
{
    void OnApplied(AugmentContext context);
}
