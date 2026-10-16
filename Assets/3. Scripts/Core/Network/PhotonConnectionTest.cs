using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// 빈 씬에 이 컴포넌트를 붙이고 실행하면 Photon Fusion 서버(클라우드) 접속을 테스트할 수 있다.
/// OnGUI 버튼으로 Host/Client/Shared 모드 접속을 시도하고 결과를 화면과 콘솔에 출력한다.
/// </summary>
public class PhotonConnectionTest : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private string sessionName = "BuBoBu_TestRoom";
    [SerializeField] private int playerCount = 4;

    private NetworkRunner _runner;
    private string _status = "대기 중";

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 220), GUI.skin.box);
        GUILayout.Label("<b>Photon Fusion 연결 테스트</b>");
        GUILayout.Label($"상태: {_status}");

        if (_runner == null)
        {
            if (GUILayout.Button("Host로 시작")) _ = StartConnection(GameMode.Host);
            if (GUILayout.Button("Client로 접속")) _ = StartConnection(GameMode.Client);
            if (GUILayout.Button("Shared로 접속")) _ = StartConnection(GameMode.Shared);
        }
        else
        {
            GUILayout.Label($"Mode: {_runner.Mode}");
            GUILayout.Label($"IsConnectedToServer: {_runner.IsConnectedToServer}");
            GUILayout.Label($"LocalPlayer: {_runner.LocalPlayer}");
            GUILayout.Label($"ActivePlayers: {_runner.ActivePlayers.Count()}");

            if (GUILayout.Button("종료 (Shutdown)")) Shutdown();
        }

        GUILayout.EndArea();
    }

    private async System.Threading.Tasks.Task StartConnection(GameMode mode)
    {
        if (_runner != null) return;

        _status = $"{mode} 모드로 접속 시도 중...";
        Debug.Log($"[PhotonConnectionTest] {_status}");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = playerCount,
        });

        if (result.Ok)
        {
            _status = $"접속 성공 ({mode})";
            Debug.Log($"[PhotonConnectionTest] {_status}, Session: {sessionName}");
        }
        else
        {
            _status = $"접속 실패: {result.ShutdownReason} {result.ErrorMessage}";
            Debug.LogError($"[PhotonConnectionTest] {_status}");
            Destroy(_runner);
            _runner = null;
        }
    }

    private void Shutdown()
    {
        if (_runner == null) return;
        _runner.Shutdown();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) =>
        Debug.Log($"[PhotonConnectionTest] 플레이어 입장: {player}");

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) =>
        Debug.Log($"[PhotonConnectionTest] 플레이어 퇴장: {player}");

    public void OnConnectedToServer(NetworkRunner runner) =>
        Debug.Log("[PhotonConnectionTest] 서버에 연결됨");

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _status = $"서버 연결 끊김: {reason}";
        Debug.LogWarning($"[PhotonConnectionTest] {_status}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        _status = $"연결 실패: {reason}";
        Debug.LogError($"[PhotonConnectionTest] {_status}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _status = $"종료됨: {shutdownReason}";
        Debug.Log($"[PhotonConnectionTest] {_status}");
        _runner = null;
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
