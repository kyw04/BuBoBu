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

    private int shownWins1 = -1;
    private int shownWins2 = -1;

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
    }

    private void Fill(Image[] circles, int wins, Color color)
    {
        for (int i = 0; i < circles.Length; i++)
            circles[i].color = i < wins ? color : emptyColor;
    }
}
