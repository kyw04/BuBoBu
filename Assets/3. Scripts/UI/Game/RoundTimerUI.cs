using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상단 라운드 남은 시간 표시. 전투 중(Playing)에만 보이고, 얼마 남지 않으면 색을 바꿈
public class RoundTimerUI : MonoBehaviour
{
    [SerializeField] private GameManager game; // 같은 씬의 GameManager
    [SerializeField] private Image timerImage;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float warningTime = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new(1f, 0.35f, 0.35f);

    private int shownSeconds = -1;

    private void Update()
    {
        if (timerText == null) return;

        bool playing = game != null && game.Object && game.Object.IsValid && game.Phase == GamePhase.Playing;
        if (!playing)
        {
            shownSeconds = -1;
            return;
        }

        float remaining = game.RemainingTime;
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        timerImage.fillAmount = seconds / game.RoundTimeLimit;
        timerText.text = seconds.ToString();
        timerText.color = normalColor;
        if (remaining <= warningTime)
            timerText.color = seconds % 2 == (int)warningTime % 2 ? warningColor : normalColor;
    }
}
