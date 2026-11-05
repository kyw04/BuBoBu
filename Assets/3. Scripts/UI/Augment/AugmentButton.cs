using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AugmentButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText; // 선택 사항
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Graphic rankFrame; // 선택 사항: 랭크 색으로 칠할 테두리/배경

    private Button button;

    // 패널이 비활성 상태면 Awake가 아직 안 불렸을 수 있으므로 지연 초기화
    public Button Button => button != null ? button : button = GetComponent<Button>();

    public void SetData(AugmentData data, AugmentDatabase database)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Color? frameColor = database != null && database.TryGetRankSetting(data.Rank, out AugmentRankSetting rank)
            ? rank.color
            : null;
        Set(data.DisplayName, data.Description, data.Icon, frameColor);
    }

    public void SetData(GunData data, Color frameColor)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Set(data.DisplayName, data.Description, data.Icon, frameColor);
    }

    private void Set(string displayName, string description, Sprite icon, Color? frameColor)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = displayName;

        if (descriptionText != null)
            descriptionText.text = description;

        if (rankFrame != null && frameColor.HasValue)
            rankFrame.color = frameColor.Value;

        Button.interactable = true;
    }
}
