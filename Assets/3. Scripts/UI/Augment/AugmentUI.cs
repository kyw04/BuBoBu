using TMPro;
using UnityEngine;

// 씬의 Augment Canvas에 붙음. 로컬 플레이어의 AugmentSelector가 찾아서 Show/Hide 호출
public class AugmentUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private AugmentButton[] buttons;
    [SerializeField] private TMP_Text rankText; // 선택 사항: "골드 증강" 같은 랭크 표시
    [SerializeField] private string gunTitle = "총 선택";
    [SerializeField] private Color gunColor = new(1f, 0.45f, 0.35f);
    [SerializeField] private Sprite gunFrame; // 총 선택 카드 배경 프레임
    [SerializeField] private float inputBlockDuration = 1f; // 패널이 뜬 직후 오클릭 방지

    private AugmentSelector selector;
    private float shownTime;

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int slot = i;
            buttons[i].Button.onClick.AddListener(() => OnClickSlot(slot));
        }

        Hide();
    }

    public void Show(AugmentSelector target)
    {
        selector = target;

        if (selector.IsGunChoice)
            ShowGuns();
        else
            ShowAugments();

        // 이미 열려 있는 상태에서 갱신될 때는 차단 시간을 다시 시작하지 않음
        if (!panel.activeSelf)
            shownTime = Time.unscaledTime;

        panel.SetActive(true);
    }

    private void ShowAugments()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            AugmentData data = i < AugmentSelector.ChoiceCount
                ? selector.Database.Get(selector.GetChoice(i))
                : null;
            buttons[i].SetData(data, selector.Database);
        }

        if (rankText != null && selector.Database.TryGetRankSetting(selector.CurrentRank, out AugmentRankSetting rank))
        {
            rankText.text = $"{rank.displayName} 증강";
            rankText.color = rank.color;
        }
    }

    private void ShowGuns()
    {
        GunDatabase guns = selector.GunDatabase;
        for (int i = 0; i < buttons.Length; i++)
        {
            GunData data = guns != null && i < AugmentSelector.ChoiceCount
                ? guns.Get(selector.GetChoice(i))
                : null;
            buttons[i].SetData(data, gunColor, gunFrame);
        }

        if (rankText != null)
        {
            rankText.text = gunTitle;
            rankText.color = gunColor;
        }
    }

    public void Hide()
    {
        selector = null;
        panel.SetActive(false);
    }

    private void OnClickSlot(int slot)
    {
        if (selector == null) return;
        if (Time.unscaledTime - shownTime < inputBlockDuration) return;

        AugmentSelector target = selector;
        Hide(); // 서버 응답 전에 바로 닫아 중복 클릭 방지
        target.Select(slot);
    }
}
