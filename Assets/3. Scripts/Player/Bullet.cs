using Fusion;
using UnityEngine;

namespace Player
{
    public class Bullet : NetworkBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 2f;

        [Networked] private Vector2 Direction { get; set; }
        [Networked] private float Speed { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }

        public void Initialize(NetworkRunner runner, Vector2 direction, float speed)
        {
            Direction = direction;
            Speed = speed;
            LifeTimer = TickTimer.CreateFromSeconds(runner, lifetimeSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (LifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            transform.position += (Vector3)(Direction * Speed * Runner.DeltaTime);
        }
    }
}
