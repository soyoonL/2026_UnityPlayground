using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct EvolutionData // Ä³¸¯ÅÍ ±¸Á¶Ã¼
{
    public string stageName;
    public Sprite characterSprite;
    public int damage;
    public int requiredPoint;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    [Header("ÁøÈ­ µ¥ÀÌÅÍ ¸®½ºÆ®")]
    public EvolutionData[] evolutionDatabase;
    private int currentStage;

    [Header("Àû µ¥ÀÌÅÍ ¸®½ºÆ®")]
    public EnemyData[] enemyDatabase;

    public int currentPoint;

    public Image characterImage;

    [SerializeField] Enemy enemy;

    public int CurrentDamage => evolutionDatabase[Mathf.Min(currentStage, evolutionDatabase.Length - 1)].damage; // ¶÷´Ù½Ä(=>) ÇÁ·ÎÆÛÆ¼ ¼±¾ð, InndexOutOfRange°¡ ³ªÁö ¾Êµµ·Ï
    public bool IsMaxStage => currentStage >= evolutionDatabase.Length - 1;
    public int CurrentRequiredPoint => IsMaxStage ? 0 : evolutionDatabase[currentStage].requiredPoint;

    private void Awake()
    {
        // ½Ì±ÛÅæ ÃÊ±âÈ­
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
        ogPointTextScale = pointCountText.transform.localScale;  // ï¿½ï¿½ï¿½ï¿½ ï¿½Ø½ï¿½Æ®ï¿½ï¿½ Å©ï¿½â¸¦ ï¿½ï¿½ÆµÎ´ï¿½ ï¿½ï¿½ï¿½ï¿½
        ResetText();
        currentStage = 0;
        UpdateCharacterStage();
        SpawnRandomEnemy();
    }

    public void PointUp()
    {
        currentPoint+= enemy.EnemyKillPoint;
        Debug.Log(currentPoint);

        ResetText();
        DoPointTextEffect(Color.gold, 1.2f);
    }

    public void Upgrade()
    {
        if (currentStage < evolutionDatabase.Length-1 && currentPoint >= evolutionDatabase[currentStage].requiredPoint)
        {
            Evolution();
            ResetText();
            DoPointTextEffect(Color.red, 0.8f);
            if (evolutionStage == evolutionSprites.Length)
            {
                evolutionPointCountText.text = "Evolution End!";
            }
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

    void ResetText()
    {
        pointCountText.text = currentPoint.ToString() + "Points";

        if(evolutionStage != evolutionSprites.Length)
        {
            evolutionPointCountText.text = "Need " + requiredPoint.ToString() + "Points";
        }
    }

    void DoPointTextEffect(Color col,float scale)
    {
        if(textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
        textEffectCoroutine = StartCoroutine(TextEffect(pointCountText,0.15f,col,scale));
    }

    IEnumerator TextEffect(TextMeshProUGUI go, float duration, Color col, float scale)
    {
        Vector3 targetScale = ogPointTextScale * scale;
        Color ogColor = go.color;
        duration /= 2;

        float t = 0;
        while(t < duration)
        {
            t +=Time.deltaTime;
            go.transform.localScale = Vector3.Lerp(ogPointTextScale, targetScale, t / duration);
            go.color = Color.Lerp(ogColor, col, t / duration); ;
            yield return null;
        }
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            go.transform.localScale = Vector3.Lerp(targetScale, ogPointTextScale, t / duration);
            go.color = Color.Lerp(col, ogColor, t / duration);
            yield return null;
        }

        go.transform.localScale = ogPointTextScale;
        go.color = ogColor;
        textEffectCoroutine = null;
    }
}
