using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image characterIcon; // 캐릭터 대표 아이콘 Image
    [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름 Text
    [SerializeField] private TextMeshProUGUI priceText; // 가격 표시 Text (필요 시)
    [SerializeField] private Button actionButton; // 구매/장착 버튼
    [SerializeField] private TextMeshProUGUI buttonText; // 버튼 내부 Text

    private CharacterData cardData; // 넘겨받은 캐릭터 데이터를 저장할 변수

    /// <summary> ShopManager에서 카드를 생성할 때 데이터를 채워주는 함수 </summary>
    public void SetUpCard(CharacterData data)
    {
        cardData = data; // data 속 정보를 저장하여 꺼내 쓸 수 있도록 함

        if (characterIcon != null && data.icon != null) characterIcon.sprite = data.icon;
        if (nameText != null) nameText.text = data.characterName;

        UpdateState();

        actionButton.onClick.RemoveAllListeners(); // 버튼에 연결되어 있던 기존 클릭 함수를 모두 제거하요 초기화
        actionButton.onClick.AddListener(OnClickActionButton); // 이벤트 하나만 깔끔하게
    }

    /// <summary> 해금 여부 및 장착 여부에 따른 버튼 상태 갱신 </summary>
    public void UpdateState()
    {
        if(cardData == null) return;

        bool isEquipped = (GameManager.Instance.currentCharacter == cardData);

        if (isEquipped) // 현재 장착 중인 캐릭터와 카드 데이터가 일치하면
        {
            buttonText.text = "Equipped";
            actionButton.interactable = false;
            priceText.gameObject.SetActive(false);
        }
        else if (cardData.isUnlocked) // 일치하진 않지만 해금은 해놓은 상태이면
        {
            buttonText.text = "Equip";
            actionButton.interactable = true;
            priceText.gameObject.SetActive(false);
        }
        else // 둘다 아니면
        {
            buttonText.text = $"{cardData.price}P";
            actionButton.interactable = true;
        }
    }

    /// <summary> 버튼 클릭 시 실행되는 함수 </summary>
    private void OnClickActionButton()
    {
        bool isEquipped = (GameManager.Instance.currentCharacter == cardData);

        if (isEquipped) return;

        if (cardData.isUnlocked) // 캐릭터를 구매한 경우
        {
            // 장착 로직 실행
            ShopManager.Instance.EquipCharacter(cardData); 
        }
        else
        {
            // 구매 로직 실행
            ShopManager.Instance.BuyCharacter(cardData);
        }
    }
}
