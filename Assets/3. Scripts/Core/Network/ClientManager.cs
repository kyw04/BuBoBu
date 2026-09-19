using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClientManager : MonoBehaviour
{
    [SerializeField] private NetworkRunner runnerPrefab;
    private NetworkRunner runner;
    
    public async void JoinGame()
    {
        if (runner != null) return;
        
        var button = EventSystem.current.currentSelectedGameObject;
        if (button.TryGetComponent(out Button btn))
            btn.interactable = false;

        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Client,
            SessionName  = "test-room",
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
        });

        if (result.Ok)
        {
            RPC_LogToServer("Joined game");
        }
        else
        {
            Debug.LogError($"접속 실패: {result.ShutdownReason}");
            Destroy(runner.gameObject);
            runner = null;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_LogToServer(string message, RpcInfo info = default)
    {
        Debug.Log($"[Client {info.Source}] {message}");
    }
}
