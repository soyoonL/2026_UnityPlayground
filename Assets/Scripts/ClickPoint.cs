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
    public int requiredPoint=5;

    public void PointUp()
    {
        currentPoint+= pointPerClick;
        Debug.Log(currentPoint);
    }

    public void Upgrade()
    {
        if (evolutionStage < evolutionSprites.Length && currentPoint >= requiredPoint)
        {
            characterImage.sprite = evolutionSprites[evolutionStage];
            evolutionStage++;
            currentPoint -= requiredPoint;
            requiredPoint *= 3;
            pointPerClick *= 2;
        }
        else if(currentPoint < requiredPoint)
        {
            Debug.Log("포인트가 부족해용..");
        }
        else
        {
            Debug.Log("끝!");
        }

    }

   
}
