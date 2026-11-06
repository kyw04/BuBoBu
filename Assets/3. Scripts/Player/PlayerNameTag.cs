using Fusion;
using TMPro;
using UnityEngine;

namespace Player
{
    // 플레이어 아래 이름표. 체력은 화면 위 TeamHealthUI에서 표시
    public class PlayerNameTag : NetworkBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
        [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);
        private Entity entity;

        private NetworkString<_16> shownName;
        private int shownTeam = -1;

        public override void Spawned()
        {
            entity = GetComponentInParent<Entity>();
        }

        public override void Render()
        {
            if (nameText == null || entity == null) return;

            if (entity.PlayerName != shownName)
            {
                shownName = entity.PlayerName;
                nameText.text = shownName.ToString();
            }

            if (entity.Team != shownTeam)
            {
                shownTeam = entity.Team;
                nameText.color = shownTeam == 0 ? team1Color : team2Color;
            }
        }
    }
}
