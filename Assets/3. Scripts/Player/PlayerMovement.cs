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
        [SerializeField] private Transform[] facingTargets;
        [SerializeField, Range(0f, 1f)] private float wallNormalThreshold = 0.7f;

        [Header("Effects")]
        [SerializeField] private EffectData jumpEffect;
        [SerializeField] private EffectData moveEffect; // 땅에서 이동 중 일정 간격으로 재생 (발소리·먼지)
        [SerializeField, Min(0.05f)] private float moveEffectInterval = 0.3f;
        [SerializeField, Min(0f)] private float moveEffectMinSpeed = 0.5f;

        [Networked] private NetworkBool IsFacingLeft { get; set; }

        private Rigidbody2D rb;
        private PlayerInputReader inputReader;
        private PlayerAnimator animator;
        private IMoveStat stat;

        private float jumpingCount = 0;
        private float[] facingTargetBaseX;
        private readonly ContactPoint2D[] wallContacts = new ContactPoint2D[8];
        private ContactFilter2D wallContactFilter;
        private int shownJumpCount;
        private float moveEffectTimer;

        public Transform Foot => groundCheck;
        // 서버는 방마다 별도 물리 씬을 쓰므로(Multi-Peer) 전역 Physics2D 대신 러너의 물리 씬으로 검사
        public bool IsGrounded => Runner.GetPhysicsScene2D().OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        public override void Spawned()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravity / Physics2D.gravity.y;
            wallContactFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayer };

            inputReader = GetComponent<PlayerInputReader>();
            animator = GetComponentInChildren<PlayerAnimator>();
            stat = GetComponent<IMoveStat>();
            shownJumpCount = animator.JumpCount; // 입장 시점 이전 점프는 연출하지 않음

            facingTargetBaseX = new float[facingTargets.Length];
            for (int i = 0; i < facingTargets.Length; i++)
                facingTargetBaseX[i] = Mathf.Abs(facingTargets[i].localPosition.x);
            ApplyFacing();
        }

        public override void FixedUpdateNetwork()
        {
            if (!inputReader.HasInput) return;

            float moveX = inputReader.Move.x;
            if (IsTouchingWall(moveX))
                moveX = 0f; // 벽 쪽으로 미는 힘이 마찰로 작용해 벽에 붙지 않도록 입력 무시

            float speedDiff = moveX * stat.MoveSpeed - rb.linearVelocity.x;
            rb.AddForce(Vector2.right * (speedDiff * moveAcceleration), ForceMode2D.Force);
            animator.MoveSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            bool grounded = IsGrounded && rb.linearVelocity.y <= 0.01f;
            if (grounded)
                jumpingCount = 0;

            if (inputReader.JumpPressed && jumpingCount < stat.JumpCount)
            {
                grounded = false;
                Jump();
            }
            animator.IsGrounded = grounded;

            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

            float aimOffsetX = inputReader.AimPoint.x - transform.position.x;
            if (aimOffsetX != 0f)
                IsFacingLeft = aimOffsetX < 0f;
            ApplyFacing();
        }

        private bool IsTouchingWall(float moveX)
        {
            if (moveX == 0f) return false;

            int count = rb.GetContacts(wallContactFilter, wallContacts);
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = wallContacts[i].normal;
                // 노멀이 거의 수평이고 입력 방향과 반대면 그 방향의 벽에 닿아 있는 것
                if (Mathf.Abs(normal.x) > wallNormalThreshold && normal.x * moveX < 0f)
                    return true;
            }
            return false;
        }

        public void Jump()
        {
            animator.JumpCount++;
            jumpingCount++;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * stat.JumpPower, ForceMode2D.Impulse);
        }

        public override void Render()
        {
            ApplyFacing();
            UpdateEffects();
        }

        // 점프 횟수/이동 속도는 PlayerAnimator가 동기화하므로 그 값을 보고 모든 클라이언트에서 연출
        private void UpdateEffects()
        {
            if (animator.JumpCount != shownJumpCount)
            {
                shownJumpCount = animator.JumpCount;
                jumpEffect.Play(groundCheck.position);
            }

            if (!animator.IsGrounded || animator.MoveSpeed < moveEffectMinSpeed)
            {
                moveEffectTimer = 0f;
                return;
            }

            moveEffectTimer -= Time.deltaTime;
            if (moveEffectTimer > 0f) return;

            moveEffectTimer = moveEffectInterval;
            moveEffect.Play(groundCheck.position);
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
