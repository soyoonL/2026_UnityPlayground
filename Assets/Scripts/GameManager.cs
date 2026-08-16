using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct EvolutionData // 캐릭터 구조체
{
    public string stageName;
    public Sprite characterSprite;
    public int damage;
    public int requiredPoint;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    [Header("진화 데이터 리스트")]
    public EvolutionData[] evolutionDatabase;
    private int currentStage;

    [Header("적 데이터 리스트")]
    public EnemyData[] enemyDatabase;

    public int currentPoint;

    public Image characterImage;

    [SerializeField] Enemy enemy;

    public int CurrentDamage => evolutionDatabase[Mathf.Min(currentStage, evolutionDatabase.Length - 1)].damage; // 람다식(=>) 프로퍼티 선언, InndexOutOfRange가 나지 않도록
    public bool IsMaxStage => currentStage >= evolutionDatabase.Length - 1;
    public int CurrentRequiredPoint => IsMaxStage ? 0 : evolutionDatabase[currentStage].requiredPoint;

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
    private void Start()
    {
        currentStage = 0;
        UpdateCharacterStage();
        UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
        SpawnRandomEnemy();
    }

    public void PointUp()
    {
        currentPoint+= enemy.EnemyKillPoint;

        UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
        UImanager.Instance.DoPointTextEffect(Color.gold, 1.2f);
    }

    public void Upgrade()
    {
        if (currentStage < evolutionDatabase.Length-1 && currentPoint >= evolutionDatabase[currentStage].requiredPoint)
        {
            Evolution();
            UImanager.Instance.ResetText(currentPoint, CurrentRequiredPoint, IsMaxStage);
            UImanager.Instance.DoPointTextEffect(Color.red, 0.8f);
            
        }
       
    }

    void Evolution()
    {
        currentPoint -= evolutionDatabase[currentStage].requiredPoint;
        currentStage++;
        UpdateCharacterStage();
       
    }

    void UpdateCharacterStage()
    {
        EvolutionData currentdata = evolutionDatabase[currentStage];
        characterImage.sprite = currentdata.characterSprite;
    }

    public void SpawnRandomEnemy()
    {
        if (enemyDatabase.Length == 0) return;

        int randomIndex = Random.Range(0, enemyDatabase.Length);
        EnemyData randomData = enemyDatabase[randomIndex];
        enemy.InitEnemy(randomData);
    }

    
}
