using Fusion;
using UnityEngine;

namespace Player
{
    public class PlayerColorTag : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer[] tintSpriteRenderers;
        [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
        [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);
        private Entity entity;

        private int shownTeam = -1;

        public override void Spawned()
        {
            entity = GetComponentInParent<Entity>();
        }

        public override void Render()
        {
            if (tintSpriteRenderers == null || entity == null ||
                entity.Team == shownTeam) return;
            
            shownTeam = entity.Team;
            foreach (var tintSpriteRenderer in tintSpriteRenderers)
                tintSpriteRenderer.color = shownTeam == 0 ? team1Color : team2Color;
        }
    }
}
