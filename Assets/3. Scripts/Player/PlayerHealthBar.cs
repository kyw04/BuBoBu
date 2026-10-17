using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class PlayerHealthBar : NetworkBehaviour
    {
        [SerializeField] private Image healthBar;
        private ICombatStat stats;

        private float oldRatio;
        
        public override void Spawned()
        {
            stats = GetComponentInParent<ICombatStat>();
        }

        public override void Render()
        {
            if (stats == null)
                return;
            float ratio = Mathf.Clamp01(stats.Health / stats.MaxHealth);
            if (Mathf.Approximately(ratio, oldRatio))
                return;
            
            healthBar.fillAmount = ratio;
        }
    }
}
