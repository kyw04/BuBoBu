using Fusion;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerFire : NetworkBehaviour
    {
        [SerializeField] private Gun gun;
        private PlayerInputReader inputReader;

        public override void Spawned()
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!inputReader.HasInput) return;

            gun.Aim(inputReader.AimPoint);

            if (!Runner.IsServer) return;
            if (!inputReader.AttackPressed) return;
            
            gun.Fire();
        }
    }
}
