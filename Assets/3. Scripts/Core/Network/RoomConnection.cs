using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

namespace PangPangShotNetwork
{
    // 클라이언트의 방 러너에 붙는 콜백. 방에서 나가거나 끊기면 메뉴로 돌아감
    // (메뉴의 ClientManager는 게임 씬 로드 시 사라지므로 러너에 직접 등록)
    public class RoomConnection : INetworkRunnerCallbacks
    {
        private const string MenuSceneName = "2. Menu";

        // 메뉴로 돌아갔을 때 보여줄 메시지 (ClientManager가 읽고 비움)
        public static string PendingMessage { get; set; }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (PendingMessage == null && shutdownReason != ShutdownReason.Ok)
                PendingMessage = $"Disconnected ({shutdownReason}).";

            SceneManager.LoadScene(MenuSceneName);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
