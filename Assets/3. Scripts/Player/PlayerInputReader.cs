using Fusion;
using PangPangShotNetwork;
using UnityEngine;

namespace Player
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : NetworkBehaviour
    {
        [Networked] private NetworkButtons PreviousButtons { get; set; }

        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool HasInput { get; private set; }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out NetworkInputData input))
            {
                HasInput = false;
                JumpPressed = false;
                AttackPressed = false;
                return;
            }

            HasInput = true;
            Move = input.Move;
            JumpPressed = input.Buttons.WasPressed(PreviousButtons, NetworkInputData.JumpButton);
            AttackPressed = input.Buttons.WasPressed(PreviousButtons, NetworkInputData.AttackButton);

            PreviousButtons = input.Buttons;
        }
    }
}
