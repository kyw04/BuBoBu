using Fusion;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerFire : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float fireCooldown = 0.25f;
        [SerializeField] private float bulletSpeed = 12f;

        [Networked] private TickTimer CooldownTimer { get; set; }

        private PlayerInputReader inputReader;

        public override void Spawned()
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        public override void FixedUpdateNetwork()
        {
            // Runner.Spawn must only be called by the State Authority (see PlayerSpawner for the same guard);
            // without this, the input-authority client would also attempt to spawn during prediction.
            if (!Runner.IsServer) return;
            if (!inputReader.HasInput) return;
            if (!inputReader.AttackPressed) return;
            if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

            CooldownTimer = TickTimer.CreateFromSeconds(Runner, fireCooldown);

            float facingSign = spriteRenderer.flipX ? -1f : 1f;
            Vector2 direction = new Vector2(facingSign, 0f);
            Vector3 localOffset = firePoint.localPosition;
            Vector3 spawnPosition = transform.position + new Vector3(Mathf.Abs(localOffset.x) * facingSign, localOffset.y, localOffset.z);

            Runner.Spawn(bulletPrefab, spawnPosition, onBeforeSpawned: (runner, obj) =>
            {
                obj.GetComponent<Bullet>().Initialize(runner, direction, bulletSpeed);
            });
        }
    }
}
