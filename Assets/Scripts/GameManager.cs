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
    public int currentPoint;     // 캐릭터가 현재 소지하고 있는 포인트
    public Image characterImage; // 캐릭터 이미지 저장하고, 캐릭터 진화 시 진화한 이미지로 교체하는 데 사용

    [Header("보스전 설정")]
    public EnemyData bossData;        // 인스펙터에서 등록할 보스 데이터  
    public int killCountToBoss = 10;  // 보스 등장에 필요한 일반 적 처치 수
    public float bossTimeLimit = 30f; // 제한시간(초)
    public int bossFailPenalty = 100; // 실패 시 차감될 포인트

    private int currentKillCount = 0;
    private bool isBossStage = false;
    private float currentBossTimer = 0f;

    /// <summary> 캐릭터가 없을 때는 0, 있을 때에는 현재 단계 데미지 반환 </summary>
    public int CurrentDamage
    {
        get
        {
            if(currentCharacter == null || currentCharacter.evolutionStages == null || currentCharacter.evolutionStages.Length == 0)
                return 0;

            int stageIndex = Mathf.Min(currentCharacter.currentStage, currentCharacter.evolutionStages.Length - 1);
            return currentCharacter.evolutionStages[stageIndex].damage;
        }
    }

    /// <summary> 캐릭터가 없거나 마지막 진화 단계면 true </summary>
    public bool IsMaxStage => currentCharacter != null
        && currentCharacter.evolutionStages != null
        && currentCharacter.currentStage >= currentCharacter.evolutionStages.Length - 1;

    /// <summary> 캐릭터가 없거나 마지막 진화 단계이면 0, 그 외에는 다음 요구 포인트 반환 </summary>
    public int CurrentRequiredPoint
    {
        get
        {
            if(currentCharacter == null || currentCharacter.evolutionStages == null || IsMaxStage)
                return 0;

            return currentCharacter.evolutionStages[currentCharacter.currentStage].requiredPoint;
        }
    }    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 단계에 맞게 캐릭터 이미지 갱신
        UpdateCharacterStage();
        // 첫번째 적 생성
        SpawnRandomEnemy(); 

    }

    //  보스전 타이머
    private void Update()
    {
        if (isBossStage)
        {
            currentBossTimer -= Time.deltaTime;
            UImanager.Instance.TimerSlider.value = currentBossTimer;

            if (UImanager.Instance != null) 
                UImanager.Instance.UpdateBossTimer(currentBossTimer);

            if (currentBossTimer <= 0f)
                OnBossFail();
        }
    }

    /// <summary> Upgrade 버튼을 누를 시 호출되는 함수로 Evolution()과 ResetText(), DoPointTextEffect()를 호출 </summary>
    public void Upgrade()
    {
        if (currentCharacter == null || currentCharacter.evolutionStages == null) return;

        if (!IsMaxStage && currentPoint >= CurrentRequiredPoint)
        {
            Evolution();

            if(UImanager.Instance != null)
            {
                UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
                UImanager.Instance.DoPointTextEffect(Color.red, 0.8f);
                UImanager.Instance.PlayMainCharacterCheer(); // 추가
            }

            AudioManager.Instance.PlaySFX(SFXType.Upgrade);

        }
    }

    /// <summary> 캐릭터 진화와 관련된 함수로, 현재 포인트에서 requiredPoint만큼 차감하고, 현재 진화 단계를 1만큼 올린다. 그리고 UpdateCharacterStage()를 호출</summary>
    void Evolution()
    {
        if (currentCharacter == null) return;

        currentPoint -= CurrentRequiredPoint;
        currentCharacter.currentStage++;
        UpdateCharacterStage();
       
    }

    /// <summary> 캐릭터의 이미지를 진화단계에 맞춰 갱신해주는 함수 </summary>
    public void UpdateCharacterStage()
    {
        if(characterImage == null) return;

        if(currentCharacter == null || currentCharacter.evolutionStages == null || currentCharacter.evolutionStages.Length == 0)
        {
            characterImage.enabled = false;
            return;
        }
         
        characterImage.enabled = true;
        EvolutionData currentdata = currentCharacter.evolutionStages[currentCharacter.currentStage];
        characterImage.sprite = currentdata.characterSprite;
        UImanager.Instance.SetMainCharacterVisual(currentdata); // 변경: 기존 MainCharacter.sprite = ... 대체
    }

    /// <summary> 적이 죽을 시 호출되는 함수로 현재 포인트에 EnemyKillPoint만큼 더한 다음 ResetText() 호출 </summary>
    public void OnEnemyKilled()
    {
        if (isBossStage)
        {
            isBossStage = false;
            AudioManager.Instance.PlayBGM(BGMType.Hunt);
            currentKillCount = 0;
            currentPoint += enemy.currentKillPoint;

            if(UImanager.Instance != null)
            {
                UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
                UImanager.Instance.DoPointTextEffect(Color.gold, 1.5f);
                UImanager.Instance.ToggleBossTimerUI(false);
            }
            SpawnRandomEnemy();
        }
        else
        {
            currentKillCount++;
            currentPoint += enemy.currentKillPoint;  

            if(UImanager.Instance != null)
            {
                UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
                UImanager.Instance.DoPointTextEffect(Color.gold, 1.2f);
            }

            if (currentKillCount >= killCountToBoss) SpawnBoss();
            else SpawnRandomEnemy();
        }
    }

    /// <summary> 보스전 시작 </summary>
    private void SpawnBoss()
    {
        isBossStage = true;
        currentBossTimer = bossTimeLimit;
        UImanager.Instance.TimerSlider.maxValue = bossTimeLimit;
        UImanager.Instance.TimerSlider.value = currentBossTimer;
        enemy.InitEnemy(bossData);
        AudioManager.Instance.PlayBGM(BGMType.Boss);

        if (UImanager.Instance != null)
            UImanager.Instance.ToggleBossTimerUI(true);
    }

    /// <summary> 보스전 실패 시 받는 패널티 </summary>
    private void OnBossFail()
    {
        isBossStage = false;
        currentKillCount = 0;

        // 포인트가 0 이하로 떨어지는 것을 방지
        currentPoint = Mathf.Max(0,currentPoint - bossFailPenalty);

        if(UImanager.Instance != null)
        {
            UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
            UImanager.Instance.DoPointTextEffect(Color.gold, 1.2f);
            UImanager.Instance.ToggleBossTimerUI(false);
        }

        Debug.Log("보스전 실패! 포인트를 잃었습니다.");
        AudioManager.Instance.PlayBGM(BGMType.Hunt);
        SpawnRandomEnemy();
    }
    /// <summary> 적을 랜덤으로 소환하는 함수로 randomData라는 인수(랜덤으로 나온 적 데이터)를 InitEnemy 함수에 전달해서 호출 </summary>
    public void SpawnRandomEnemy()
    {
        if (enemyDatabase == null || enemyDatabase.Length == 0 || enemy == null) return;

        int randomIndex = Random.Range(0, enemyDatabase.Length);
        EnemyData randomData = enemyDatabase[randomIndex];
        enemy.InitEnemy(randomData);
    }

}
