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
    public GameObject GlovalUI;

    [Header("메인화면 캐릭터 & 애니메이션")]
    public Image MainCharacter;
    public Animator mainCharacterAnimator; // 하나로 통합 사용
    [SerializeField] private RuntimeAnimatorController baseController; // 베이스 애니메이터 컨트롤러

    private AnimatorOverrideController overrideController;
    [Header("보스전 UI")]
    public GameObject bossTimerGroup;
    public TextMeshProUGUI bossTimerText;
    public Slider TimerSlider;

    [Header("일반 적 UI")]
    public Slider hpSlider; // 적 체력바 UI 저장
    public Image enemyImage; // 적 이미지 저장하고, 적이 사망 시 랜덤한 적의 이미지로 교체하는 데 사용

    Coroutine textEffectCoroutine;
    Vector3 ogPointTextScale;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pointCountText != null) ogPointTextScale = pointCountText.transform.localScale;// 원래 텍스트의 크기를 담아두는 변수

        if (mainCharacterAnimator != null && baseController != null)
        {
            overrideController = new AnimatorOverrideController(baseController);
            mainCharacterAnimator.runtimeAnimatorController = overrideController;
        }

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
        AudioManager.Instance.PlaySFX(SFXType.ButtonClick);
        AudioManager.Instance.PlayBGM(BGMType.Main);

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
        ShopManager.Instance.SortCards();
        GameManager.Instance.currentCharacter = data; 
        GameManager.Instance.UpdateCharacterStage();

        ResetText(
                GameManager.Instance.currentPoint,
               GameManager.Instance.CurrentRequiredPoint,
                GameManager.Instance.IsMaxStage
                );
        if (selectCharacterPanel != null) selectCharacterPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        AudioManager.Instance.PlayBGM(BGMType.Main);
        if (GlovalUI != null) GlovalUI.SetActive(true);
        if (ShopManager.Instance != null) ShopManager.Instance.RefreshAllCards();
        
    }

    /// <summary> 전투 화면으로 이동 </summary>
    public void OpenHuntPanel()
    {
        mainPanel.SetActive(false);
        huntPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(SFXType.ButtonClick);
        AudioManager.Instance.PlayBGM(BGMType.Hunt);
    }

    public void OpenShopPanel()
    {
        shopPanel.SetActive(true);
        mainPanel.SetActive(false);
        //huntPanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFXType.ButtonClick);
        AudioManager.Instance.PlayBGM(BGMType.Shop);
    }
    public void CloseShopPanel()
    {
        shopPanel.SetActive(false);
        mainPanel.SetActive(true);
        //huntPanel.SetActive(false);
        AudioManager.Instance.PlaySFX(SFXType.ButtonClick);
        AudioManager.Instance.PlayBGM(BGMType.Main);

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
        pointCountText.text = currentPoint.ToString();

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

    /// <summary> 메인 캐릭터의 애니메이션과 스프라이트를 진화 단계에 맞게 교체 </summary>
    public void SetMainCharacterVisual(EvolutionData data)
    {
        // 1. 베이스 컨트롤러의 원본 클립을 현재 진화 단계의 클립으로 교체(Override)
        if (overrideController != null)
        {
            if (data.idleClip != null) overrideController["Idle"] = data.idleClip;
            if (data.cheerClip != null) overrideController["Cheer"] = data.cheerClip;
        }

        // 2. 캐릭터 이미지 교체
        if (MainCharacter != null)
            MainCharacter.sprite = data.characterSprite;
    }

    /// <summary> 환호 애니메이션 재생 </summary>
    public void PlayMainCharacterCheer()
    {
        if (mainCharacterAnimator == null || !mainCharacterAnimator.isActiveAndEnabled) return;

        // "Cheer" 트리거 파라미터 호출
        mainCharacterAnimator.SetTrigger("Cheer");
    }
}
