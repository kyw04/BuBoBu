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
        private ICombatStat stats;
        private Entity entity;

        private float oldRatio;
        private NetworkString<_16> shownName;

        public override void Spawned()
        {
            stats = GetComponentInParent<ICombatStat>();
            entity = GetComponentInParent<Entity>();
        }

        public override void Render()
        {
            UpdateName();

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
    }
}
