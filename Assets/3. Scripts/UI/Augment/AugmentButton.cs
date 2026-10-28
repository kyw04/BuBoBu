using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AugmentButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText; // 선택 사항
    [SerializeField] private TMP_Text descriptionText;

    private Button button;

    // 패널이 비활성 상태면 Awake가 아직 안 불렸을 수 있으므로 지연 초기화
    public Button Button => button != null ? button : button = GetComponent<Button>();

    public void SetData(AugmentData data)
    {
        gameObject.SetActive(data != null);
        if (data == null) return;

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (nameText != null)
            nameText.text = data.DisplayName;

        if (descriptionText != null)
            descriptionText.text = data.Description;

        Button.interactable = true;
    }
}
