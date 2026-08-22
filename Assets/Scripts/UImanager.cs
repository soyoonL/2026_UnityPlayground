using TMPro;
using UnityEngine;
using System.Collections;

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

    Coroutine textEffectCoroutine;
    Vector3 ogPointTextScale;

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

        if (pointCountText != null)
        {
            ogPointTextScale = pointCountText.transform.localScale;  // 원래 텍스트의 크기를 담아두는 변수
        }
    }

    private void Start()
    {
       OpenMainPanel();
    }

    /// <summary> 메인 화면으로 이동 </summary>
    public void OpenMainPanel()
    {
        mainPanel.SetActive(true);
        huntPanel.SetActive(false);
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
