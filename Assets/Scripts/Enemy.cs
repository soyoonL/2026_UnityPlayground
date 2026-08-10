using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] Slider hpSlider;
    public float EnemyHp = 100;
    public int EnemyKillPoint = 10;

    void Start()
    {
        hpSlider.maxValue = EnemyHp;
        hpSlider.value = EnemyHp;
    }

    public void TakeDamage(int damage)
    {
        EnemyHp-=damage;
        hpSlider.value = EnemyHp;

        if (EnemyHp <= 0)
        {
            gameManager.PointUp();
            Destroy(gameObject);
            hpSlider.gameObject.SetActive(false);
        }
    }
}
