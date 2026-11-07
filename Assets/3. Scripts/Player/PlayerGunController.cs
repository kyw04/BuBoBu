using Fusion;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerGunController : NetworkBehaviour
    {
        [SerializeField] private Gun gun;
        private PlayerInputReader inputReader;

        public override void Spawned()
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!inputReader.HasInput)
                return;

            gun.Aim(inputReader.AimPoint);

            if (!Runner.IsServer)
                return;

            if (inputReader.AttackPressed)
                gun.Fire();

            if (inputReader.ReloadPressed)
                gun.Reload();
        }
    }
}
