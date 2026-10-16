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
            // Runner.Spawn must only be called by the State Authority (see PlayerSpawner for the same guard);
            // without this, the input-authority client would also attempt to spawn during prediction.
            if (!Runner.IsServer) return;
            if (!inputReader.HasInput) return;
            if (!inputReader.AttackPressed) return;
            
            gun.Fire();
        }
    }
}
