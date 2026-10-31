using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PangPangShotNetwork
{
    // 데디 서버 프로세스 하나가 로비 러너 1개 + 방 러너 여러 개를 운영 (Multi-Peer)
    // 클라이언트는 로비에 잠깐 접속해 방 생성을 요청하고, 받은 코드로 방 러너에 접속함
    public class ServerManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const string MenuSceneName = "2. Menu";
        private const int GameSceneIndex = 2; // "3. Game"

        [SerializeField] private int playerCount;          // 방 하나의 최대 인원
        [SerializeField] private int lobbyPlayerCount = 50; // 방 생성 요청용 로비 동시 접속 수
        [SerializeField] private int maxRooms = 20;
        [SerializeField] private NetworkRunner runnerPrefab;

        private readonly Dictionary<string, NetworkRunner> rooms = new();
        private NetworkRunner lobbyRunner;

        private async void Start()
        {
            if (!Application.isBatchMode)
            {
                SceneManager.LoadScene(MenuSceneName);
                return;
            }

            var args = Environment.GetCommandLineArgs();
            Application.targetFrameRate = 30;

            string lobby = GetArg(args, "-lobby") ?? LobbyProtocol.DefaultLobbySession;
            ushort port = ushort.TryParse(GetArg(args, "-port"), out var p) ? p : (ushort)27015;
            string region = GetArg(args, "-region");

            if (!string.IsNullOrEmpty(region))
                PhotonAppSettings.Global.AppSettings.FixedRegion = region;

            // 한 프로세스에서 러너 여러 개를 돌리기 위해 서버만 Multi-Peer로 동작
            NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;

            lobbyRunner = Instantiate(runnerPrefab);
            lobbyRunner.name = "Lobby Runner";
            lobbyRunner.AddCallbacks(this);

            var result = await lobbyRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Server,
                SessionName = lobby,
                PlayerCount = lobbyPlayerCount,
                Address = NetAddress.Any(port),
                IsVisible = false,
                SceneManager = lobbyRunner.GetComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
                Debug.Log($"[Server] lobby started: {lobby} / port: {port}");
            else
                Debug.LogError($"[Server] lobby failed: {result.ShutdownReason}");
        }

        private void Update()
        {
            // 종료된 방(빈 방 타임아웃 등) 정리
            List<string> closed = null;
            foreach (var (code, runner) in rooms)
            {
                if (runner == null || runner.IsShutdown)
                    (closed ??= new List<string>()).Add(code);
            }

            if (closed == null) return;
            foreach (string code in closed)
            {
                rooms.Remove(code);
                Debug.Log($"[Server] room removed: {code} ({rooms.Count} rooms)");
            }
        }

        private async Task<string> CreateRoom()
        {
            if (rooms.Count >= maxRooms) return null;

            string code;
            do code = LobbyProtocol.GenerateCode();
            while (rooms.ContainsKey(code));

            var runner = Instantiate(runnerPrefab);
            runner.name = $"Room Runner {code}";
            rooms[code] = runner; // 시작 중에도 코드 중복이 안 나도록 먼저 등록

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Server,
                SessionName = code,
                PlayerCount = playerCount,
                Address = NetAddress.Any(),
                IsVisible = false, // 코드로만 입장
                Scene = SceneRef.FromIndex(GameSceneIndex),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runner.GetComponent<NetworkObjectPool>() // 없으면 Fusion 기본 동작
            });

            if (result.Ok)
            {
                Debug.Log($"[Server] room created: {code} ({rooms.Count} rooms)");
                return code;
            }

            Debug.LogError($"[Server] room failed: {code} / {result.ShutdownReason}");
            rooms.Remove(code);
            return null;
        }

        private async void HandleCreateRoomRequest(NetworkRunner runner, PlayerRef player)
        {
            string code = await CreateRoom();
            if (runner != null && runner.IsRunning)
                runner.SendReliableDataToPlayer(player, LobbyProtocol.CreateRoomResponse, LobbyProtocol.EncodeCode(code));
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            if (runner != lobbyRunner) return;

            if (key == LobbyProtocol.CreateRoomRequest)
                HandleCreateRoomRequest(runner, player);
        }

        private static string GetArg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return (index >= 0 && index + 1 < args.Length) ? args[index + 1] : null;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
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
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
