using UnityEngine;

// 씬의 Augment Canvas에 붙음. 로컬 플레이어의 AugmentSelector가 찾아서 Show/Hide 호출
public class AugmentUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private AugmentButton[] buttons;
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

        for (int i = 0; i < buttons.Length; i++)
        {
            AugmentData data = i < AugmentSelector.ChoiceCount
                ? selector.Database.Get(selector.GetChoice(i))
                : null;
            buttons[i].SetData(data);
        }

        // 이미 열려 있는 상태에서 갱신될 때는 차단 시간을 다시 시작하지 않음
        if (!panel.activeSelf)
            shownTime = Time.unscaledTime;

        panel.SetActive(true);
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
