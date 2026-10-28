using UnityEngine;

// 씬의 Augment Canvas에 붙음. 로컬 플레이어의 AugmentSelector가 찾아서 Show/Hide 호출
public class AugmentUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private AugmentButton[] buttons;

    private AugmentSelector selector;

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

        AugmentSelector target = selector;
        Hide(); // 서버 응답 전에 바로 닫아 중복 클릭 방지
        target.Select(slot);
    }
}
