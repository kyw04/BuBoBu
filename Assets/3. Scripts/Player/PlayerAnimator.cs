using Fusion;
using UnityEngine;

namespace Player
{
    public class PlayerAnimator : NetworkBehaviour
    {
        [SerializeField] private Animator animator;

        [Networked] public float MoveSpeed { get; set; }
        [Networked] public int JumpCount { get; set; }
        [Networked] public bool IsGrounded { get; set; }
        
        private ChangeDetector changes;

        public override void Spawned()
        {
            changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Render()
        {
            animator.SetFloat("MoveSpeed", MoveSpeed);
            animator.SetBool("Grounded", IsGrounded);
            
            foreach (var change in changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(JumpCount):
                        animator.SetTrigger("Jump");
                        break;
                }
            }
        }
    }
}