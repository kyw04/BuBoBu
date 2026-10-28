using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상단 승리 카운트 표시. 팀별 동그라미 중 이긴 횟수만큼 채워서 보여줌
public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameManager game; // 같은 씬의 GameManager
    [SerializeField] private Image[] team1Circles;
    [SerializeField] private Image[] team2Circles;
    [SerializeField] private Color team1Color = new(0.3f, 0.6f, 1f);
    [SerializeField] private Color team2Color = new(1f, 0.35f, 0.35f);
    [SerializeField] private Color emptyColor = new(1f, 1f, 1f, 0.25f);
    [SerializeField] private TMP_Text statusText; // 라운드 시작 카운트다운 (선택 사항)
    [SerializeField] private GameObject messagePanel; // 라운드 시작/증강 선택/게임 종료 때 화면을 덮는 패널
    [SerializeField] private TMP_Text messageText;

    private int shownWins1 = -1;
    private int shownWins2 = -1;
    private Coroutine statusCoroutine;
    
    private void Update()
    {
        if (game == null || !game.Object || !game.Object.IsValid) return;

        int wins1 = game.GetWins(0);
        int wins2 = game.GetWins(1);
        if (wins1 != shownWins1)
        {
            shownWins1 = wins1;
            Fill(team1Circles, wins1, team1Color);
        }
        if (wins2 != shownWins2)
        {
            shownWins2 = wins2;
            Fill(team2Circles, wins2, team2Color);
        }
        
        string message = GetMessage(game);
        bool showPanel = message != null;
        if (messagePanel != null && messagePanel.activeSelf != showPanel)
            messagePanel.SetActive(showPanel);
        if (showPanel && messageText != null)
            messageText.text = message;
    }

    private void Fill(Image[] circles, int wins, Color color)
    {
        for (int i = 0; i < circles.Length; i++)
            circles[i].color = i < wins ? color : emptyColor;
    }
    
    public void ShowStatus(string message, float seconds)
    {
        if (statusText == null) return;

        statusText.text = message;
        if (statusCoroutine != null) StopCoroutine(statusCoroutine);
        statusCoroutine = StartCoroutine(StatusCoroutine(seconds));
    }

    private IEnumerator StatusCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        statusText.text = string.Empty;
        statusCoroutine = null;
    }

    // 패널을 띄울 단계면 메시지, 아니면 null
    private static string GetMessage(GameManager game)
    {
        int winner = game.LastRoundWinner + 1;
        int loser = 3 - winner;

        return game.Phase switch
        {
            GamePhase.AugmentSelect =>
                $"TEAM {winner} wins the round!\n<size=70%>TEAM {loser} is choosing augments...  {Mathf.CeilToInt(game.RemainingTime)}</size>",
            GamePhase.RoundStart => $"Round {game.Round}  {Mathf.CeilToInt(game.RemainingTime)}",
            GamePhase.GameOver =>
                $"TEAM {winner} VICTORY!\n<size=70%>Back to lobby in {Mathf.CeilToInt(game.RemainingTime)}</size>",
            _ => null
        };
    }
}
