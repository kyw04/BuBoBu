using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace PangPangShotNetwork
{
    public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private Vector3 spawnPosition = new(0f, -1.6f, 0f);

        private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

        public void PlayerJoined(PlayerRef player)
        {
            if (!Runner.IsServer) return;

            NetworkObject playerObject = Runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            spawnedPlayers[player] = playerObject;
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (!Runner.IsServer) return;
            if (!spawnedPlayers.TryGetValue(player, out NetworkObject playerObject)) return;

            if (playerObject) Runner.Despawn(playerObject); // 사망으로 이미 Despawn된 경우 제외
            spawnedPlayers.Remove(player);
        }
    }
}
