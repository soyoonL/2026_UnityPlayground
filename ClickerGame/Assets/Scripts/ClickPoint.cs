using UnityEngine;
using UnityEngine.UI;

public class ClickPoint : MonoBehaviour
{
    [SerializeField]
    private Image characterImage;
    public Sprite[] evolutionSprites;
    private int evolutionStage = 0;

    public int point = 0;
    public int pointPerClick = 1;

    public void PointUp()
    {
        point+= pointPerClick;
        Debug.Log(point);
    }

    public void Upgrade()
    {
        if (evolutionStage < evolutionSprites.Length )
        {
            characterImage.sprite = evolutionSprites[evolutionStage];
            evolutionStage++;
            pointPerClick = pointPerClick * 2;
        }
        else
        {
            Debug.Log("³¡!");
        }

    }

   
}
