using Fusion;
using UnityEngine;

namespace Player
{
    // Must run before PlayerFire each tick: PlayerFire reads spriteRenderer.flipX (set below) to pick a
    // firing direction, via GetComponent rather than an event, so a stale flipX would fire one tick behind
    // on a turn+attack tick. Runs after PlayerInputReader (-100), before default-order (0) PlayerFire.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(IMoveStat))]
    [DefaultExecutionOrder(-50)]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveAcceleration = 50f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        // maxFallSpeed * Runner.DeltaTime must stay below groundCheckRadius to avoid tunneling through the ground on landing.
        [SerializeField, Min(0f)] private float maxFallSpeed = 8f;

        private Rigidbody2D rb;
        private PlayerInputReader inputReader;
        private IMoveStat stat;
        
        public bool IsGrounded => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        public override void Spawned()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravity / Physics2D.gravity.y;

            inputReader = GetComponent<PlayerInputReader>();
            stat = GetComponent<IMoveStat>();
            Debug.Log($"stat -> move speed: {stat.MoveSpeed}");
        }

        public override void FixedUpdateNetwork()
        {
            if (!inputReader.HasInput) return;

            bool grounded = IsGrounded;

            float speedDiff = inputReader.Move.x * stat.MoveSpeed - rb.linearVelocity.x;
            rb.AddForce(Vector2.right * (speedDiff * moveAcceleration), ForceMode2D.Force);

            if (grounded && inputReader.JumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * stat.JumpPower, ForceMode2D.Impulse);
            }

            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

            if (inputReader.Move.x != 0f)
                spriteRenderer.flipX = inputReader.Move.x < 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.darkGreen;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
