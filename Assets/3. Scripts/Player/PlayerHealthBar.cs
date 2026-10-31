using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class PlayerHealthBar : NetworkBehaviour
    {
        [SerializeField] private Image[] healthBar = new Image[4];
        // [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
        // [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);
        private List<ICombatStat> stats;
        // private List<Entity> entities;

        private float oldRatio;
        // private int shownTeam = -1;

        public override void Render()
        {
            // UpdateTeamColor();

            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] == null)
                    continue;
                float ratio = Mathf.Clamp01(stats[i].Health / stats[i].MaxHealth);
                if (Mathf.Approximately(ratio, oldRatio))
                    continue;
                
                oldRatio = ratio;
                healthBar[i].fillAmount = ratio;
            }
        }

        // private void UpdateTeamColor()
        // {
        //     if (entities == null || entities.Team == shownTeam) return;
        //
        //     shownTeam = entities.Team;
        //     Color color = shownTeam == 0 ? team1Color : team2Color;
        //
        //     if (healthBar != null)
        //         healthBar.color = color;
        // }
    }
}
