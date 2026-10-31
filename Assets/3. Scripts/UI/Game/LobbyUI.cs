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
    [SerializeField] private Button toggleCodeButton; // 코드 보이기/숨기기 (기본 숨김)
    [SerializeField] private TMP_Text toggleCodeLabel;
    [SerializeField] private Button copyCodeButton;   // 숨김 상태에서도 복사 가능
    [SerializeField] private TMP_Text copyCodeLabel;
    [SerializeField] private float copiedFeedbackDuration = 1.5f;
    [SerializeField] private TMP_Text team1ListText;
    [SerializeField] private TMP_Text team2ListText;
    [SerializeField] private Button team1Button;
    [SerializeField] private Button team2Button;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text leaveButtonLabel; // 방장이면 "방 닫기"
    [SerializeField] private TMP_Text hintText;

    private readonly StringBuilder team1Builder = new();
    private readonly StringBuilder team2Builder = new();

    private bool showCode;
    private float copiedUntil;

    private void Awake()
    {
        team1Button.onClick.AddListener(() => SelectTeam(0));
        team2Button.onClick.AddListener(() => SelectTeam(1));
        startButton.onClick.AddListener(StartGame);
        toggleCodeButton.onClick.AddListener(() => showCode = !showCode);
        copyCodeButton.onClick.AddListener(CopyCode);
        leaveButton.onClick.AddListener(Leave);
        panel.SetActive(false);
    }

    private void Update()
    {
        bool show = game != null && game.Object && game.Object.IsValid && game.Phase == GamePhase.Lobby;
        if (panel.activeSelf != show)
            panel.SetActive(show);
        if (!show) return;

        bool isHost = game.Host == game.Runner.LocalPlayer;
        SetText(leaveButtonLabel, isHost ? "방 닫기" : "나가기");
        bool canStart = game.CanStart; // Players 순회 전에 계산 (공유 리스트 사용)
        int localTeam = -1;
        string hostName = string.Empty;

        team1Builder.Clear();
        team2Builder.Clear();
        foreach (Entity entity in game.Players)
        {
            StringBuilder sb = entity.Team == 0 ? team1Builder : team2Builder;
            string playerName = entity.PlayerName.ToString();
            sb.Append(playerName);
            if (entity.Object.InputAuthority == game.Host)
            {
                hostName = playerName;
                sb.Append("  <color=#FFD54A>방장</color>");
            }
            if (entity.HasInputAuthority)
            {
                sb.Append("  <color=#9CFF9C>(나)</color>");
                localTeam = entity.Team;
            }
            sb.Append('\n');
        }

        // 방 코드는 방장에게만 보여주고, 다른 사람에게는 누구 방인지 표시
        toggleCodeButton.gameObject.SetActive(isHost);
        copyCodeButton.gameObject.SetActive(isHost);
        if (isHost)
        {
            string code = showCode ? game.RoomCode : new string('*', game.RoomCode.Length);
            SetText(roomCodeText, $"방 코드  <b>{code}</b>");
            SetText(toggleCodeLabel, showCode ? "숨기기" : "보기");
            SetText(copyCodeLabel, Time.unscaledTime < copiedUntil ? "복사됨!" : "복사");
        }
        else
        {
            SetText(roomCodeText, $"<b>{hostName}</b>의 방");
        }
        SetText(team1ListText, team1Builder.ToString());
        SetText(team2ListText, team2Builder.ToString());

        team1Button.interactable = localTeam != 0;
        team2Button.interactable = localTeam != 1;

        startButton.gameObject.SetActive(isHost);
        startButton.interactable = canStart;

        SetText(hintText, !isHost ? "방장이 시작하기를 기다리는 중..."
            : canStart ? "준비되면 시작을 누르세요"
            : $"팀마다 최소 {game.MinPlayersPerTeam}명이 필요합니다");
    }

    // 방장이 나가면 서버가 방을 닫아 모두 메뉴로 돌아감
    private void Leave()
    {
        leaveButton.interactable = false;
        if (game != null && game.Object && game.Object.IsValid)
            game.Leave();
    }

    // 화면에 숨겨져 있어도 실제 코드를 클립보드에 복사
    private void CopyCode()
    {
        if (game == null || !game.Object || !game.Object.IsValid) return;

        GUIUtility.systemCopyBuffer = game.RoomCode;
        copiedUntil = Time.unscaledTime + copiedFeedbackDuration;
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
