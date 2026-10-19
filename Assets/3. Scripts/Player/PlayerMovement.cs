using Fusion;
using UnityEngine;

namespace Player
{
    // Runs after PlayerInputReader (-100) so Move/AimPoint are current this tick.
    // The gun is not a facing target: Gun rotates itself toward AimPoint (see Gun.Aim).
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
        // Children mirrored on facing left. Facing can't rotate the root: NetworkRigidbody2D syncs only the Z
        // angle and overwrites the root rotation every Render, so a Y flip there is erased and never replicated.
        [SerializeField] private Transform[] facingTargets;

        [Networked] private NetworkBool IsFacingLeft { get; set; }

        private Rigidbody2D rb;
        private PlayerInputReader inputReader;
        private PlayerAnimator animator;
        private IMoveStat stat;

        private float jumpingCount = 0;
        private float[] facingTargetBaseX;
        
        public bool IsGrounded => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        public override void Spawned()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravity / Physics2D.gravity.y;

            inputReader = GetComponent<PlayerInputReader>();
            animator = GetComponentInChildren<PlayerAnimator>();
            stat = GetComponent<IMoveStat>();

            facingTargetBaseX = new float[facingTargets.Length];
            for (int i = 0; i < facingTargets.Length; i++)
                facingTargetBaseX[i] = Mathf.Abs(facingTargets[i].localPosition.x);
            ApplyFacing();
        }

        public override void FixedUpdateNetwork()
        {
            if (!inputReader.HasInput) return;

            float speedDiff = inputReader.Move.x * stat.MoveSpeed - rb.linearVelocity.x;
            rb.AddForce(Vector2.right * (speedDiff * moveAcceleration), ForceMode2D.Force);
            animator.MoveSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            bool grounded = IsGrounded && rb.linearVelocity.y <= 0.01f;
            if (grounded)
                jumpingCount = 0;

            if (inputReader.JumpPressed && jumpingCount < stat.JumpCount)
            {
                grounded = false;
                animator.JumpCount++;
                jumpingCount++;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * stat.JumpPower, ForceMode2D.Impulse);
            }
            animator.IsGrounded = grounded;

            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

            float aimOffsetX = inputReader.AimPoint.x - transform.position.x;
            if (aimOffsetX != 0f)
                IsFacingLeft = aimOffsetX < 0f;
            ApplyFacing();
        }

        public override void Render()
        {
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            bool left = IsFacingLeft;
            for (int i = 0; i < facingTargets.Length; i++)
            {
                Transform t = facingTargets[i];
                Vector3 pos = t.localPosition;
                pos.x = left ? -facingTargetBaseX[i] : facingTargetBaseX[i];
                t.localPosition = pos;
                t.localRotation = Quaternion.Euler(0f, left ? 180f : 0f, 0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.darkGreen;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
