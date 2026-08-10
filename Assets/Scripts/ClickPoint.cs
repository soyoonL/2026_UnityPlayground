using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClickPoint : MonoBehaviour
{
    [Header("업그레이드 시 이미지 변경")]
    public Image characterImage;
    public Sprite[] evolutionSprites;
    private int evolutionStage = 0;

    [Header("포인트")]
    public int currentPoint = 0;
    public int pointPerClick = 1;
    public int requiredPoint= 5;

    [Header("텍스트")]
    public TextMeshProUGUI evolutionPointCountText;
    public TextMeshProUGUI pointCountText;

    Coroutine textEffectCoroutine;
    Vector3 ogPointTextScale;

    private void Start()
    {
        ogPointTextScale = pointCountText.transform.localScale;  // 원래 텍스트의 크기를 담아두는 변수
        ResetText();
    }

    public void PointUp()
    {
        currentPoint+= pointPerClick;
        Debug.Log(currentPoint);

        ResetText();
        DoPointTextEffect(Color.gold, 1.2f);
    }

    public void Upgrade()
    {
        if (evolutionStage < evolutionSprites.Length && currentPoint >= requiredPoint)
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
        characterImage.sprite = evolutionSprites[evolutionStage];
        evolutionStage++;
        currentPoint -= requiredPoint;
        requiredPoint *= 3;
        pointPerClick *= 2;
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
