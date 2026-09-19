using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace PangPangShotNetwork
{
    public class ClientManager : MonoBehaviour
    {
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private Button joinButton;
        private NetworkRunner runner;

        private void Awake()
        {
            joinButton.onClick.AddListener(JoinGame);
        }

        public async void JoinGame()
        {
            if (runner != null) return;

            joinButton.interactable = false;

            runner = Instantiate(runnerPrefab);
            runner.ProvideInput = true;

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = "test-room",
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
            });

            if (!result.Ok)
            {
                Debug.LogError($"접속 실패: {result.ShutdownReason}");
                Destroy(runner.gameObject);
                runner = null;
                joinButton.interactable = true;
            }
        }

        private void OnApplicationQuit()
        {
            runner?.Shutdown();
        }
    }
}