using Fusion;
using PangPangShotNetwork;
using UnityEngine;

namespace Player
{
    // Must run before PlayerMovement/PlayerFire each tick (they read Move/JumpPressed/AttackPressed/HasInput
    // via GetComponent, not events), otherwise those consumers read last tick's stale values.
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : NetworkBehaviour
    {
        [Networked] private NetworkButtons PreviousButtons { get; set; }

        private Entity entity;

        public Vector2 Move { get; private set; }
        public Vector2 AimPoint { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool HasInput { get; private set; }

        public override void Spawned()
        {
            entity = GetComponent<Entity>();
        }

        public override void FixedUpdateNetwork()
        {
            // 사망했거나 라운드 진행 중이 아니면 입력이 없는 것처럼 처리
            bool hasInput = GetInput(out NetworkInputData input);
            if (hasInput && entity != null && !entity.CanAct)
            {
                PreviousButtons = input.Buttons;
                hasInput = false;
            }

            if (!hasInput)
            {
                HasInput = false;
                Move = Vector2.zero;
                JumpPressed = false;
                AttackPressed = false;
                return;
            }

            HasInput = true;
            Move = input.Move;
            AimPoint = input.AimPoint;
            JumpPressed = input.Buttons.WasPressed(PreviousButtons, NetworkInputData.JumpButton);
            AttackPressed = input.Buttons.WasPressed(PreviousButtons, NetworkInputData.AttackButton);

            PreviousButtons = input.Buttons;
        }
    }
}
