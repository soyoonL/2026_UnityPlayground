using UnityEngine;

public class Enemy : MonoBehaviour
{
    //[SerializeField] private ClickPoint gameManager;
    public float EnemyHp = 100;
    
    void Start()
    {
        
    }

    public void TakeDamage(int damage)
    {
        EnemyHp-=damage;
        
        //if(EnemyHp <= 0)
        //{

       // }
    }
}
