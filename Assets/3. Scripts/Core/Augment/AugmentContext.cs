public enum AugmentSubject
{
    Caster, // 증강을 가진 플레이어
    Target  // 트리거가 정한 대상 (맞은 상대 등)
}

public class AugmentContext
{
    public Entity Caster {  get; set; }
    public Entity Target {  get; set; }
    public float Value { get; set; }
    public Bullet Bullet { get; set; } // 총알 관련 트리거가 정한 총알 (발사한 총알, 밟은 거품 등)

    public Entity Get(AugmentSubject subject) => subject == AugmentSubject.Caster ? Caster : Target;
}