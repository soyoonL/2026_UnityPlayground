using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    public float EnemyHp = 100;
    public int EnemyKillPoint = 10;

    public void TakeDamage(int damage)
    {
        EnemyHp-=damage;
        
        if(EnemyHp <= 0)
        {
            gameManager.PointUp();
            Destroy(gameObject);
        }
    }
}
