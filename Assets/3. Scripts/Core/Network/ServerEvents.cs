using System.Linq;
using Fusion;
using UnityEngine;

namespace PangPangShotNetwork
{
    public class ServerEvents : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        public void PlayerJoined(PlayerRef player)
            => Debug.Log($"[Server] Join {player} → {Runner.ActivePlayers.Count()}명");

        public void PlayerLeft(PlayerRef player)
            => Debug.Log($"[Server] Left {player} → {Runner.ActivePlayers.Count()}명");
    }
}
