// 조건에서 비교할 값을 엔티티에서 읽어옴 (스탯, 현재 탄약 등). 새 값이 필요하면 구현 클래스를 추가
public interface IAugmentValue
{
    float GetValue(Entity entity);
}
