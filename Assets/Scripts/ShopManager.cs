using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("상점 데이터 및 프리팹")]
    [SerializeField] private CharacterData[] allCharacters; // 상점에 판매할 모든 캐릭터
    [SerializeField] private ShopCardUI cardPrefab; // 카드 UI 프리팹
    [SerializeField] private Transform cardParent; // ScrollView의 Content Transform

    private List<ShopCardUI> spawnedCards = new List<ShopCardUI>(); // 카드 프리팹을 저장할 리스트

    private void Awake()
    {
        if (Instance == null) Instance = this; 
        else Destroy(gameObject);

    }

    private void Start()
    {
        CreatShopCards();
    }

    /// <summary> CharacterData 배열 수만큼 카드 프리팹 동적 생성 </summary>
    private void CreatShopCards()
    {
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }
        spawnedCards.Clear(); // 카드 중복 생성을 막는 안전장치이자 청소 코드

        foreach(CharacterData data in allCharacters)
        {
            ShopCardUI newCard = Instantiate(cardPrefab, cardParent); // 카드 프리팹과 생성 위치 
            newCard.SetUpCard(data); // 카드 프리팹에 캐릭터 데이터 전달
            spawnedCards.Add(newCard); // 이렇게 만들어진 캐릭터 프리팹을 리스트에 저장
        }
    }

    /// <summary> 캐릭터 구매 기능 </summary>
    public void BuyCharacter(CharacterData data)
    {
        if(GameManager.Instance.currentPoint < data.price)
        {
            Debug.Log("포인트 부족합니다");
            return;
        }

        GameManager.Instance.currentPoint -= data.price;
        data.isUnlocked = true;

        RefreshAllCards();

        if(UImanager.Instance != null)
        {
            UImanager.Instance.ResetText(GameManager.Instance.currentPoint,
                GameManager.Instance.CurrentRequiredPoint,
                GameManager.Instance.IsMaxStage
            ); // ResetText 함수가 3개의 매개변수를 받도록 작성되어서 에러 방지를 위해 변경되지 않는 값도 일단 넣어둠)
        }
    }

    /// <summary> 캐릭터 장착 기능 </summary>
    public void EquipCharacter(CharacterData data)
    {
        GameManager.Instance.currentCharacter = data;
        RefreshAllCards();
    }

    /// <summary> 모든 카드의 UI 버튼 상태(장착 중/장착/구매) 갱신 </summary>
    public void RefreshAllCards()
    {
        foreach (var card in spawnedCards) // var : 암시적 타입 ,오른쪽 자료형을 보고 컴파일러가 타입을 알아서 추론하게 함, 가독성 관리에 굿
        {
            card.UpdateState();
        }
    }
}
