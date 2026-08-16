using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct EnemyData
{
    public string enemyName;
    public float maxHp;
    public int killPoint;
    public Sprite enemySprite;
}

public class Enemy : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] Slider hpSlider;
    [SerializeField] Image enemyImage;

    public float currentHp;
    public int EnemyKillPoint = 10;

    public void InitEnemy(EnemyData data)
    {
        currentHp = data.maxHp;
        EnemyKillPoint = data.killPoint;

        hpSlider.maxValue = data.maxHp;
        hpSlider.value = currentHp;
        enemyImage.sprite = data.enemySprite;
    }
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        hpSlider.value = currentHp;

        if (currentHp <= 0)
        {
            gameManager.PointUp();
            gameManager.SpawnRandomEnemy();
        }
    }
}
