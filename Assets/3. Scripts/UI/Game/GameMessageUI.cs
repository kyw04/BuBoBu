using System.Collections;
using TMPro;
using UnityEngine;

// 화면 중앙 안내 표시
// - 상태 문구: 서버가 RPC로 보낸 짧은 알림을 일정 시간 표시
// - 메시지 패널: 라운드 시작/증강 선택/게임 종료 단계 동안 화면을 덮는 안내
public class GameMessageUI : MonoBehaviour
{
    [SerializeField] private GameManager game; // 같은 씬의 GameManager
    [SerializeField] private TMP_Text statusText; // 선택 사항
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    private Coroutine statusCoroutine;

    private void Update()
    {
        if (game == null || !game.Object || !game.Object.IsValid) return;

        string message = GetMessage(game);
        bool showPanel = message != null;
        if (messagePanel != null && messagePanel.activeSelf != showPanel)
            messagePanel.SetActive(showPanel);
        if (showPanel && messageText != null)
            messageText.text = message;
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
                $"{winner}팀 라운드 승리!\n<size=70%>{loser}팀 증강 선택 중...  {Mathf.CeilToInt(game.RemainingTime)}</size>",
            GamePhase.RoundStart => $"라운드 {game.Round}  {Mathf.CeilToInt(game.RemainingTime)}",
            GamePhase.GameOver =>
                $"{winner}팀 승리!\n<size=70%>{Mathf.CeilToInt(game.RemainingTime)}초 후 대기실로 돌아갑니다</size>",
            _ => null
        };
    }
}
