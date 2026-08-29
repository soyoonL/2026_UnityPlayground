using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UImanager : MonoBehaviour
{
    public static UImanager Instance { get; private set; }

    [Header("텍스트")]
    public TextMeshProUGUI evolutionPointCountText;
    public TextMeshProUGUI pointCountText;

    [Header("패널 관리")]
    public GameObject mainPanel;
    public GameObject huntPanel;
    public GameObject shopPanel;
    public GameObject selectCharacterPanel;

    [Header("메인화면 버튼 관리")]
    public Image MainCharacter;

    [Header("보스전 UI")]
    public GameObject bossTimerGroup;
    public TextMeshProUGUI bossTimerText;

    Coroutine textEffectCoroutine;
    Vector3 ogPointTextScale;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pointCountText != null) ogPointTextScale = pointCountText.transform.localScale;// 원래 텍스트의 크기를 담아두는 변수
        
    }

    private void Start()
    {
        OpenSelectCharacterPanel();
    }

    /// <summary> 메인 화면으로 이동 </summary>
    public void OpenMainPanel()
    {
        mainPanel.SetActive(true);
        huntPanel.SetActive(false);

    }

    public void OpenSelectCharacterPanel()
    {
        if(selectCharacterPanel != null) selectCharacterPanel.SetActive(true);
       
    }

    public void SelectStarterCharacter(CharacterData data)
    {
        ShopManager.Instance.ResetAllCharacterData();
        data.isUnlocked = true;
        //data.defaultIsUnlocked = true;
        GameManager.Instance.currentCharacter = data; 
        GameManager.Instance.UpdateCharacterStage();

        ResetText(
                GameManager.Instance.currentPoint,
               GameManager.Instance.CurrentRequiredPoint,
                GameManager.Instance.IsMaxStage
                );
        if (selectCharacterPanel != null) selectCharacterPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (ShopManager.Instance != null) ShopManager.Instance.RefreshAllCards();
        
    }

    /// <summary> 전투 화면으로 이동 </summary>
    public void OpenHuntPanel()
    {
        mainPanel.SetActive(false);
        huntPanel.SetActive(true);
       
    }

    public void OpenShopPanel()
    {
        shopPanel.SetActive(true);
        mainPanel.SetActive(false);
        //huntPanel.SetActive(false);
       
    }
    public void CloseShopPanel()
    {
        shopPanel.SetActive(false);
        mainPanel.SetActive(true);
        //huntPanel.SetActive(false);
       
    }

    /// <summary> 보스전 UI 활성화 </summary>
    public void ToggleBossTimerUI(bool isActive)
    {
        if(bossTimerGroup != null) bossTimerGroup.SetActive(isActive);
    }

    /// <summary> 보스전 타이머 </summary>
    public void UpdateBossTimer(float remainingTime)
    {
        // Mathf.CeilToInt(f) : 입력받은 수를 무조건 올림하여 정수(int)로 반환하는 유니티 함수
        if (bossTimerText != null) bossTimerText.text = $"Boss : {Mathf.CeilToInt(remainingTime)}s";
    }

    public void ResetText(int currentPoint, int requiredPoint, bool isMaxStage)
    {
        pointCountText.text = currentPoint.ToString() + "Points";

        if (!isMaxStage)
        {
            evolutionPointCountText.text = "Need " + requiredPoint.ToString() + "Points";
        }
        else
        {
            evolutionPointCountText.text = "Evolution End";
        }
    }

    public void DoPointTextEffect(Color col, float scale)
    {
        if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
        textEffectCoroutine = StartCoroutine(TextEffect(pointCountText, 0.15f, col, scale));
    }

    IEnumerator TextEffect(TextMeshProUGUI go, float duration, Color col, float scale)
    {
        Vector3 targetScale = ogPointTextScale * scale;
        Color ogColor = go.color;
        duration /= 2;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
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
