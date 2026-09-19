using System;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PangPangShotNetwork
{
    public class ServerManager : MonoBehaviour
    {
        private const string MenuSceneName = "2. Menu";

        [SerializeField] private int playerCount;
        [SerializeField] private NetworkRunner runnerPrefab;

        private async void Start()
        {
            if (!Application.isBatchMode)
            {
                SceneManager.LoadScene(MenuSceneName);
                return;
            }

            var args = Environment.GetCommandLineArgs();
            Application.targetFrameRate = 30;

            string session = GetArg(args, "-session") ?? Guid.NewGuid().ToString();
            ushort port = ushort.TryParse(GetArg(args, "-port"), out var p) ? p : (ushort)27015;
            string region = GetArg(args, "-region");

            if (!string.IsNullOrEmpty(region))
                PhotonAppSettings.Global.AppSettings.FixedRegion = region;

            var runner = Instantiate(runnerPrefab);
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Server,
                SessionName = session,
                PlayerCount = playerCount,
                Address = NetAddress.Any(port),
                Scene = SceneRef.FromIndex(2), // "3. Game" scene index
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
                Debug.Log($"[Server] started: {session} /  port: {port}");
            else
                Debug.LogError($"[Server] failed: {result.ShutdownReason}");
        }

        private static string GetArg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return (index >= 0 && index + 1 < args.Length) ? args[index + 1] : null;
        }
    }
}