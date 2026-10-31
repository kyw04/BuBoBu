using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class PlayerHealthBar : NetworkBehaviour
    {
        [SerializeField] private Image healthBar;
        [SerializeField] private TMP_Text nameText; // 체력바 아래 플레이어 이름
        [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
        [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);
        private ICombatStat stats;
        private Entity entity;

        private float oldRatio;
        private NetworkString<_16> shownName;
        private int shownTeam = -1;

        public override void Spawned()
        {
            stats = GetComponentInParent<ICombatStat>();
            entity = GetComponentInParent<Entity>();
        }

        public override void Render()
        {
            UpdateName();
            UpdateTeamColor();

            if (stats == null)
                return;
            float ratio = Mathf.Clamp01(stats.Health / stats.MaxHealth);
            if (Mathf.Approximately(ratio, oldRatio))
                return;

            oldRatio = ratio;
            healthBar.fillAmount = ratio;
        }

        private void UpdateName()
        {
            if (nameText == null || entity == null) return;
            if (entity.PlayerName == shownName) return;

            shownName = entity.PlayerName;
            nameText.text = shownName.ToString();
        }

        private void UpdateTeamColor()
        {
            if (entity == null || entity.Team == shownTeam) return;

            shownTeam = entity.Team;
            Color color = shownTeam == 0 ? team1Color : team2Color;

            if (healthBar != null)
                healthBar.color = color;
            if (nameText != null)
                nameText.color = color;
        }
    }
}
