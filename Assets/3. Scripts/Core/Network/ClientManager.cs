using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PangPangShotNetwork
{
    // 메뉴 화면: 방 만들기(로비 서버에 요청 → 받은 코드로 입장) / 코드 입력해서 입장
    public class ClientManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private TMP_InputField nameInput; // 비워 두면 플레이어NNN
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string lobbySession = LobbyProtocol.DefaultLobbySession;
        [SerializeField] private float requestTimeout = 10f;

        private NetworkRunner runner;
        private TaskCompletionSource<string> createRoomResponse;

        private void Awake()
        {
            createButton.onClick.AddListener(CreateRoom);
            joinButton.onClick.AddListener(JoinByCode);

            codeInput.characterLimit = LobbyProtocol.CodeLength;
            codeInput.onValueChanged.AddListener(OnCodeChanged);
            OnCodeChanged(codeInput.text);

            nameInput.characterLimit = PlayerNameRules.MaxLength;
            nameInput.text = PlayerNameRules.LoadSaved(); // 지난번에 입력한 이름 유지 (PlayerPrefs라 재실행해도 남음)
            nameInput.onEndEdit.AddListener(SaveName);    // 방에 안 들어가고 꺼도 저장되도록 입력 끝나면 바로 저장

            // 방에서 나왔거나 방이 닫혀서 돌아온 경우 이유 표시
            SetStatus(RoomConnection.PendingMessage ?? string.Empty);
            RoomConnection.PendingMessage = null;
        }

        public async void CreateRoom()
        {
            if (runner != null) return;
            SetBusy(true, "방을 만드는 중...");

            string code = await RequestRoomCode();
            if (this == null) return;

            if (string.IsNullOrEmpty(code))
            {
                SetBusy(false, "방을 만들지 못했습니다.");
                return;
            }

            await JoinRoom(code);
        }

        public async void JoinByCode()
        {
            if (runner != null) return;

            string code = LobbyProtocol.NormalizeCode(codeInput.text);
            if (code.Length != LobbyProtocol.CodeLength)
            {
                SetStatus($"{LobbyProtocol.CodeLength}자리 코드를 입력하세요.");
                return;
            }

            SetBusy(true, $"{code} 방에 입장하는 중...");
            await JoinRoom(code);
        }

        // 로비 러너에 잠깐 접속해 방 생성을 요청하고 코드를 받은 뒤 바로 나감
        private async Task<string> RequestRoomCode()
        {
            var lobby = Instantiate(runnerPrefab);
            lobby.name = "Lobby Runner";
            lobby.ProvideInput = false;
            lobby.AddCallbacks(this);

            var result = await lobby.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = lobbySession,
                SceneManager = lobby.GetComponent<NetworkSceneManagerDefault>(),
            });

            string code = null;
            if (result.Ok)
            {
                createRoomResponse = new TaskCompletionSource<string>();
                lobby.SendReliableDataToServer(LobbyProtocol.CreateRoomRequest, new byte[] { 1 });

                Task finished = await Task.WhenAny(createRoomResponse.Task, Task.Delay(TimeSpan.FromSeconds(requestTimeout)));
                if (finished == createRoomResponse.Task)
                    code = createRoomResponse.Task.Result;
                createRoomResponse = null;
            }
            else
            {
                Debug.LogError($"로비 접속 실패: {result.ShutdownReason}");
            }

            if (lobby != null)
                await lobby.Shutdown();
            return code;
        }

        private async Task JoinRoom(string code)
        {
            string typedName = PlayerNameRules.Sanitize(nameInput.text);
            PlayerNameRules.Save(typedName); // 랜덤 이름은 저장하지 않음
            string playerName = typedName.Length > 0 ? typedName : PlayerNameRules.RandomName();

            runner = Instantiate(runnerPrefab);
            runner.ProvideInput = true;
            runner.AddCallbacks(runner.GetComponent<PlayerInputHandler>());

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = code,
                ConnectionToken = PlayerNameRules.ToToken(playerName),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runner.GetComponent<NetworkObjectPool>(), // 없으면 Fusion 기본 동작
            });

            if (result.Ok)
            {
                // 접속 성공 후부터 끊기면 메뉴로 복귀 (실패 시엔 이 메뉴에 그대로 남음)
                runner.AddCallbacks(new RoomConnection());
                return; // 서버가 게임 씬을 로드하면 이 메뉴는 사라짐
            }

            Debug.LogError($"접속 실패: {result.ShutdownReason}");
            if (runner != null)
                Destroy(runner.gameObject);
            runner = null;

            if (this == null) return;
            SetBusy(false, result.ShutdownReason == ShutdownReason.GameNotFound
                ? $"{code} 방을 찾을 수 없습니다."
                : $"입장하지 못했습니다 ({result.ShutdownReason}).");
        }

        private static void SaveName(string value) => PlayerNameRules.Save(PlayerNameRules.Sanitize(value));

        private void OnCodeChanged(string value)
        {
            string normalized = LobbyProtocol.NormalizeCode(value);
            if (normalized != value)
                codeInput.SetTextWithoutNotify(normalized);
        }

        private void SetBusy(bool busy, string message)
        {
            createButton.interactable = !busy;
            joinButton.interactable = !busy;
            codeInput.interactable = !busy;
            nameInput.interactable = !busy;
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void OnApplicationQuit()
        {
            runner?.Shutdown();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            if (key == LobbyProtocol.CreateRoomResponse)
                createRoomResponse?.TrySetResult(LobbyProtocol.DecodeCode(data.ToArray()));
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
