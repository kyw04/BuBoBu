using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 방 대기실 UI. 로비 단계에서만 표시: 방 코드, 팀별 인원, 팀 선택, 방장 전용 시작 버튼
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameManager game; // 같은 씬의 GameManager
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text team1ListText;
    [SerializeField] private TMP_Text team2ListText;
    [SerializeField] private Button team1Button;
    [SerializeField] private Button team2Button;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text hintText;

    private readonly StringBuilder team1Builder = new();
    private readonly StringBuilder team2Builder = new();

    private void Awake()
    {
        team1Button.onClick.AddListener(() => SelectTeam(0));
        team2Button.onClick.AddListener(() => SelectTeam(1));
        startButton.onClick.AddListener(StartGame);
        panel.SetActive(false);
    }

    private void Update()
    {
        bool show = game != null && game.Object && game.Object.IsValid && game.Phase == GamePhase.Lobby;
        if (panel.activeSelf != show)
            panel.SetActive(show);
        if (!show) return;

        bool isHost = game.Host == game.Runner.LocalPlayer;
        bool canStart = game.CanStart; // Players 순회 전에 계산 (공유 리스트 사용)
        int localTeam = -1;

        team1Builder.Clear();
        team2Builder.Clear();
        foreach (Entity entity in game.Players)
        {
            StringBuilder sb = entity.Team == 0 ? team1Builder : team2Builder;
            sb.Append('P').Append(entity.Object.InputAuthority.PlayerId);
            if (entity.Object.InputAuthority == game.Host) sb.Append("  <color=#FFD54A>HOST</color>");
            if (entity.HasInputAuthority)
            {
                sb.Append("  <color=#9CFF9C>(YOU)</color>");
                localTeam = entity.Team;
            }
            sb.Append('\n');
        }

        SetText(roomCodeText, $"ROOM CODE  <b>{game.RoomCode}</b>");
        SetText(team1ListText, team1Builder.ToString());
        SetText(team2ListText, team2Builder.ToString());

        team1Button.interactable = localTeam != 0;
        team2Button.interactable = localTeam != 1;

        startButton.gameObject.SetActive(isHost);
        startButton.interactable = canStart;

        SetText(hintText, isHost
            ? canStart ? "Press START when ready" : "Each team needs at least 1 player"
            : "Waiting for the host to start...");
    }

    private void SelectTeam(int team)
    {
        if (game != null && game.Object && game.Object.IsValid)
            game.RPC_SelectTeam(team);
    }

    private void StartGame()
    {
        if (game != null && game.Object && game.Object.IsValid)
            game.RPC_StartGame();
    }

    // 같은 문자열이면 TMP 리빌드를 피함
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value)
            text.text = value;
    }
}
