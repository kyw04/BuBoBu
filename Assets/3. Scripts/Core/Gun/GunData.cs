using System.Collections.Generic;
using UnityEngine;

// 총 증강으로 고를 수 있는 총. 고르면 플레이어의 Gun에 기본 스탯과 모양을 덮어씀
[CreateAssetMenu(fileName = "Gun", menuName = "BuBoBu/Gun/Gun Data")]
public class GunData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;   // 선택 카드에 표시
    [SerializeField] private Sprite sprite; // 플레이어가 들고 있는 총 모양. 비우면 기본 총 모양 유지
    [SerializeField] private Dictionary<GunStatType, float> stats = new(); // 여기 없는 스탯은 기본 총 값 사용

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public Sprite Sprite => sprite;
    public IReadOnlyDictionary<GunStatType, float> Stats => stats;
}
