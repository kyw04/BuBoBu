using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace PangPangShotNetwork
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float moveAcceleration = 50f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        // maxFallSpeed * Runner.DeltaTime must stay below groundCheckRadius to avoid tunneling through the ground on landing.
        [SerializeField, Min(0f)] private float maxFallSpeed = 8f;

        private Rigidbody2D rb;

        [Networked] private NetworkButtons PreviousButtons { get; set; }

        public bool IsGrounded => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        public override void Spawned()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravity / Physics2D.gravity.y;
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out NetworkInputData input)) return;

            bool grounded = IsGrounded;

            float speedDiff = input.Move.x * moveSpeed - rb.linearVelocity.x;
            rb.AddForce(Vector2.right * (speedDiff * moveAcceleration), ForceMode2D.Force);

            if (grounded && input.Buttons.WasPressed(PreviousButtons, NetworkInputData.JumpButton))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }

            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

            if (input.Move.x != 0f)
                spriteRenderer.flipX = input.Move.x < 0f;

            PreviousButtons = input.Buttons;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.darkGreen;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
