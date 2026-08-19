using UnityEngine;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;  // 발사체 프리팹
    [SerializeField] public Transform enemyTransform;   // 적 위치

    /// <summary> 캐릭터를 클릭 시 발사체 생성 </summary>
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
