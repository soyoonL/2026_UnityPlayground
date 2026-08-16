using UnityEngine;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;  // 불꽃 틀
    [SerializeField] public Transform enemyTransform;     // 적 위치

    public void Shoot()
    {
        if(enemyTransform!=null)
        {
   
            Projectile fire = Instantiate(projectilePrefab, transform.position, transform.rotation);

            int currentDamage = GameManager.Instance.CurrentDamage;

            fire.SetTarget(enemyTransform,currentDamage);
            
        }
       
    }
}
