using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    /// <summary> 캐릭터의 진화 단계에 필요한 정보를 저장하는 배열 </summary>
    [Header("현재 선택된 캐릭터 Data")]
    public CharacterData currentCharacter; // 기존 evolutionDatabase 대신 현재 캐릭터 에셋을 참조

    /// <summary> 적과 관련된 정보를 저장하는 배열 </summary>
    [Header("적 데이터 리스트")]
    public EnemyData[] enemyDatabase;

    /// <summary> 적 제거 시 획득한 포인트(EnemyKillPoint)를 가져오기 위한 Enemy 스크립트 참조 </summary>
    [SerializeField] Enemy enemy;

    // 캐릭터 관련 테이터
    public int currentPoint; // 캐릭터가 현재 소지하고 있는 포인트
    public Image characterImage; // 캐릭터 이미지 저장하고, 캐릭터 진화 시 진화한 이미지로 교체하는 데 사용
   // private int currentStage; // 캐릭터의 현재 진화 단계

    /// <summary> evolutionDatabase에서 damage를 안전하게 참조,InndexOutOfRange가 나지 않도록 Mathf 사용 </summary>
    public int CurrentDamage => currentCharacter.evolutionStages[Mathf.Min(currentCharacter.currentStage, currentCharacter.evolutionStages.Length - 1)].damage;

    /// <summary> ResetText()의 매개변수,현재 마지막 진화 단계에 도달했는지 여부 </summary>
    public bool IsMaxStage => currentCharacter.currentStage >= currentCharacter.evolutionStages.Length - 1;

    /// <summary> ResetText()의 매개변수, 마지막 진화 단계에 도달할시 0을 반환 </summary>
    public int CurrentRequiredPoint => IsMaxStage ? 0 : currentCharacter.evolutionStages[currentCharacter.currentStage].requiredPoint; 

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary> 게임 시작 시 진화단계, 캐릭터, UI, 적 초기화 </summary>
    private void Start()
    {
        // 만약에 시작부터 기본 캐릭터를 지급한다면
        if (currentCharacter != null)
        {
            currentCharacter.isUnlocked = true;
        }

        //currentStage = 0; // 시작 진화 단계는 0단계부터
        UpdateCharacterStage(); // 단계에 맞게 캐릭터 이미지 갱신

        // Instance가 확실히 준비된 후 안전하게 호출
        if(UImanager.Instance != null)
        {
            UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage); // UI 텍스트 
        }
       
        SpawnRandomEnemy(); // 첫번째 적 생성
    }

    //private void OnEnable()
    //{
    //    if(UImanager.Instance != null)
    //    {
    //        UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage); // UI 텍스트 
    //    }
    //}

    /// <summary> 적이 죽을 시 호출되는 함수로 현재 포인트에 EnemyKillPoint만큼 더한 다음 ResetText() 호출 </summary>
    public void PointUp()
    {
        currentPoint+= enemy.currentKillPoint;

        UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
        UImanager.Instance.DoPointTextEffect(Color.gold, 1.2f);
    }

    /// <summary> Upgrade 버튼을 누를 시 호출되는 함수로 Evolution()과 ResetText(), DoPointTextEffect()를 호출 </summary>
    public void Upgrade()
    {
        if (currentCharacter.currentStage < currentCharacter.evolutionStages.Length-1 && currentPoint >= currentCharacter.evolutionStages[currentCharacter.currentStage].requiredPoint)
        {
            Evolution();
            UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
            UImanager.Instance.DoPointTextEffect(Color.red, 0.8f);
            
        }
    }

    /// <summary> 캐릭터 진화와 관련된 함수로, 현재 포인트에서 requiredPoint만큼 차감하고, 현재 진화 단계를 1만큼 올린다. 그리고 UpdateCharacterStage()를 호출</summary>
    void Evolution()
    {
        currentPoint -= currentCharacter.evolutionStages[currentCharacter.currentStage].requiredPoint;
        currentCharacter.currentStage++;
        UpdateCharacterStage();
       
        
    }

    /// <summary> 캐릭터의 이미지를 진화단계에 맞춰 갱신해주는 함수 </summary>
    public void UpdateCharacterStage()
    {
        EvolutionData currentdata = currentCharacter.evolutionStages[currentCharacter.currentStage];
        characterImage.sprite = currentdata.characterSprite;
    }

    /// <summary> 적을 랜덤으로 소환하는 함수로 randomData라는 인수(랜덤으로 나온 적 데이터)를 InitEnemy 함수에 전달해서 호출 </summary>
    public void SpawnRandomEnemy()
    {
        if (enemyDatabase.Length == 0) return;

        int randomIndex = Random.Range(0, enemyDatabase.Length);
        EnemyData randomData = enemyDatabase[randomIndex];
        enemy.InitEnemy(randomData);
    }

}
