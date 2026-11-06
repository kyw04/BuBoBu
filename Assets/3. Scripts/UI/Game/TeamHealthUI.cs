using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 화면 위쪽 팀별 플레이어 슬롯에 체력을 표시. 점수판 Canvas에 붙음
// 내 팀은 나를 첫 슬롯에, 나머지는 입장 순서(PlayerId)대로 슬롯을 채우고, 비어 있는 슬롯은 숨김
public class TeamHealthUI : MonoBehaviour
{
    [System.Serializable]
    public struct Slot
    {
        public CanvasGroup group; // 슬롯 전체 (빈 슬롯이면 숨김)
        public Image healthBar;   // Filled 이미지
    }

    [SerializeField] private GameManager game; // 같은 씬의 GameManager
    [SerializeField] private Slot[] team1Slots;
    [SerializeField] private Slot[] team2Slots;
    [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
    [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);

    private readonly List<Entity> team1 = new();
    private readonly List<Entity> team2 = new();

    // 로컬 플레이어가 맨 앞, 그 외에는 입장 순서
    private static readonly System.Comparison<Entity> BySlotOrder = (a, b) =>
    {
        bool aLocal = a.Object.HasInputAuthority;
        bool bLocal = b.Object.HasInputAuthority;
        if (aLocal != bLocal) return aLocal ? -1 : 1;
        return a.Object.InputAuthority.PlayerId.CompareTo(b.Object.InputAuthority.PlayerId);
    };

    private void Update()
    {
        if (game == null || !game.Object || !game.Object.IsValid) return;

        team1.Clear();
        team2.Clear();
        foreach (Entity entity in game.Players)
        {
            if (entity == null || !entity.Object || !entity.Object.IsValid) continue;
            (entity.Team == 0 ? team1 : team2).Add(entity);
        }
        team1.Sort(BySlotOrder);
        team2.Sort(BySlotOrder);

        Apply(team1Slots, team1, team1Color);
        Apply(team2Slots, team2, team2Color);
    }

    private void Apply(Slot[] slots, List<Entity> members, Color color)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Entity entity = i < members.Count ? members[i] : null;
            Slot slot = slots[i];

            if (slot.group != null)
                slot.group.alpha = entity != null ? 1f - i * 0.75f : 0f;

            if (slot.healthBar == null) continue;

            slot.healthBar.color = color;
            slot.healthBar.fillAmount = entity == null ? 1f
                : entity.IsDead ? 0f
                : Mathf.Clamp01(entity.stats.Health / Mathf.Max(entity.stats.MaxHealth, 1f));
        }
    }
}
