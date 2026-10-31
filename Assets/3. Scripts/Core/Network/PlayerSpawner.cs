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

            // 로비 러너처럼 게임 씬이 없는 러너에서는 플레이어를 만들지 않음
            GameManager gameManager = GameManager.Get(Runner);
            if (gameManager == null) return;

            int team = gameManager.GetTeamForNewPlayer();
            Vector3 position = gameManager.GetSpawnPosition(team, player.PlayerId);

            // 클라이언트가 접속할 때 보낸 이름 (비어 있으면 플레이어NNN)
            string playerName = PlayerNameRules.FromToken(Runner.GetPlayerConnectionToken(player));
            if (playerName.Length == 0)
                playerName = PlayerNameRules.RandomName();

            NetworkObject playerObject = Runner.Spawn(playerPrefab, position, Quaternion.identity, player,
                (_, obj) =>
                {
                    var entity = obj.GetComponent<Entity>();
                    entity.Team = team;
                    entity.PlayerName = playerName;
                });
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
