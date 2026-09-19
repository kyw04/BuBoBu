using Fusion;
using UnityEngine;

namespace PangPangShotNetwork
{
    public struct NetworkInputData : INetworkInput
    {
        public const int JumpButton = 0;

        public Vector2 Move;
        public NetworkButtons Buttons;
    }
}
