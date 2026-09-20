using Fusion;
using UnityEngine;

namespace PangPangShotNetwork
{
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        // maxFallSpeed * Runner.DeltaTime must stay below groundCheckRadius to avoid tunneling through the ground on landing.
        [SerializeField] private float maxFallSpeed = 8f;

        [Networked] private float VerticalVelocity { get; set; }
        [Networked] private NetworkButtons PreviousButtons { get; set; }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out NetworkInputData input)) return;

            bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (grounded && input.Buttons.WasPressed(PreviousButtons, NetworkInputData.JumpButton))
                VerticalVelocity = jumpForce;
            else if (grounded)
                VerticalVelocity = 0f;
            else
                VerticalVelocity = Mathf.Max(VerticalVelocity + gravity * Runner.DeltaTime, -maxFallSpeed);

            Vector3 delta = new Vector3(input.Move.x * moveSpeed, VerticalVelocity, 0f) * Runner.DeltaTime;
            transform.position += delta;
            Physics2D.SyncTransforms();

            if (input.Move.x != 0f)
                spriteRenderer.flipX = input.Move.x < 0f;

            PreviousButtons = input.Buttons;
        }
    }
}
