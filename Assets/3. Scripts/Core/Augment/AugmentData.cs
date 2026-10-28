using System.Collections.Generic;
using UnityEngine;

public enum AugmentTarget
{
    Player,
    Gun
}

[System.Serializable]
public struct AugmentModifier
{
    public AugmentTarget target;
    public PlayerStatType playerStat; // target == Player 일 때 사용
    public GunStatType gunStat;       // target == Gun 일 때 사용
    public float value;               // Bonus에 더해지는 값
}

[CreateAssetMenu(fileName = "Augment", menuName = "BuBoBu/Augment/Augment Data")]
public class AugmentData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private List<AugmentModifier> modifiers = new();

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    // 서버(StateAuthority)에서만 호출
    public void Apply(PlayerStats playerStats, GunStats gunStats)
    {
        foreach (AugmentModifier modifier in modifiers)
        {
            switch (modifier.target)
            {
                case AugmentTarget.Player when playerStats != null:
                    playerStats.AddBonusStat(modifier.playerStat, modifier.value);
                    break;
                case AugmentTarget.Gun when gunStats != null:
                    gunStats.AddBonusStat(modifier.gunStat, modifier.value);
                    break;
            }
        }
    }
}
